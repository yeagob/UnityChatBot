using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Networking;
using UnityEngine;
using ChatSystem.Models.LLM;
using ChatSystem.Models.Context;
using ChatSystem.Models.Tools;
using ChatSystem.Services.Logging;
using ChatSystem.Enums;

namespace ChatSystem.Services.LLM
{
    public class OpenAIService
    {
        private const string DEFAULT_BASE_URL = "https://api.openai.com/v1/chat/completions";
        
        public static async Task<LLMResponse> CompleteChatAsync(LLMRequest request, string apiKey, string baseUrl = DEFAULT_BASE_URL)
        {
            try
            {
                LoggingService.LogInfo($"Making OpenAI API call to model: {request.model}");
                
                string jsonPayload = BuildOpenAIPayload(request);
                
                UnityWebRequest webRequest = new UnityWebRequest(baseUrl, "POST");
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
                webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                
                webRequest.SetRequestHeader("Content-Type", "application/json");
                webRequest.SetRequestHeader("Authorization", $"Bearer {apiKey}");
                
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
                    LoggingService.Error(error);
                    return CreateErrorResponse(request.model, error);
                }
            }
            catch (Exception ex)
            {
                LoggingService.Error($"OpenAI API Exception: {ex.Message}");
                return CreateErrorResponse(request.model, ex.Message);
            }
        }
        
        private static string BuildOpenAIPayload(LLMRequest request)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            sb.Append($"\"model\":\"{request.model}\",");
            sb.Append($"\"temperature\":{request.temperature},");
            sb.Append($"\"max_tokens\":{request.maxTokens},");
            
            sb.Append("\"messages\":[");
            for (int i = 0; i < request.messages.Count; i++)
            {
                if (i > 0) sb.Append(",");
                Message msg = request.messages[i];
                sb.Append("{");
                sb.Append($"\"role\":\"{GetOpenAIRole(msg.role)}\",");
                sb.Append($"\"content\":\"{EscapeJsonString(msg.content)}\"");
                sb.Append("}");
            }
            sb.Append("]");
            
            if (request.tools != null && request.tools.Count > 0)
            {
                sb.Append(",\"tools\":[");
                for (int i = 0; i < request.tools.Count; i++)
                {
                    if (i > 0) sb.Append(",");
                    sb.Append(request.tools[i].ToOpenAIFormat());
                }
                sb.Append("]");
                sb.Append(",\"tool_choice\":\"auto\"");
            }
            
