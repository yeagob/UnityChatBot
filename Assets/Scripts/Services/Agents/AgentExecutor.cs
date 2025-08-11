using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChatSystem.Models.Agents;
using ChatSystem.Models.Context;
using ChatSystem.Models.LLM;
using ChatSystem.Models.Tools;
using ChatSystem.Enums;
using ChatSystem.Services.Agents.Interfaces;
using ChatSystem.Services.Tools.Interfaces;
using ChatSystem.Services.Logging;

namespace ChatSystem.Services.Agents
{
    public class AgentExecutor : IAgentExecutor
    {
        private readonly Dictionary<string, IToolSet> toolSets;
        
        public AgentExecutor()
        {
            toolSets = new Dictionary<string, IToolSet>();
            LoggingService.LogInfo("AgentExecutor initialized");
        }
        
        public async Task<AgentResponse> ExecuteAgentAsync(Agent agent, ConversationContext context)
        {
            LoggingService.LogAgentExecution(agent.agentId, "Starting agent execution");
            
            try
            {
                LLMRequest llmRequest = BuildLLMRequest(agent, context);
                LLMResponse llmResponse = await CallLLMAsync(llmRequest);
                
                AgentResponse agentResponse = await ProcessLLMResponse(agent, llmResponse, context);
                
                LoggingService.LogAgentExecution(agent.agentId, "Agent execution completed successfully");
                return agentResponse;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Agent execution failed for {agent.agentId}: {ex.Message}");
                return CreateErrorResponse(agent, ex.Message);
            }
        }
        
        public async Task<LLMResponse> CallLLMAsync(LLMRequest request)
        {
            LoggingService.LogInfo($"Calling LLM service: {request.model}");
            
            await Task.Delay(1000);
            
            string mockResponse = GenerateMockLLMResponse(request);
            List<ToolCall> mockToolCalls = GenerateMockToolCalls(request);
            
            return new LLMResponse
            {
                content = mockResponse,
                toolCalls = mockToolCalls,
                model = request.model,
                usage = new Dictionary<string, object>
                {
                    ["prompt_tokens"] = 150,
                    ["completion_tokens"] = 80,
                    ["total_tokens"] = 230
                },
                timestamp = DateTime.UtcNow
            };
        }
        
        public async Task<bool> ValidateAgentAsync(Agent agent)
        {
            await Task.CompletedTask;
            
            if (string.IsNullOrEmpty(agent.agentId) || string.IsNullOrEmpty(agent.llmConfiguration.model))
                return false;
                
            if (agent.toolSetIds != null)
            {
                foreach (string toolSetId in agent.toolSetIds)
                {
                    if (!toolSets.ContainsKey(toolSetId))
                    {
                        LoggingService.LogWarning($"ToolSet not found: {toolSetId}");
                        return false;
                    }
                }
            }
            
            return true;
        }
        
        public void RegisterToolSet(IToolSet toolSet)
        {
            toolSets[toolSet.ToolSetId] = toolSet;
            LoggingService.LogInfo($"ToolSet registered: {toolSet.ToolSetId}");
        }
        
        public void UnregisterToolSet(string toolSetId)
        {
            if (toolSets.Remove(toolSetId))
            {
                LoggingService.LogInfo($"ToolSet unregistered: {toolSetId}");
            }
        }
        
        public List<IToolSet> GetRegisteredToolSets()
        {
            return toolSets.Values.ToList();
        }
        
        public List<IToolSet> GetToolSetsForAgent(Agent agent)
        {
            if (agent.toolSetIds == null || agent.toolSetIds.Count == 0)
                return new List<IToolSet>();
                
            return agent.toolSetIds
                .Where(id => toolSets.ContainsKey(id))
                .Select(id => toolSets[id])
                .ToList();
        }
        
        private LLMRequest BuildLLMRequest(Agent agent, ConversationContext context)
        {
            List<IToolSet> agentToolSets = GetToolSetsForAgent(agent);
            List<ToolConfiguration> availableTools = agentToolSets
                .SelectMany(ts => ts.GetAvailableTools())
                .ToList();
            
            LoggingService.LogPromptConstruction(agent.agentId, context.GetMessages().Count);
            
            return new LLMRequest
            {
                model = agent.llmConfiguration.model,
                serviceUrl = agent.llmConfiguration.serviceUrl,
                messages = context.GetMessages(),
                tools = availableTools,
                temperature = 0.7f,
                maxTokens = 2000,
                timestamp = DateTime.UtcNow
            };
        }
        
