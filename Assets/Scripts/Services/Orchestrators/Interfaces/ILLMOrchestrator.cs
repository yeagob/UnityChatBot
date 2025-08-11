using System.Threading.Tasks;
using ChatSystem.Models.Context;
using ChatSystem.Models.LLM;

namespace ChatSystem.Services.Orchestrators.Interfaces
{
    public interface ILLMOrchestrator
    {
        Task<LLMResponse> ProcessMessageAsync(ConversationContext context);
        void SetAgentConfigurations(AgentConfig[] agentConfigs);
        void AddAgentConfiguration(AgentConfig agentConfig);
        void RemoveAgentConfiguration(string agentId);
        string[] GetActiveAgentIds();
        void ClearAgentConfigurations();
    }
}
