using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChatSystem.Models.Context;
using ChatSystem.Models.LLM;
using ChatSystem.Models.Agents;
using ChatSystem.Services.Orchestrators.Interfaces;
using ChatSystem.Services.Agents.Interfaces;
using ChatSystem.Services.Logging;
using ChatSystem.Enums;

namespace ChatSystem.Services.Orchestrators
{
    public class LLMOrchestrator : ILLMOrchestrator
    {
        private List<string> activeAgentIds;
        private Dictionary<string, Agent> agentConfigurations;
        private IAgentExecutor agentExecutor;
        
        public LLMOrchestrator()
        {
            activeAgentIds = new List<string>();
            agentConfigurations = new Dictionary<string, Agent>();
            LoggingService.LogInfo("LLMOrchestrator initialized");
        }
        
        public void SetAgentExecutor(IAgentExecutor executor)
        {
            agentExecutor = executor ?? throw new ArgumentNullException(nameof(executor));
            LoggingService.LogDebug("AgentExecutor set in LLMOrchestrator");
        }
        
        public async Task<LLMResponse> ProcessMessageAsync(ConversationContext context)
        {
            ValidateContext(context);
            ValidateAgentExecutor();
            
            if (!HasActiveAgents())
            {
                return CreateNoAgentsResponse();
            }
            
            LLMResponse aggregatedResponse = await ExecuteAgentFlow(context);
            return aggregatedResponse;
        }
        
        public void SetAgentConfigurations(AgentConfig[] agentConfigs)
        {
            activeAgentIds.Clear();
            agentConfigurations.Clear();
            
            if (agentConfigs != null)
            {
                foreach (AgentConfig config in agentConfigs)
                {
                    AddAgentFromConfig(config);
                }
            }
            
            LoggingService.LogDebug($"Agent configurations set: {string.Join(", ", activeAgentIds)}");
        }
        
        public void AddAgentConfiguration(AgentConfig agentConfig)
        {
            if (agentConfig == null)
            {
                throw new ArgumentNullException(nameof(agentConfig));
            }
            
            if (string.IsNullOrWhiteSpace(agentConfig.AgentId))
            {
                throw new ArgumentException("AgentConfig must have a valid AgentId", nameof(agentConfig));
            }
            
            if (!activeAgentIds.Contains(agentConfig.AgentId))
            {
                AddAgentFromConfig(agentConfig);
                LoggingService.LogInfo($"Agent configuration added: {agentConfig.AgentId}");
            }
        }
        
        public void RemoveAgentConfiguration(string agentId)
        {
            activeAgentIds.Remove(agentId);
            agentConfigurations.Remove(agentId);
            LoggingService.LogInfo($"Agent configuration removed: {agentId}");
        }
        
        public string[] GetActiveAgentIds()
        {
            return activeAgentIds.ToArray();
        }
        
        public void ClearAgentConfigurations()
        {
            activeAgentIds.Clear();
            agentConfigurations.Clear();
            LoggingService.LogInfo("All agent configurations cleared");
        }
        
        private void AddAgentFromConfig(AgentConfig config)
        {
            Agent agent = ConvertAgentConfigToAgent(config);
            
            activeAgentIds.Add(agent.agentId);
            agentConfigurations[agent.agentId] = agent;
        }
        
        private Agent ConvertAgentConfigToAgent(AgentConfig config)
        {
            List<string> toolSetIds = new List<string>();
            
            if (config.ToolConfigs != null)
            {
                foreach (ToolConfig toolConfig in config.ToolConfigs)
                {
                    if (!string.IsNullOrWhiteSpace(toolConfig.ToolId))
                    {
                        toolSetIds.Add(toolConfig.ToolId);
                    }
                }
            }
            
            LLMConfiguration llmConfig = new LLMConfiguration
            {
                model = config.Model,
                token = config.Token,
                serviceProvider = config.ServiceProvider,
                serviceUrl = config.ServiceUrl,
                inputTokenCost = config.InputTokenCost,
                outputTokenCost = config.OutputTokenCost
            };
            
            return new Agent
            {
                agentId = config.AgentId,
                agentName = config.AgentName,
                currentState = AgentState.Idle,
                llmConfiguration = llmConfig,
                toolSetIds = toolSetIds,
                createdAt = DateTime.UtcNow,
                lastExecuted = DateTime.MinValue
            };
        }
        
        private void ValidateContext(ConversationContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            
            if (context.GetMessages() == null || context.GetMessages().Count == 0)
            {
                throw new ArgumentException("Context must contain at least one message", nameof(context));
            }
        }
        
        private void ValidateAgentExecutor()
        {
            if (agentExecutor == null)
            {
                throw new InvalidOperationException("AgentExecutor not set. Call SetAgentExecutor first.");
            }
        }
        
        private bool HasActiveAgents()
        {
            return activeAgentIds.Count > 0;
        }
        
