using System;
using System.Threading.Tasks;
using ChatSystem.Services.Orchestrators.Interfaces;
using ChatSystem.Services.Communication.Interfaces;
using ChatSystem.Services.Audio.Interfaces;
using ChatSystem.Services.Agents.Interfaces;
using ChatSystem.Services.Context.Interfaces;
using ChatSystem.Services.Logging;
using ChatSystem.Models.Audio;
using ChatSystem.Models.Communication;
using ChatSystem.Models.Context;
using ChatSystem.Configuration.Voice;
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
        private VoiceAgentConfig currentAgentConfig;
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
                currentAgentConfig = GetVoiceAgentConfig(agentId);
                
                if (currentAgentConfig == null)
                {
                    string errorMsg = $"VoiceAgentConfig not found for agent: {agentId}";
                    LoggingService.LogError(errorMsg);
                    OnErrorOccurred?.Invoke(errorMsg);
                    return;
                }

                await ConnectWebSocket();
                await InitializeSession();
                
                audioService.SetVoiceSettings(currentAgentConfig.VoiceSettings);
                
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
                currentAgentConfig = null;
                
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

                VoiceAgentConfig newAgentConfig = GetVoiceAgentConfig(newAgentId);
                if (newAgentConfig == null)
                {
                    string errorMsg = $"VoiceAgentConfig not found for agent: {newAgentId}";
                    LoggingService.LogError(errorMsg);
                    OnErrorOccurred?.Invoke(errorMsg);
                    return;
                }

                currentAgentId = newAgentId;
                currentAgentConfig = newAgentConfig;
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
            string apiKey = GetApiKey();
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("OpenAI API key not configured");
            }

            await webSocketService.ConnectAsync(currentAgentConfig.RealtimeEndpoint, apiKey);
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
                        model = currentAgentConfig.Model,
                        voice = currentAgentConfig.Voice,
                        instructions = GetSystemPrompt(),
                        turn_detection = currentAgentConfig.EnableTurnDetection ? 
                            new { type = "server_vad" } : null,
                        tools = GetToolDefinitions(),
                        tool_choice = "auto",
                        temperature = currentAgentConfig.modelConfig?.temperature?? 1.0f,
                        max_response_output_tokens = currentAgentConfig.maxResponseTokens
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
                        model = currentAgentConfig.Model,
                        voice = currentAgentConfig.Voice,
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
            string toolArguments = ExtractToolArguments(wsEvent.data.ToString());
            string toolCallId = ExtractToolCallId(wsEvent.data.ToString());
            
            if (!string.IsNullOrEmpty(toolName))
            {
                LoggingService.LogInfo($"Tool call received: {toolName}");
                OnToolExecuted?.Invoke($"Executing: {toolName}");
                
                try
                {
                    var toolResult = await agentExecutor.ExecuteToolAsync(
                        currentAgentId, toolName, toolArguments, currentSessionId);
                    
                    await SendToolResponse(toolCallId, toolResult.Content);
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

        private VoiceAgentConfig GetVoiceAgentConfig(string agentId)
        {
            return null;
        }

        private string GetApiKey()
        {
            return Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? 
                   PlayerPrefs.GetString("OPENAI_API_KEY", "");
        }

        private string GetSystemPrompt()
        {
            return currentAgentConfig?.systemPrompt?.content??
                   "You are a helpful assistant with voice capabilities.";
        }

        private object[] GetToolDefinitions()
        {
            return new object[0];
        }

        private string ExtractTranscriptionText(string data) => "";
        private byte[] ExtractAudioData(string data) => new byte[0];
        private string ExtractToolName(string data) => "";
        private string ExtractToolArguments(string data) => "";
        private string ExtractToolCallId(string data) => "";
        private string ExtractResponseText(string data) => "";
        private string ExtractErrorMessage(string data) => "";
    }
}