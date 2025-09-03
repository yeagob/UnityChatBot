using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using ChatSystem.Services.Orchestrators.Interfaces;
using ChatSystem.Services.Communication.Interfaces;
using ChatSystem.Services.Audio.Interfaces;
using ChatSystem.Services.Agents.Interfaces;
using ChatSystem.Services.Context.Interfaces;
using ChatSystem.Services.Logging;
using ChatSystem.Models.Audio;
using ChatSystem.Models.Communication;
using ChatSystem.Models.Context;
using ChatSystem.Models.Tools;
using ChatSystem.Configuration.Voice;
using ChatSystem.Configuration.ScriptableObjects;
using ChatSystem.Enums;

namespace ChatSystem.Services.Orchestrators
{
    public class RealtimeOrchestrator : IRealtimeOrchestrator
    {
        private readonly IWebSocketService webSocketService;
        private readonly IAudioService audioService;
        private readonly IAgentExecutor agentExecutor;
        private readonly IContextManager contextManager;
        
        private string currentSessionId;
        private string currentAgentId;
        private VoiceAgentConfig currentVoiceAgentConfig;
        private bool isSessionActive;

        public event Action<string> OnTranscriptionReceived;
        public event Action<string> OnResponseGenerated;
        public event Action<string> OnToolExecuted;
        public event Action<string> OnAudioReceived;
        public event Action<string> OnErrorOccurred;

        public bool IsSessionActive => isSessionActive;
        public string CurrentSessionId => currentSessionId;
        public string CurrentAgentId => currentAgentId;

        public RealtimeOrchestrator(
            IWebSocketService webSocketService,
            IAudioService audioService,
            IAgentExecutor agentExecutor,
            IContextManager contextManager)
        {
            this.webSocketService = webSocketService;
            this.audioService = audioService;
            this.agentExecutor = agentExecutor;
            this.contextManager = contextManager;
            
            SetupWebSocketEvents();
            LoggingService.LogInfo("RealtimeOrchestrator initialized");
        }

        public async Task StartSessionAsync(string conversationId, string agentId)
        {
            try
            {
                if (isSessionActive)
                {
                    LoggingService.LogWarning("Session already active, ending current session");
                    await EndSessionAsync();
                }

                currentSessionId = conversationId;
                currentAgentId = agentId;
                currentVoiceAgentConfig = FindVoiceAgentConfig(agentId);
                
                if (currentVoiceAgentConfig == null)
                {
                    string errorMsg = $"VoiceAgentConfig not found for agent: {agentId}";
                    LoggingService.LogError(errorMsg);
                    OnErrorOccurred?.Invoke(errorMsg);
                    return;
                }

                await ConnectWebSocket();
                await InitializeSession();
                
                audioService.SetVoiceSettings(currentVoiceAgentConfig.VoiceSettings);
                
                isSessionActive = true;
                LoggingService.LogInfo($"Realtime session started: {conversationId} with agent: {agentId}");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Failed to start session: {ex.Message}");
                OnErrorOccurred?.Invoke(ex.Message);
            }
        }

        public async Task ProcessVoiceInputAsync(AudioData audioData)
        {
            if (!isSessionActive)
            {
                LoggingService.LogWarning("No active session for voice input");
                return;
            }

            try
            {
                await webSocketService.SendAudioAsync(audioData.rawData);
                LoggingService.LogDebug($"Sent audio data: {audioData.rawData.Length} bytes");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Failed to process voice input: {ex.Message}");
                OnErrorOccurred?.Invoke(ex.Message);
            }
        }

        public async Task SendTextMessageAsync(string message)
        {
            if (!isSessionActive)
            {
                LoggingService.LogWarning("No active session for text message");
                return;
            }

            try
            {
                WebSocketEvent textEvent = new WebSocketEvent
                {
                    type = "conversation.item.create",
                    eventId = Guid.NewGuid().ToString(),
                    data = new
                    {
                        item = new
                        {
                            type = "message",
                            role = "user",
                            content = new[] { new { type = "input_text", text = message } }
                        }
                    }
                };

                await webSocketService.SendEventAsync(textEvent);
                
                await contextManager.AddUserMessageAsync(currentSessionId, message);
                
                LoggingService.LogInfo($"Sent text message: {message}");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Failed to send text message: {ex.Message}");
                OnErrorOccurred?.Invoke(ex.Message);
            }
        }

