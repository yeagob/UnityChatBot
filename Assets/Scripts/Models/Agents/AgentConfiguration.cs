using System;
using System.Collections.Generic;
using ChatSystem.Models.LLM;

namespace ChatSystem.Models.Agents
{
    [Serializable]
    public class AgentConfiguration
    {
        public string agentId;
        public string agentName;
        public LLMConfiguration llmConfig;
        public List<string> toolSetIds;
        public bool executeBeforeUser;
        public bool executeAfterUser;
        public int executionOrder;
        public bool isEnabled;
        public string promptId;
        
        public AgentConfiguration()
        {
            toolSetIds = new List<string>();
            executeAfterUser = true;
            executionOrder = 0;
            isEnabled = true;
        }
        
        public AgentConfiguration(string id, string name) : this()
        {
            agentId = id;
            agentName = name;
        }
        
        public static AgentConfiguration FromScriptableObject(AgentConfig config)
        {
            var toolIds = new List<string>();
            if (config.ToolConfigs != null)
            {
                foreach (var toolConfig in config.ToolConfigs)
                {
                    if (toolConfig != null)
                    {
                        toolIds.Add(toolConfig.ToolId);
                    }
                }
            }
            
            return new AgentConfiguration
            {
                agentId = config.AgentId,
                agentName = config.AgentName,
                llmConfig = new LLMConfiguration
                {
                    token = config.Token,
                    model = config.Model,
                    serviceProvider = config.ServiceProvider,
                    serviceUrl = config.ServiceUrl,
                    inputTokenCost = config.InputTokenCost,
                    outputTokenCost = config.OutputTokenCost
                },
                toolSetIds = toolIds
            };
        }
    }
}
