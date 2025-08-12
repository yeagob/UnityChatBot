using System.Threading.Tasks;
using ChatSystem.Models.LLM;

namespace ChatSystem.Services.Orchestrators.Interfaces
{
    public interface IChatOrchestrator
    {
        Task<LLMResponse> ProcessUserMessageAsync(string conversationId, string userMessage);
        string GetConversationContext(string conversationId);
        void ClearConversation(string conversationId);
    }
}