        private async Task<LLMResponse> ExecuteAgentFlow(ConversationContext context)
        {
            LoggingService.LogInfo($"Executing agent flow with {activeAgentIds.Count} agents");
            
            LLMResponse aggregatedResponse = CreateInitialResponse();
            ConversationContext currentContext = CloneContext(context);
            
            foreach (string agentId in activeAgentIds)
            {
                Agent agent = agentConfigurations[agentId];
                AgentResponse agentResponse = await ExecuteSingleAgent(agent, currentContext);
                
                MergeAgentResponse(aggregatedResponse, agentResponse);
                UpdateContextWithAgentResponse(currentContext, agentResponse);
            }
            
            FinalizeResponse(aggregatedResponse);
            return aggregatedResponse;
        }
        
        private async Task<AgentResponse> ExecuteSingleAgent(Agent agent, ConversationContext context)
        {
            try
            {
                LoggingService.LogAgentExecution(agent.agentId, "Starting execution");
                return await agentExecutor.ExecuteAgentAsync(agent, context);
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Agent execution failed: {agent.agentId} - {ex.Message}");
                return CreateAgentErrorResponse(agent, ex);
            }
        }
        
        private void MergeAgentResponse(LLMResponse aggregatedResponse, AgentResponse agentResponse)
        {
            if (agentResponse.state == AgentState.Error)
            {
                aggregatedResponse.success = false;
                aggregatedResponse.errorMessage += $"Agent {agentResponse.agentId} failed; ";
                return;
            }
            
            if (!string.IsNullOrEmpty(agentResponse.content))
            {
                aggregatedResponse.content += agentResponse.content + " ";
            }
            
            if (agentResponse.toolCalls != null)
            {
                aggregatedResponse.toolCalls.AddRange(agentResponse.toolCalls);
            }
            
            if (agentResponse.toolResponses != null)
            {
                aggregatedResponse.toolResponses.AddRange(agentResponse.toolResponses);
            }
            
            if (agentResponse.usage != null)
            {
                aggregatedResponse.usage = MergeUsageData(aggregatedResponse.usage, agentResponse.usage);
            }
        }
        
        private void UpdateContextWithAgentResponse(ConversationContext context, AgentResponse agentResponse)
        {
            if (agentResponse.state == AgentState.Completed && !string.IsNullOrEmpty(agentResponse.content))
            {
                context.AddAssistantMessage(agentResponse.content);
            }
            
            if (agentResponse.toolResponses != null)
            {
                foreach (var toolResponse in agentResponse.toolResponses)
                {
                    context.AddToolMessage(toolResponse.content, toolResponse.toolCallId);
                }
            }
        }
        
        private ConversationContext CloneContext(ConversationContext original)
        {
            ConversationContext clone = new ConversationContext(original.ConversationId);
            
            foreach (Message message in original.GetMessages())
            {
                clone.AddMessage(message);
            }
            
            return clone;
        }
        
        private LLMResponse CreateInitialResponse()
        {
            return new LLMResponse
            {
                success = true,
                content = string.Empty,
                promptsUsed = new List<string>(),
                usage = new Dictionary<string, object>(),
                timestamp = DateTime.UtcNow
            };
        }
        
        private void FinalizeResponse(LLMResponse response)
        {
            if (string.IsNullOrEmpty(response.content))
            {
                response.content = "No content generated by agents";
            }
            
            response.content = response.content.Trim();
            LoggingService.LogInfo("Agent flow execution completed");
        }
        
        private Dictionary<string, object> MergeUsageData(Dictionary<string, object> existing, Dictionary<string, object> newUsage)
        {
            if (existing == null) return newUsage ?? new Dictionary<string, object>();
            if (newUsage == null) return existing;
            
            Dictionary<string, object> merged = new Dictionary<string, object>(existing);
            
            foreach (var kvp in newUsage)
            {
                if (merged.ContainsKey(kvp.Key))
                {
                    if (int.TryParse(merged[kvp.Key]?.ToString(), out int existingInt) &&
                        int.TryParse(kvp.Value?.ToString(), out int newInt))
                    {
                        merged[kvp.Key] = existingInt + newInt;
                    }
                }
                else
                {
                    merged[kvp.Key] = kvp.Value;
                }
            }
            
            return merged;
        }
        
        private LLMResponse CreateNoAgentsResponse()
        {
            return new LLMResponse
            {
                success = false,
                errorMessage = "No active agents configured",
                content = "Unable to process message: no agents available",
                timestamp = DateTime.UtcNow
            };
        }
        
        private AgentResponse CreateAgentErrorResponse(Agent agent, Exception exception)
        {
            return new AgentResponse
            {
                agentId = agent.agentId,
                content = string.Empty,
                state = AgentState.Error,
                timestamp = DateTime.UtcNow,
                usage = new Dictionary<string, object>()
            };
        }
    }
}
