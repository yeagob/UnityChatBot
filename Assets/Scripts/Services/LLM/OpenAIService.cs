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
                    //Extra tool info?
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
            return new LLMResponse
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
    }
}
