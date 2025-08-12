using System.Collections.Generic;
using System.Threading.Tasks;
using ChatSystem.Models;
using ChatSystem.Models.Context;
using ChatSystem.Configuration.ScriptableObjects;

namespace ChatSystem.Services.Orchestrators.Interfaces
{
    public interface ILLMOrchestrator
    {
        Task<LLMResponse> ProcessMessageAsync(ConversationContext context);
        void RegisterAgentConfig(AgentConfig config);
        List<string> GetActiveAgents();
        void EnableAgent(string agentId);
        void DisableAgent(string agentId);
    }
}