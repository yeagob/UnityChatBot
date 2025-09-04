using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Networking;
using UnityEngine;
using ChatSystem.Models.LLM;
using ChatSystem.Models.Context;
using ChatSystem.Models.Tools;
using ChatSystem.Models.LLM.OpenAI;
using ChatSystem.Configuration.ScriptableObjects;
using ChatSystem.Configuration.Voice;
using ChatSystem.Services.Logging;
using ChatSystem.Enums;

namespace ChatSystem.Services.LLM
{
    public class OpenAIService
    {
        public static async Task<LLMResponse> CompleteChatAsync(LLMRequest request, string apiKey, string baseUrl)
        {
            try
            {
                string jsonPayload = BuildOpenAIPayload(request);
                
                LoggingService.LogDebug($"[OpenAIService] Making OpenAI API call to model- {request.model} with PAYLOAD: {jsonPayload}");
                
                UnityWebRequest webRequest = CreateWebRequest(jsonPayload, apiKey, baseUrl);
                
                await SendWebRequestAsync(webRequest);
                
                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    string responseText = webRequest.downloadHandler.text;
                    return ParseOpenAIResponse(responseText, request.model);
                }
                
                string error = $"[OpenAIService] OpenAI API Error: {webRequest.error} - {webRequest.downloadHandler.text}";
                
                LoggingService.LogError(error);
                
                return CreateErrorResponse(request.model, error);
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"[OpenAIService] OpenAI API Exception: {ex.Message}");
                return CreateErrorResponse(request.model, ex.Message);
            }
        }

        #region Realtime API Methods

        public static string BuildRealtimeSessionPayload(VoiceAgentConfig agentConfig)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("{");
                sb.Append("\"type\":\"session.update\",");
                sb.Append($"\"event_id\":\"{Guid.NewGuid()}\",");
                sb.Append("\"session\":{");
                
                sb.Append($"\"model\":\"{agentConfig.Model}\",");
                sb.Append($"\"voice\":\"{agentConfig.Voice}\",");
                sb.Append($"\"instructions\":\"{EscapeJsonString(GetSystemPromptFromConfig(agentConfig))}\",");
                
                if (agentConfig.EnableTurnDetection)
                {
                    sb.Append("\"turn_detection\":{\"type\":\"server_vad\"},");
                }
                
                AppendRealtimeTools(sb, agentConfig);
                sb.Append("\"tool_choice\":\"auto\",");
                sb.Append($"\"temperature\":{GetTemperatureFromConfig(agentConfig)},");
                sb.Append($"\"max_response_output_tokens\":{agentConfig.MaxResponseTokens}");
                
                sb.Append("}}");
                
                string payload = sb.ToString();
                LoggingService.LogDebug($"[OpenAIService] Realtime session payload: {payload}");
                return payload;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"[OpenAIService] Failed to build realtime session payload: {ex.Message}");
                return "{}";
            }
        }

        public static string BuildRealtimeTextMessagePayload(string message)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("{");
                sb.Append("\"type\":\"conversation.item.create\",");
                sb.Append($"\"event_id\":\"{Guid.NewGuid()}\",");
                sb.Append("\"item\":{");
                sb.Append("\"type\":\"message\",");
                sb.Append("\"role\":\"user\",");
                sb.Append("\"content\":[{");
                sb.Append("\"type\":\"input_text\",");
                sb.Append($"\"text\":\"{EscapeJsonString(message)}\"");
                sb.Append("}]}}");
                
                return sb.ToString();
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"[OpenAIService] Failed to build text message payload: {ex.Message}");
                return "{}";
            }
        }

        public static string BuildRealtimeToolResponsePayload(string toolCallId, string result)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("{");
                sb.Append("\"type\":\"conversation.item.create\",");
                sb.Append($"\"event_id\":\"{Guid.NewGuid()}\",");
                sb.Append("\"item\":{");
                sb.Append("\"type\":\"function_call_output\",");
                sb.Append($"\"call_id\":\"{toolCallId}\",");
                sb.Append($"\"output\":\"{EscapeJsonString(result)}\"");
                sb.Append("}}");
                
                return sb.ToString();
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"[OpenAIService] Failed to build tool response payload: {ex.Message}");
                return "{}";
            }
        }

        public static string BuildRealtimeAudioPayload(byte[] audioData)
        {
            try
            {
                string base64Audio = Convert.ToBase64String(audioData);
                
                StringBuilder sb = new StringBuilder();
                sb.Append("{");
                sb.Append("\"type\":\"input_audio_buffer.append\",");
                sb.Append($"\"event_id\":\"{Guid.NewGuid()}\",");
                sb.Append($"\"audio\":\"{base64Audio}\"");
                sb.Append("}");
                
                return sb.ToString();
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"[OpenAIService] Failed to build audio payload: {ex.Message}");
                return "{}";
            }
        }

        private static void AppendRealtimeTools(StringBuilder sb, VoiceAgentConfig agentConfig)
        {
            if (agentConfig.AvailableTools == null || agentConfig.AvailableTools.Length == 0)
            {
                return;
            }

            List<ToolConfig> enabledTools = new List<ToolConfig>();
            foreach (ToolConfig tool in agentConfig.AvailableTools)
            {
                if (tool != null && tool.Enabled)
                {
                    enabledTools.Add(tool);
                }
            }

            if (enabledTools.Count == 0)
            {
                return;
            }

            sb.Append("\"tools\":[");
            
            for (int i = 0; i < enabledTools.Count; i++)
            {
                if (i > 0) sb.Append(",");
                
                ToolConfig tool = enabledTools[i];
                sb.Append("{");
                sb.Append("\"type\":\"function\",");
                sb.Append("\"function\":{");
                sb.Append($"\"name\":\"{tool.ToolId}\",");
                sb.Append($"\"description\":\"{EscapeJsonString(tool.InputSchema?.Description ?? "")}\",");
                
                if (tool.InputSchema != null)
                {
                    sb.Append("\"parameters\":");
                    sb.Append(SerializeToolParameters(tool));
                }
                else
                {
                    sb.Append("\"parameters\":{\"type\":\"object\",\"properties\":{}}");
                }
                
                sb.Append("}}");
            }
            
            sb.Append("],");
        }

        private static string SerializeToolParameters(ToolConfig tool)
        {
            try
            {
                if (tool.InputSchema?.Parameters == null)
                {
                    return "{\"type\":\"object\",\"properties\":{}}";
                }

                StringBuilder sb = new StringBuilder();
                sb.Append("{\"type\":\"object\",\"properties\":{");

                bool first = true;
                foreach (var param in tool.InputSchema.Parameters)
                {
                    if (!first) sb.Append(",");
                    
                    sb.Append($"\"{param.Name}\":{{");
                    sb.Append($"\"type\":\"{param.Type}\",");
                    sb.Append($"\"description\":\"{EscapeJsonString(param.Description ?? "")}\"");
                    
                    if (param.Enum != null && param.Enum.Length > 0)
                    {
                        sb.Append(",\"enum\":[");
                        for (int i = 0; i < param.Enum.Length; i++)
                        {
                            if (i > 0) sb.Append(",");
                            sb.Append($"\"{EscapeJsonString(param.Enum[i])}\"");
                        }
                        sb.Append("]");
                    }
                    
                    sb.Append("}");
                    first = false;
                }

                sb.Append("}");

                if (tool.InputSchema.Required != null && tool.InputSchema.Required.Length > 0)
                {
                    sb.Append(",\"required\":[");
                    for (int i = 0; i < tool.InputSchema.Required.Length; i++)
                    {
                        if (i > 0) sb.Append(",");
                        sb.Append($"\"{tool.InputSchema.Required[i]}\"");
                    }
                    sb.Append("]");
                }

                sb.Append("}");
                return sb.ToString();
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"[OpenAIService] Failed to serialize tool parameters: {ex.Message}");
                return "{\"type\":\"object\",\"properties\":{}}";
            }
        }

        private static string GetSystemPromptFromConfig(VoiceAgentConfig agentConfig)
        {
            return agentConfig?.SystemPrompt?.SystemPrompt ?? 
                   "You are a helpful voice assistant with tool capabilities.";
        }

        private static float GetTemperatureFromConfig(VoiceAgentConfig agentConfig)
        {
            return agentConfig?.ModelConfig?.Temperature ?? 1.0f;
        }

        #endregion

        #region Existing Chat API Methods

        private static UnityWebRequest CreateWebRequest(string jsonPayload, string apiKey, string baseUrl)
        {
            UnityWebRequest webRequest = new UnityWebRequest(baseUrl, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            
            webRequest.SetRequestHeader("Content-Type", "application/json");
            webRequest.SetRequestHeader("Authorization", $"Bearer {apiKey}");
            
            return webRequest;
        }
        
        private static string BuildOpenAIPayload(LLMRequest request)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("{");
            stringBuilder.Append($"\"model\":\"{request.model}\",");
            stringBuilder.Append($"\"temperature\":{request.temperature.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)}");
            
            List<Message> filteredMessages = FilterMessagesForOpenAI(request.messages);
            AppendMessages(stringBuilder, filteredMessages);
            AppendTools(stringBuilder, request.tools);
            stringBuilder.Append("}");
            
            return stringBuilder.ToString();
        }
        
        private static List<Message> FilterMessagesForOpenAI(List<Message> messages)
        {
            List<Message> filtered = new List<Message>();
            
            for (int i = 0; i < messages.Count; i++)
            {
                Message msg = messages[i];
                
                if (msg.role == MessageRole.System && IsToolDebugMessage(msg.content))
                {
                    continue;
                }
                
                filtered.Add(msg);
            }
            
            return filtered;
        }
        
        private static bool IsToolDebugMessage(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                return false;
            }
                
            return content.StartsWith("🔧 Tool Executed:") || content.StartsWith("❌ Tool Error:");
        }
        
        private static void AppendMessages(StringBuilder sb, List<Message> messages)
        {
            sb.Append(",\"messages\":[");
            
            for (int i = 0; i < messages.Count; i++)
            {
                if (i > 0) sb.Append(",");
                Message msg = messages[i];
                sb.Append("{");
                sb.Append($"\"role\":\"{GetOpenAIRole(msg.role)}\",");
                sb.Append($"\"content\":\"{EscapeJsonString(msg.content)}\"");
                
                if (msg.role == MessageRole.Assistant && msg.toolCalls != null && msg.toolCalls.Count > 0)
                {
                    AppendFunctionToolCalls(sb, msg.toolCalls);
                }
                
                if (msg.role == MessageRole.Tool)
                {
                    sb.Append($",\"tool_call_id\":\"{msg.toolCallId}\"");
                }
                
                sb.Append("}");
            }
            sb.Append("]");
        }
        
        private static void AppendFunctionToolCalls(StringBuilder sb, List<ToolCall> toolCalls)
        {
            sb.Append(",\"tool_calls\":[");
            
            for (int i = 0; i < toolCalls.Count; i++)
            {
                if (i > 0) sb.Append(",");
                ToolCall toolCall = toolCalls[i];
                sb.Append("{");
                sb.Append($"\"id\":\"{toolCall.id}\",");
                sb.Append("\"type\":\"function\",");
                sb.Append("\"function\":{");
                sb.Append($"\"name\":\"{toolCall.name}\",");
                sb.Append($"\"arguments\":\"{EscapeJsonString(SerializeArguments(toolCall.arguments))}\"");
                sb.Append("}}");
            }
            
            sb.Append("]");
        }
        
        private static string SerializeArguments(Dictionary<string, object> arguments)
        {
            if (arguments == null || arguments.Count == 0)
            {
                return "{}";
            }
                
            StringBuilder sb = new StringBuilder();
            
            sb.Append("{");
            
            bool first = true;
            
            foreach (KeyValuePair<string, object> kvp in arguments)
            {
                if (!first)
                {
                    sb.Append(",");
                }
                
                sb.Append($"\"{kvp.Key}\":");
                
                if (kvp.Value is string)
                {
                    sb.Append($"\"{EscapeJsonString(kvp.Value.ToString())}\"");
                }
                else if (kvp.Value is bool)
                {
                    sb.Append(kvp.Value.ToString().ToLower());
                }
                else if (kvp.Value is int || kvp.Value is float || kvp.Value is double)
                {
                    sb.Append(kvp.Value);
                }
                else
                {
                    sb.Append($"\"{EscapeJsonString(kvp.Value?.ToString() ?? "")}\"");
                }
                
                first = false;
            }
            
            sb.Append("}");
            
            return sb.ToString();
        }
        
        private static void AppendTools(StringBuilder sb, List<ToolConfiguration> tools)
        {
            if (tools != null && tools.Count > 0)
            {
                sb.Append(",\"tools\":[");
                
                for (int i = 0; i < tools.Count; i++)
                {
                    if (i > 0) sb.Append(",");
                    sb.Append(tools[i].ToOpenAIFormat());
                }
                
                sb.Append("]");
                sb.Append(",\"tool_choice\":\"auto\"");
            }
        }
        
        private static LLMResponse ParseOpenAIResponse(string responseText, string model)
        {
            try
            {
                LoggingService.LogDebug($"OpenAI response: {responseText}");

                OpenAIResponse response = JsonUtility.FromJson<OpenAIResponse>(responseText);
                
                string content = ExtractContent(response);
                List<ToolCall> toolCalls = ExtractToolCalls(response);
                int outputTokens = ExtractTokenCount(response);
                
                bool success = HasValidResponse(content, toolCalls);
                
                return CreateSuccessResponse(content, toolCalls, model, outputTokens, success);
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Failed to parse OpenAI response: {ex.Message}");
                return CreateErrorResponse(model, "Failed to parse API response");
            }
        }
        
        private static string ExtractContent(OpenAIResponse response)
        {
            if (response.choices != null && response.choices.Count > 0)
            {
                string messageContent = response.choices[0].message.content;
                if (!string.IsNullOrWhiteSpace(messageContent) && messageContent != "null")
                {
                    return UnescapeJsonString(messageContent.Trim());
                }
            }
            return string.Empty;
        }
        
        private static List<ToolCall> ExtractToolCalls(OpenAIResponse response)
        {
            if (response.choices == null || response.choices.Count == 0)
            {
                return null;
            }
            
            List<OpenAIToolCall> apiToolCalls = response.choices[0].message.tool_calls;
            if (apiToolCalls == null || apiToolCalls.Count == 0)
            {
                return null;
            }
            
            List<ToolCall> toolCalls = new List<ToolCall>();
            
            foreach (OpenAIToolCall apiToolCall in apiToolCalls)
            {
                try
                {
                    Dictionary<string, object> args = SimpleJsonParser.ParseArguments(apiToolCall.function.arguments);
                    
                    toolCalls.Add(new ToolCall(apiToolCall.function.name, args)
                    {
                        id = apiToolCall.id
                    });
                }
                catch (Exception ex)
                {
                    LoggingService.LogError($"Failed to parse tool call arguments: {ex.Message}");
                }
            }
            return toolCalls;
        }
        
        private static int ExtractTokenCount(OpenAIResponse response)
        {
            return response.usage.completion_tokens;
        }
        
        private static bool HasValidResponse(string content, List<ToolCall> toolCalls)
        {
            return !string.IsNullOrWhiteSpace(content) || (toolCalls != null && toolCalls.Count > 0);
        }
        
        private static LLMResponse CreateSuccessResponse(string content, List<ToolCall> toolCalls, string model, int outputTokens, bool success)
        {
            return new LLMResponse
            {
                content = content,
                toolCalls = toolCalls,
                model = model,
                outputTokens = outputTokens,
                success = success,
                timestamp = DateTime.UtcNow
            };
        }
        
        private static string GetOpenAIRole(MessageRole role)
        {
            switch (role)
            {
                case MessageRole.User: return "user";
                case MessageRole.Assistant: return "assistant";
                case MessageRole.System: return "system";
                case MessageRole.Tool: return "tool";
                
                default: return "user";
            }
        }
        
        private static string EscapeJsonString(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }
            
            return input
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }
        
        private static string UnescapeJsonString(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }
            
            return input
                .Replace("\\\\", "\\")
                .Replace("\\\"", "\"")
                .Replace("\\n", "\n")
                .Replace("\\r", "\r")
                .Replace("\\t", "\t");
        }
        
        private static LLMResponse CreateErrorResponse(string model, string error)
        {
            return new LLLResponse
            {
                content = $"Error: {error}",
                model = model,
                success = false,
                timestamp = DateTime.UtcNow
            };
        }
        
        private static async Task SendWebRequestAsync(UnityWebRequest request)
        {
            UnityWebRequestAsyncOperation operation = request.SendWebRequest();
            
            while (!operation.isDone)
            {
                await Task.Yield();
            }
        }

        #endregion
    }
}