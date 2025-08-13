using System;
using System.Threading.Tasks;
using UnityEngine;
using ChatSystem.Controllers.Interfaces;
using ChatSystem.Models.Context;
using ChatSystem.Models.LLM;
using ChatSystem.Views.Interfaces;
using ChatSystem.Services.Orchestrators.Interfaces;
using ChatSystem.Services.Logging;

namespace ChatSystem.Controllers
{
    public class ChatController : IChatController
    {
        private string defaultConversationId;
        private ConversationContext currentContext;
        private IResponsable responseTarget;
        private IChatOrchestrator chatOrchestrator;

        public ChatController(string conversationId = "default-conversation")
        {
            defaultConversationId = conversationId;
            InitializeController();
        }

        public void SetChatOrchestrator(IChatOrchestrator orchestrator)
        {
            chatOrchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
            LoggingService.LogInfo($"[ChatController] ChatOrchestrator set successfully: {orchestrator.GetType().Name}");
        }

        public void InitializeConversation(string conversationId)
        {
            if (string.IsNullOrWhiteSpace(conversationId))
            {
                conversationId = Guid.NewGuid().ToString();
            }

            currentContext = new ConversationContext(conversationId);
        }

        public void SetResponseTarget(IResponsable target)
        {
            responseTarget = target;
            LoggingService.LogDebug($"[ChatController] Response target set: {target?.GetType().Name ?? "null"}");
        }

        public async Task ProcessUserMessageAsync(string messageText)
        {
            if (string.IsNullOrWhiteSpace(messageText) || currentContext == null)
            {
                LoggingService.LogWarning("[ChatController] Invalid message or context is null");
                return;
            }

            try
            {
                await HandleUserMessage(messageText);
            }
            catch (Exception ex)
            {
                HandleProcessingError(ex);
            }
        }

        public ConversationContext GetCurrentContext()
        {
            return currentContext;
        }

        public void ClearConversation()
        {
            if (currentContext != null)
            {
                InitializeConversation(currentContext.conversationId);
            }
        }

        private async Task HandleUserMessage(string messageText)
        {
            AddUserMessageToContext(messageText);
            
            if (chatOrchestrator != null)
            {
                await ProcessWithOrchestrator(messageText);
            }
            else
            {
                LoggingService.LogError($"[ChatController] ChatOrchestrator is null");
            }
        }

        private async Task ProcessWithOrchestrator(string messageText)
        {
            try
            {
                LLMResponse response = await chatOrchestrator.ProcessUserMessageAsync(
                    currentContext.conversationId, 
                    messageText
                );

                LoggingService.LogDebug($"[ChatController] Response received! Success: {response.success}");

                if (response.success && !string.IsNullOrEmpty(response.content))
                {
                    currentContext.AddAssistantMessage(response.content);
                    NotifyResponseTarget(GetLastMessage());
                    LogAssistantResponse(response.content);
                }
                else
                {
                    string errorMsg = response.errorMessage ?? "Unknown orchestrator error";
                    LoggingService.LogError($"[ChatController] Orchestrator failed. Error: {errorMsg}");
                    HandleOrchestratorError(errorMsg);
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"[ChatController] Exception in ProcessWithOrchestrator: {ex.Message}");
                LoggingService.LogError($"[ChatController] Stack trace: {ex.StackTrace}");
                HandleProcessingError(ex);
            }
        }

        private void AddUserMessageToContext(string messageText)
        {
            currentContext.AddUserMessage(messageText);
            
            Message userMessage = GetLastMessage();
            NotifyResponseTarget(userMessage);
            
            LogUserMessage(messageText);
        }

        private Message GetLastMessage()
        {
            var messages = currentContext.GetAllMessages();
            return messages[messages.Count - 1];
        }

        private void NotifyResponseTarget(Message message)
        {
            responseTarget?.ReceiveResponse(message);
        }

        private void HandleOrchestratorError(string errorMessage)
        {
            string fullError = $"Orchestrator error: {errorMessage}";
            responseTarget?.ReceiveError(fullError);
            LoggingService.LogError($"[ChatController] {fullError}");
        }

        private void HandleProcessingError(Exception ex)
        {
            string errorMessage = $"Error processing message: {ex.Message}";
            responseTarget?.ReceiveError(errorMessage);
            LoggingService.LogError($"[ChatController] {errorMessage}");
        }

        private void InitializeController()
        {
            InitializeConversation(defaultConversationId);
            LogControllerInitialized();
        }

        private void LogUserMessage(string message)
        {
            LoggingService.LogInfo($"[ChatController] User message received: {message}");
        }

        private void LogAssistantResponse(string response)
        {
            LoggingService.LogInfo($"[ChatController] Assistant response generated: {response}");
        }

        private void LogControllerInitialized()
        {
            LoggingService.LogInfo("[ChatController] Chat Controller initialized");
        }
    }
}
