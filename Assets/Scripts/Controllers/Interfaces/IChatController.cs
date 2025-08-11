using System.Threading.Tasks;
using ChatSystem.Models.Context;
using ChatSystem.Services.Orchestrators.Interfaces;

namespace ChatSystem.Controllers.Interfaces
{
    public interface IChatController
    {
        Task ProcessUserMessageAsync(string message);
        void InitializeConversation(string conversationId);
        void SetResponseTarget(Views.Interfaces.IResponsable target);
        void SetChatOrchestrator(IChatOrchestrator orchestrator);
        ConversationContext GetCurrentContext();
        void ClearConversation();
    }
}
