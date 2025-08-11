using System.Collections.Generic;
using System.Threading.Tasks;
using ChatSystem.Models.Agents;
using ChatSystem.Models.Context;
using ChatSystem.Models.LLM;
using ChatSystem.Services.Tools.Interfaces;

namespace ChatSystem.Services.Agents.Interfaces
{
    public interface IAgentExecutor
    {
        Task<AgentResponse> ExecuteAgentAsync(Agent agent, ConversationContext context);
        Task<LLMResponse> CallLLMAsync(LLMRequest request);
        Task<bool> ValidateAgentAsync(Agent agent);
        void RegisterToolSet(IToolSet toolSet);
        void UnregisterToolSet(string toolSetId);
        List<IToolSet> GetRegisteredToolSets();
        List<IToolSet> GetToolSetsForAgent(Agent agent);
    }
}