            sb.Append("}");
            return sb.ToString();
        }
        
        private static LLMResponse ParseOpenAIResponse(string responseText, string model)
        {
            try
            {
                OpenAIResponseData response = ParseOpenAIJson(responseText);
                
                string content = string.Empty;
                List<ToolCall> toolCalls = null;
                int outputTokens = 0;
                
                if (response.choices != null && response.choices.Count > 0)
                {
                    OpenAIChoice choice = response.choices[0];
                    content = choice.message?.content ?? string.Empty;
                    
                    if (choice.message?.tool_calls != null && choice.message.tool_calls.Count > 0)
                    {
                        toolCalls = new List<ToolCall>();
                        foreach (OpenAIToolCall toolCall in choice.message.tool_calls)
                        {
                            Dictionary<string, object> args = SimpleJsonParser.ParseArguments(toolCall.function.arguments);
                            toolCalls.Add(new ToolCall(toolCall.function.name, args)
                            {
                                id = toolCall.id
                            });
                        }
                    }
                }
                
                if (response.usage != null)
                {
                    outputTokens = response.usage.completion_tokens;
                }
                
                return new LLMResponse
                {
                    content = content,
                    toolCalls = toolCalls,
                    model = model,
                    outputTokens = outputTokens,
                    success = true,
                    timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                LoggingService.Error($"Failed to parse OpenAI response: {ex.Message}");
                return CreateErrorResponse(model, "Failed to parse API response");
            }
        }
        
        private static OpenAIResponseData ParseOpenAIJson(string json)
        {
            OpenAIResponseData response = new OpenAIResponseData();
            
            int choicesStart = json.IndexOf("\"choices\":[");
            if (choicesStart != -1)
            {
                response.choices = new List<OpenAIChoice>();
                int messageStart = json.IndexOf("\"message\":", choicesStart);
                if (messageStart != -1)
                {
                    OpenAIChoice choice = new OpenAIChoice();
                    choice.message = new OpenAIMessage();
                    
                    int contentStart = json.IndexOf("\"content\":\"", messageStart);
                    if (contentStart != -1)
                    {
                        contentStart += 11;
                        int contentEnd = json.IndexOf("\",", contentStart);
                        if (contentEnd == -1) contentEnd = json.IndexOf("\"}", contentStart);
                        if (contentEnd != -1)
                        {
                            choice.message.content = json.Substring(contentStart, contentEnd - contentStart);
                        }
                    }
                    
                    int toolCallsStart = json.IndexOf("\"tool_calls\":[", messageStart);
                    if (toolCallsStart != -1)
                    {
                        choice.message.tool_calls = ParseToolCalls(json, toolCallsStart);
                    }
                    
                    response.choices.Add(choice);
                }
            }
            
            int usageStart = json.IndexOf("\"usage\":{");
            if (usageStart != -1)
            {
                response.usage = new OpenAIUsage();
                int completionTokensStart = json.IndexOf("\"completion_tokens\":", usageStart);
                if (completionTokensStart != -1)
                {
                    completionTokensStart += 20;
                    int tokenEnd = json.IndexOf(",", completionTokensStart);
                    if (tokenEnd == -1) tokenEnd = json.IndexOf("}", completionTokensStart);
                    if (tokenEnd != -1)
                    {
                        string tokenStr = json.Substring(completionTokensStart, tokenEnd - completionTokensStart);
                        if (int.TryParse(tokenStr, out int tokens))
                        {
                            response.usage.completion_tokens = tokens;
                        }
                    }
                }
            }
            
            return response;
        }
        
        private static List<OpenAIToolCall> ParseToolCalls(string json, int startIndex)
        {
            List<OpenAIToolCall> toolCalls = new List<OpenAIToolCall>();
            
            int currentIndex = startIndex + 14; // Skip "tool_calls":[
            int bracketCount = 0;
            bool inToolCall = false;
            int toolCallStart = -1;
            
            for (int i = currentIndex; i < json.Length; i++)
            {
                char c = json[i];
                
                if (c == '{')
                {
                    if (!inToolCall)
                    {
                        inToolCall = true;
                        toolCallStart = i;
                    }
                    bracketCount++;
                }
                else if (c == '}')
                {
                    bracketCount--;
                    if (bracketCount == 0 && inToolCall)
                    {
                        string toolCallJson = json.Substring(toolCallStart, i - toolCallStart + 1);
                        OpenAIToolCall toolCall = ParseSingleToolCall(toolCallJson);
                        if (toolCall != null)
                        {
                            toolCalls.Add(toolCall);
                        }
                        inToolCall = false;
                    }
                }
                else if (c == ']' && bracketCount == 0)
                {
                    break;
                }
            }
            
            return toolCalls;
        }
        
        private static OpenAIToolCall ParseSingleToolCall(string json)
        {
            OpenAIToolCall toolCall = new OpenAIToolCall();
            toolCall.function = new OpenAIFunction();
            
            int idStart = json.IndexOf("\"id\":\"");
            if (idStart != -1)
            {
                idStart += 6;
                int idEnd = json.IndexOf("\"", idStart);
                if (idEnd != -1)
                {
                    toolCall.id = json.Substring(idStart, idEnd - idStart);
                }
            }
            
            int nameStart = json.IndexOf("\"name\":\"");
            if (nameStart != -1)
            {
                nameStart += 8;
                int nameEnd = json.IndexOf("\"", nameStart);
                if (nameEnd != -1)
                {
                    toolCall.function.name = json.Substring(nameStart, nameEnd - nameStart);
                }
            }
            
            int argsStart = json.IndexOf("\"arguments\":\"");
            if (argsStart != -1)
            {
                argsStart += 13;
                int argsEnd = json.LastIndexOf("\"");
                if (argsEnd > argsStart)
                {
                    toolCall.function.arguments = json.Substring(argsStart, argsEnd - argsStart);
                }
            }
            
            return toolCall;
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
    
    public class OpenAIResponseData
    {
        public List<OpenAIChoice> choices;
        public OpenAIUsage usage;
    }
    
    public class OpenAIChoice
    {
        public OpenAIMessage message;
    }
    
    public class OpenAIMessage
    {
        public string content;
        public List<OpenAIToolCall> tool_calls;
    }
    
    public class OpenAIToolCall
    {
        public string id;
        public OpenAIFunction function;
    }
    
    public class OpenAIFunction
    {
        public string name;
        public string arguments;
    }
    
    public class OpenAIUsage
    {
        public int completion_tokens;
    }
}