        public async Task EndSessionAsync()
        {
            try
            {
                isSessionActive = false;
                
                if (webSocketService != null && webSocketService.IsConnected)
                {
                    await webSocketService.DisconnectAsync();
                }
                
                if (audioService != null)
                {
                    if (audioService.IsRecording)
                    {
                        await audioService.StopRecordingAsync();
                    }
                    audioService.StopPlayback();
                }
                
                currentSessionId = null;
                currentAgentId = null;
                currentVoiceAgentConfig = null;
                
                LoggingService.LogInfo("Realtime session ended");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error ending session: {ex.Message}");
                OnErrorOccurred?.Invoke(ex.Message);
            }
        }

        public async Task SwitchAgentAsync(string newAgentId)
        {
            try
            {
                if (newAgentId == currentAgentId)
                {
                    LoggingService.LogInfo("Same agent already active");
                    return;
                }

                VoiceAgentConfig newAgentConfig = FindVoiceAgentConfig(newAgentId);
                if (newAgentConfig == null)
                {
                    string errorMsg = $"VoiceAgentConfig not found for agent: {newAgentId}";
                    LoggingService.LogError(errorMsg);
                    OnErrorOccurred?.Invoke(errorMsg);
                    return;
                }

                currentAgentId = newAgentId;
                currentVoiceAgentConfig = newAgentConfig;
                audioService.SetVoiceSettings(newAgentConfig.VoiceSettings);
                
                await UpdateSessionConfiguration();
                
                LoggingService.LogInfo($"Switched to agent: {newAgentId}");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Failed to switch agent: {ex.Message}");
                OnErrorOccurred?.Invoke(ex.Message);
            }
        }

        private async Task ConnectWebSocket()
        {
            string apiKey = GetApiKeyFromAgentConfig();
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("OpenAI API key not configured in agent provider settings");
            }

            await webSocketService.ConnectAsync(currentVoiceAgentConfig.RealtimeEndpoint, apiKey);
        }

        private async Task InitializeSession()
        {
            WebSocketEvent sessionUpdate = new WebSocketEvent
            {
                type = "session.update",
                eventId = Guid.NewGuid().ToString(),
                data = new
                {
                    session = new
                    {
                        model = currentVoiceAgentConfig.Model,
                        voice = currentVoiceAgentConfig.Voice,
                        instructions = GetSystemPrompt(),
                        turn_detection = currentVoiceAgentConfig.EnableTurnDetection ? 
                            new { type = "server_vad" } : null,
                        tools = GetToolDefinitions(),
                        tool_choice = "auto",
<<<<<<< HEAD
                        temperature = currentAgentConfig.modelConfig?.temperature?? 1.0f,
                        max_response_output_tokens = currentAgentConfig.maxResponseTokens
=======
                        temperature = currentVoiceAgentConfig.ModelConfig?.Temperature ?? 1.0f,
                        max_response_output_tokens = currentVoiceAgentConfig.MaxResponseTokens
>>>>>>> 694ffc53cb9fa4e3129f1024f95adc93ad3daebf
                    }
                }
            };

            await webSocketService.SendEventAsync(sessionUpdate);
        }

        private async Task UpdateSessionConfiguration()
        {
            WebSocketEvent sessionUpdate = new WebSocketEvent
            {
                type = "session.update",
                eventId = Guid.NewGuid().ToString(),
                data = new
                {
                    session = new
                    {
                        model = currentVoiceAgentConfig.Model,
                        voice = currentVoiceAgentConfig.Voice,
                        instructions = GetSystemPrompt(),
                        tools = GetToolDefinitions()
                    }
                }
            };

            await webSocketService.SendEventAsync(sessionUpdate);
        }