        private async Task<AgentResponse> ProcessLLMResponse(Agent agent, LLMResponse llmResponse, ConversationContext context)
        {
            List<ToolResponse> toolResponses = new List<ToolResponse>();
            
            if (llmResponse.toolCalls != null && llmResponse.toolCalls.Count > 0)
            {
                toolResponses = await ExecuteToolCallsAsync(llmResponse.toolCalls);
                await AddToolResponsesToContext(context, toolResponses);
            }
            
            return new AgentResponse
            {
                agentId = agent.agentId,
                content = llmResponse.content,
                toolCalls = llmResponse.toolCalls ?? new List<ToolCall>(),
                toolResponses = toolResponses,
                state = AgentState.Completed,
                timestamp = DateTime.UtcNow,
                usage = llmResponse.usage
            };
        }
        
        private async Task<List<ToolResponse>> ExecuteToolCallsAsync(List<ToolCall> toolCalls)
        {
            List<ToolResponse> responses = new List<ToolResponse>();
            
            foreach (ToolCall toolCall in toolCalls)
            {
                IToolSet targetToolSet = FindToolSetForTool(toolCall.name);
                
                if (targetToolSet != null)
                {
                    ToolResponse response = await targetToolSet.ExecuteToolAsync(toolCall);
                    responses.Add(response);
                }
                else
                {
                    LoggingService.LogError($"No ToolSet found for tool: {toolCall.name}");
                    responses.Add(CreateToolErrorResponse(toolCall));
                }
            }
            
            return responses;
        }
        
        private async Task AddToolResponsesToContext(ConversationContext context, List<ToolResponse> toolResponses)
        {
            foreach (ToolResponse response in toolResponses)
            {
                Message toolMessage = new Message
                {
                    id = Guid.NewGuid().ToString(),
                    role = MessageRole.Tool,
                    type = MessageType.ToolResponse,
                    content = response.content,
                    toolCallId = response.toolCallId,
                    timestamp = DateTime.UtcNow,
                    conversationId = context.ConversationId
                };
                
                context.AddMessage(toolMessage);
            }
            
            await Task.CompletedTask;
        }
        
        private IToolSet FindToolSetForTool(string toolName)
        {
            return toolSets.Values.FirstOrDefault(ts => ts.IsToolSupported(toolName));
        }
        
        private string GenerateMockLLMResponse(LLMRequest request)
        {
            bool hasTools = request.tools != null && request.tools.Count > 0;
            
            if (hasTools && request.messages.Count > 1)
            {
                string lastMessage = request.messages.Last().content;
                
                if (lastMessage.ToLower().Contains("user") || lastMessage.ToLower().Contains("profile"))
                {
                    return "I'll help you with user management. Let me update your information.";
                }
                
                if (lastMessage.ToLower().Contains("travel") || lastMessage.ToLower().Contains("trip"))
                {
                    return "I'll search for travel options for you. Let me find the best matches.";
                }
            }
            
            return "I understand your request and I'm here to help you with that.";
        }
        
        private List<ToolCall> GenerateMockToolCalls(LLMRequest request)
        {
            if (request.tools == null || request.tools.Count == 0)
                return new List<ToolCall>();
            
            string lastMessage = request.messages.LastOrDefault().content;
            
            if (lastMessage.ToLower().Contains("update") && lastMessage.ToLower().Contains("name"))
            {
                return new List<ToolCall>
                {
                    new ToolCall
                    {
                        id = Guid.NewGuid().ToString(),
                        name = "update_user_name",
                        arguments = new Dictionary<string, object> { ["id"] = "user123", ["name"] = "John Doe" }
                    }
                };
            }
            
            if (lastMessage.ToLower().Contains("travel") || lastMessage.ToLower().Contains("trip"))
            {
                return new List<ToolCall>
                {
                    new ToolCall
                    {
                        id = Guid.NewGuid().ToString(),
                        name = "search_travels_by_country",
                        arguments = new Dictionary<string, object> { ["country"] = "Spain" }
                    }
                };
            }
            
            return new List<ToolCall>();
        }
        
        private AgentResponse CreateErrorResponse(Agent agent, string error)
        {
            return new AgentResponse
            {
                agentId = agent.agentId,
                content = $"Agent execution failed: {error}",
                toolCalls = new List<ToolCall>(),
                toolResponses = new List<ToolResponse>(),
                state = AgentState.Error,
                timestamp = DateTime.UtcNow,
                usage = new Dictionary<string, object>()
            };
        }
        
        private ToolResponse CreateToolErrorResponse(ToolCall toolCall)
        {
            return new ToolResponse
            {
                toolCallId = toolCall.id,
                content = $"Tool execution failed: Tool {toolCall.name} not found",
                success = false,
                responseTimestamp = DateTime.UtcNow
            };
        }
        
       
    }
}
