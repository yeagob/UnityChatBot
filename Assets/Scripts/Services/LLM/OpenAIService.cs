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
                LoggingService.LogInfo($"Making OpenAI API call to model: {request.model}");
                
                string jsonPayload = BuildOpenAIPayload(request);
                
                LoggingService.LogDebug($"Making OpenAI API call with PAYLOAD: {jsonPayload}");
                
                UnityWebRequest webRequest = CreateWebRequest(jsonPayload, apiKey, baseUrl);
                
                await SendWebRequestAsync(webRequest);
                
                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    string responseText = webRequest.downloadHandler.text;
                    LoggingService.LogInfo("OpenAI API call successful");
                    return ParseOpenAIResponse(responseText, request.model);
                }
                else
                {
                    string error = $"OpenAI API Error: {webRequest.error} - {webRequest.downloadHandler.text}";
                    LoggingService.LogError(error);
                    return CreateErrorResponse(request.model, error);
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"OpenAI API Exception: {ex.Message}");
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
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            sb.Append($"\"model\":\"{request.model}\",");
            sb.Append($"\"temperature\":{request.temperature.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)}");
            
            AppendMessages(sb, request.messages);
            AppendTools(sb, request.tools);
            
            sb.Append("}");
            return sb.ToString();
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
                sb.Append("}");
            }
            sb.Append("]");
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
                LoggingService.LogDebug($" OpenAI response: {responseText}");

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
            if (response.choices == null || response.choices.Count == 0) return null;
            
            List<OpenAIToolCall> apiToolCalls = response.choices[0].message.tool_calls;
            if (apiToolCalls == null || apiToolCalls.Count == 0) return null;
            
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
        
        private static string GetFallbackResponse()
        {
            LoggingService.LogWarning("OpenAI response has no valid content or tool calls - applying fallback");
            return "¡Hola Santiago! Me encanta que quieras viajar a China. Es un destino fascinante con una cultura milenaria. ¿Hay alguna región específica de China que te interese más? ¿Prefieres ciudades como Beijing y Shanghai, o te llama más la atención algo como la Gran Muralla o los paisajes de Guilin?";
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
            if (string.IsNullOrEmpty(input)) return string.Empty;
            
            return input
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }
        
        private static string UnescapeJsonString(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            
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