        private void SetupWebSocketEvents()
        {
            webSocketService.OnEventReceived += HandleWebSocketEvent;
            webSocketService.OnError += (error) => OnErrorOccurred?.Invoke(error);
            webSocketService.OnDisconnected += () => isSessionActive = false;
        }

        private async void HandleWebSocketEvent(WebSocketEvent wsEvent)
        {
            try
            {
                switch (wsEvent.type)
                {
                    case "conversation.item.input_audio_transcription.completed":
                        await HandleTranscription(wsEvent);
                        break;
                    case "response.audio.delta":
                        await HandleAudioDelta(wsEvent);
                        break;
                    case "response.function_call_arguments.done":
                        await HandleToolCall(wsEvent);
                        break;
                    case "response.text.done":
                        await HandleTextResponse(wsEvent);
                        break;
                    case "error":
                        HandleError(wsEvent);
                        break;
                    default:
                        LoggingService.LogDebug($"Unhandled WebSocket event: {wsEvent.type}");
                        break;
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error handling WebSocket event: {ex.Message}");
                OnErrorOccurred?.Invoke(ex.Message);
            }
        }

        private async Task HandleTranscription(WebSocketEvent wsEvent)
        {
            string transcription = ExtractTranscriptionText(wsEvent.data.ToString());
            if (!string.IsNullOrEmpty(transcription))
            {
                OnTranscriptionReceived?.Invoke(transcription);
                
                await contextManager.AddUserMessageAsync(currentSessionId, transcription);
                LoggingService.LogInfo($"Transcription received: {transcription}");
            }
        }

        private async Task HandleAudioDelta(WebSocketEvent wsEvent)
        {
            byte[] audioChunk = ExtractAudioData(wsEvent.data.ToString());
            if (audioChunk != null && audioChunk.Length > 0)
            {
                await audioService.PlayAudioChunkAsync(audioChunk);
                OnAudioReceived?.Invoke($"Audio chunk: {audioChunk.Length} bytes");
            }
        }

        private async Task HandleToolCall(WebSocketEvent wsEvent)
        {
            string toolName = ExtractToolName(wsEvent.data.ToString());
            string toolArgumentsJson = ExtractToolArguments(wsEvent.data.ToString());
            string toolCallId = ExtractToolCallId(wsEvent.data.ToString());
            
            if (!string.IsNullOrEmpty(toolName))
            {
                LoggingService.LogInfo($"Tool call received: {toolName}");
                OnToolExecuted?.Invoke($"Executing: {toolName}");
                
                try
                {
                    ConversationContext context = await contextManager.GetContextAsync(currentSessionId);
                    
                    var agentResponse = await agentExecutor.ExecuteAgentAsync(currentAgentId, context);
                    
                    string toolResult = agentResponse.success ? agentResponse.content : $"Tool execution failed: {agentResponse.content}";
                    
                    await contextManager.AddToolMessageAsync(currentSessionId, toolResult, toolCallId);
                    
                    await SendToolResponse(toolCallId, toolResult);
                }
                catch (Exception ex)
                {
                    LoggingService.LogError($"Tool execution failed: {ex.Message}");
                    await SendToolResponse(toolCallId, $"Error: {ex.Message}");
                }
            }
        }

        private async Task HandleTextResponse(WebSocketEvent wsEvent)
        {
            string responseText = ExtractResponseText(wsEvent.data.ToString());
            if (!string.IsNullOrEmpty(responseText))
            {
                OnResponseGenerated?.Invoke(responseText);
                
                await contextManager.AddAssistantMessageAsync(currentSessionId, responseText);
                LoggingService.LogInfo($"Text response: {responseText}");
            }
        }

        private void HandleError(WebSocketEvent wsEvent)
        {
            string errorMessage = ExtractErrorMessage(wsEvent.data.ToString());
            LoggingService.LogError($"WebSocket error: {errorMessage}");
            OnErrorOccurred?.Invoke(errorMessage);
        }

        private async Task SendToolResponse(string toolCallId, string result)
        {
            WebSocketEvent toolResponse = new WebSocketEvent
            {
                type = "conversation.item.create",
                eventId = Guid.NewGuid().ToString(),
                data = new
                {
                    item = new
                    {
                        type = "function_call_output",
                        call_id = toolCallId,
                        output = result
                    }
                }
            };

            await webSocketService.SendEventAsync(toolResponse);
        }

        private VoiceAgentConfig FindVoiceAgentConfig(string agentId)
        {
            return null;
        }

        private string GetApiKeyFromAgentConfig()
        {
            if (currentVoiceAgentConfig?.ProviderConfig?.ApiKey != null)
            {
                return currentVoiceAgentConfig.ProviderConfig.ApiKey;
            }
            
            string envKey = System.Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            if (!string.IsNullOrEmpty(envKey))
            {
                return envKey;
            }
            
            LoggingService.LogError("No API key found in agent provider configuration or environment");
            return string.Empty;
        }

        private string GetSystemPrompt()
        {
<<<<<<< HEAD
            return currentAgentConfig?.systemPrompt?.content??
                   "You are a helpful assistant with voice capabilities.";
=======
            return currentVoiceAgentConfig?.SystemPrompt?.SystemPrompt ?? 
                   "You are a helpful voice assistant with tool capabilities.";
>>>>>>> 694ffc53cb9fa4e3129f1024f95adc93ad3daebf
        }

        private object[] GetToolDefinitions()
        {
            if (currentVoiceAgentConfig?.AvailableTools == null)
                return new object[0];

            List<object> tools = new List<object>();
            
            foreach (ToolConfig toolConfig in currentVoiceAgentConfig.AvailableTools)
            {
                if (toolConfig != null && toolConfig.Enabled)
                {
                    tools.Add(new
                    {
                        type = "function",
                        function = new
                        {
                            name = toolConfig.ToolId,
                            description = toolConfig.InputSchema?.Description ?? "",
                            parameters = toolConfig.InputSchema?.ToOpenAIFormat() ?? new object()
                        }
                    });
                }
            }
            
            return tools.ToArray();
        }

        private string ExtractTranscriptionText(string data)
        {
            return ExtractJsonValue(data, "transcript") ?? "";
        }

        private byte[] ExtractAudioData(string data)
        {
            string base64Audio = ExtractJsonValue(data, "delta");
            if (!string.IsNullOrEmpty(base64Audio))
            {
                try
                {
                    return Convert.FromBase64String(base64Audio);
                }
                catch (Exception ex)
                {
                    LoggingService.LogError($"Failed to decode base64 audio: {ex.Message}");
                }
            }
            return new byte[0];
        }

        private string ExtractToolName(string data)
        {
            return ExtractJsonValue(data, "name") ?? "";
        }

        private string ExtractToolArguments(string data)
        {
            return ExtractJsonValue(data, "arguments") ?? "{}";
        }

        private string ExtractToolCallId(string data)
        {
            return ExtractJsonValue(data, "call_id") ?? Guid.NewGuid().ToString();
        }

        private string ExtractResponseText(string data)
        {
            return ExtractJsonValue(data, "text") ?? "";
        }

        private string ExtractErrorMessage(string data)
        {
            return ExtractJsonValue(data, "message") ?? ExtractJsonValue(data, "error") ?? "Unknown error";
        }

        private string ExtractJsonValue(string json, string key)
        {
            if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(key))
                return null;
                
            string searchKey = $@"""{key}"":""";
            int startIndex = json.IndexOf(searchKey);
            if (startIndex == -1) 
            {
                searchKey = $@"""{key}"": """;
                startIndex = json.IndexOf(searchKey);
                if (startIndex == -1) return null;
            }
            
            startIndex += searchKey.Length;
            int endIndex = json.IndexOf('"', startIndex);
            if (endIndex == -1) return null;
            
            return json.Substring(startIndex, endIndex - startIndex);
        }
    }
}