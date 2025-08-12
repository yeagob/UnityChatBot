using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using ChatSystem.Models;
using ChatSystem.Models.Context;
using ChatSystem.Models.Agents;
using ChatSystem.Models.Tools;
using ChatSystem.Configuration.ScriptableObjects;
using ChatSystem.Services.Tools.Interfaces;
using ChatSystem.Services.Agents.Interfaces;
using ChatSystem.Services.Logging;
using ChatSystem.Enums;
using ChatSystem.Models.LLM;

namespace ChatSystem.Services.Agents
{
    public class AgentExecutor : IAgentExecutor
    {
        private readonly Dictionary<string, IToolSet> registeredToolSets;
        private readonly Dictionary<string, AgentConfig> agentConfigs;
        
        public AgentExecutor()
        {
            registeredToolSets = new Dictionary<string, IToolSet>();
            agentConfigs = new Dictionary<string, AgentConfig>();
        }
        
        public async Task<AgentResponse> ExecuteAgentAsync(string agentId, ConversationContext context)
        {
            LoggingService.LogAgentExecution(agentId, "Starting");
            
            if (!agentConfigs.TryGetValue(agentId, out AgentConfig agentConfig))
            {
                LoggingService.LogError($"Agent {agentId} not found");
                return CreateErrorResponse(agentId, "Agent configuration not found");
            }
            
            if (!agentConfig.enabled)
            {
                LoggingService.LogWarning($"Agent {agentId} is disabled");
                return CreateErrorResponse(agentId, "Agent is disabled");
            }
            
            try
            {
                LLMRequest request = BuildLLMRequest(agentConfig, context);
                LLMResponse llmResponse = await SimulateLLMCallAsync(request);
                
                if (llmResponse.toolCalls != null && llmResponse.toolCalls.Count > 0)
                {
                    List<ToolResponse> toolResponses = await ExecuteToolCallsAsync(
                        llmResponse.toolCalls, agentConfig.maxToolCalls);
                    
                    context.AddToolMessages(toolResponses);
                    
                    LLMRequest followUpRequest = BuildLLMRequest(agentConfig, context);
                    llmResponse = await SimulateLLMCallAsync(followUpRequest);
                }
                
                LoggingService.LogAgentExecution(agentId, "Completed");
                
                return new AgentResponse
                {
                    agentId = agentId,
                    content = llmResponse.content,
                    toolCalls = llmResponse.toolCalls,
                    success = true,
                    timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Agent {agentId} execution failed: {ex.Message}");
                return CreateErrorResponse(agentId, ex.Message);
            }
        }
        
        public void RegisterAgent(AgentConfig agentConfig)
        {
            if (agentConfig == null || string.IsNullOrEmpty(agentConfig.agentId))
            {
                LoggingService.LogError("Invalid agent configuration");
                return;
            }
            
            agentConfigs[agentConfig.agentId] = agentConfig;
            LoggingService.LogInfo($"Agent {agentConfig.agentId} registered");
        }
        
        public void RegisterToolSet(IToolSet toolSet)
        {
            if (toolSet == null)
            {
                LoggingService.LogError("Cannot register null ToolSet");
                return;
            }
            
            string toolSetName = toolSet.GetType().Name;
            registeredToolSets[toolSetName] = toolSet;
            LoggingService.LogInfo($"ToolSet {toolSetName} registered with {toolSet.GetAvailableTools().Count} tools");
        }
        
        public void UnregisterToolSet(string toolSetName)
        {
            if (registeredToolSets.Remove(toolSetName))
            {
                LoggingService.LogInfo($"ToolSet {toolSetName} unregistered");
            }
        }
        
        public List<string> GetRegisteredToolSets()
        {
            return new List<string>(registeredToolSets.Keys);
        }
        
        private LLMRequest BuildLLMRequest(AgentConfig agentConfig, ConversationContext context)
        {
            List<Message> messages = new List<Message>();
            
            if (agentConfig.systemPrompt != null)
            {
                messages.Add(new Message
                {
                    role = MessageRole.System,
                    content = agentConfig.GetFullSystemPrompt(),
                    timestamp = DateTime.UtcNow
                });
            }
            
            messages.AddRange(context.GetAllMessages());
            
            return new LLMRequest
            {
                messages = messages,
                tools = agentConfig.GetToolDefinitions(),
                maxTokens = agentConfig.maxResponseTokens,
                temperature = agentConfig.modelConfig?.temperature ?? 0.7f,
                model = agentConfig.modelConfig?.modelName ?? "default",
                provider = agentConfig.modelConfig?.provider ?? ServiceProvider.Custom
            };
        }
        
        private async Task<LLMResponse> SimulateLLMCallAsync(LLMRequest request)
        {
            await Task.Delay(1000);
            
            bool shouldCallTool = UnityEngine.Random.Range(0f, 1f) > 0.5f && 
                                 request.tools != null && request.tools.Count > 0;
            
            List<ToolCall> toolCalls = null;
            
            if (shouldCallTool)
            {
                string toolToCall = request.tools[UnityEngine.Random.Range(0, request.tools.Count)];
                
                toolCalls = new List<ToolCall>
                {
                    new ToolCall
                    {
                        id = Guid.NewGuid().ToString(),
                        name = ExtractToolName(toolToCall),
                        arguments = "{}"
                    }
                };
                
                LoggingService.LogToolCall(toolCalls[0].name, "{}");
            }
            
            return new LLMResponse
            {
                content = toolCalls != null ? 
                    "I'll help you with that request." : 
                    GenerateSimulatedResponse(request),
                toolCalls = toolCalls,
                provider = request.provider,
                model = request.model,
                timestamp = DateTime.UtcNow,
                totalTokens = UnityEngine.Random.Range(100, 500)
            };
        }
        
        private string ExtractToolName(string toolDefinition)
        {
            int nameStart = toolDefinition.IndexOf("\"name\": \"") + 10;
            int nameEnd = toolDefinition.IndexOf("\"", nameStart);
            return nameEnd > nameStart ? 
                toolDefinition.Substring(nameStart, nameEnd - nameStart) : "unknown_tool";
        }
        
        private string GenerateSimulatedResponse(LLMRequest request)
        {
            List<string> responses = new List<string>
            {
                "I understand your request and I'm here to help.",
                "Based on the context provided, here's my response.",
                "Let me assist you with that.",
                "I've analyzed your request and here's what I found."
            };
            
            return responses[UnityEngine.Random.Range(0, responses.Count)];
        }
        
        private async Task<List<ToolResponse>> ExecuteToolCallsAsync(List<ToolCall> toolCalls, int maxCalls)
        {
            List<ToolResponse> responses = new List<ToolResponse>();
            int callsToExecute = Math.Min(toolCalls.Count, maxCalls);
            
            for (int i = 0; i < callsToExecute; i++)
            {
                ToolCall call = toolCalls[i];
                
                try
                {
                    string result = await ExecuteToolAsync(call.name, call.arguments);
                    
                    responses.Add(new ToolResponse
                    {
                        toolCallId = call.id,
                        content = result,
                        success = true,
                        timestamp = DateTime.UtcNow
                    });
                    
                    LoggingService.LogToolResponse(call.name, "Success");
                }
                catch (Exception ex)
                {
                    responses.Add(new ToolResponse
                    {
                        toolCallId = call.id,
                        content = ex.Message,
                        success = false,
                        timestamp = DateTime.UtcNow
                    });
                    
                    LoggingService.LogToolResponse(call.name, "Error: " + ex.Message);
                }
            }
            
            return responses;
        }
        
        private async Task<string> ExecuteToolAsync(string toolName, string arguments)
        {
            foreach (IToolSet toolSet in registeredToolSets.Values)
            {
                if (toolSet.HasTool(toolName))
                {
                    return await toolSet.ExecuteToolAsync(toolName, arguments);
                }
            }
            
            throw new InvalidOperationException($"Tool {toolName} not found in any registered ToolSet");
        }
        
        private AgentResponse CreateErrorResponse(string agentId, string error)
        {
            return new AgentResponse
            {
                agentId = agentId,
                content = $"Error: {error}",
                success = false,
                timestamp = DateTime.UtcNow
            };
        }
    }
}