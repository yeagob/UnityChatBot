using System;
using System.Threading.Tasks;
using UnityEngine;
using ChatSystem.Controllers.Interfaces;
using ChatSystem.Models.Context;
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
            LogConversationInitialized(conversationId);
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
            
            LoggingService.LogDebug($"[ChatController] Checking orchestrator availability. Is null: {chatOrchestrator == null}");
            
            if (chatOrchestrator != null)
            {
                LoggingService.LogInfo("[ChatController] Using orchestrator path");
                await ProcessWithOrchestrator(messageText);
            }
            else
            {
                LoggingService.LogWarning("[ChatController] ChatOrchestrator is null - falling back to simulation");
                await ProcessWithSimulation(messageText);
            }
        }

        private async Task ProcessWithOrchestrator(string messageText)
        {
            try
            {
                LoggingService.LogDebug($"[ChatController] Starting orchestrator processing for message: {messageText}");
                
                var response = await chatOrchestrator.ProcessUserMessageAsync(
                    currentContext.conversationId, 
                    messageText
                );

                LoggingService.LogDebug($"[ChatController] Orchestrator response received. Success: {response.success}, Content empty: {string.IsNullOrEmpty(response.content)}, Error: {response.errorMessage}");

                if (response.success && !string.IsNullOrEmpty(response.content))
                {
                    currentContext.AddAssistantMessage(response.content);
                    NotifyResponseTarget(GetLastMessage());
                    LogAssistantResponse(response.content);
                    LoggingService.LogInfo("[ChatController] Successfully processed message with orchestrator");
                }
                else
                {
                    string errorMsg = response.errorMessage ?? "Unknown orchestrator error";
                    LoggingService.LogError($"[ChatController] Orchestrator failed. Success: {response.success}, Error: {errorMsg}");
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

        private async Task ProcessWithSimulation(string messageText)
        {
            LoggingService.LogInfo("[ChatController] Starting simulation processing");
            await SimulateProcessing();
            await GenerateSimulatedResponse(messageText);
        }

        private void AddUserMessageToContext(string messageText)
        {
            currentContext.AddUserMessage(messageText);
            
            Message userMessage = GetLastMessage();
            NotifyResponseTarget(userMessage);
            
            LogUserMessage(messageText);
        }

        private async Task SimulateProcessing()
        {
            await Task.Delay(1000);
        }

        private async Task GenerateSimulatedResponse(string userMessage)
        {
            string simulatedResponse = CreateSimulatedResponse(userMessage);
            
            currentContext.AddAssistantMessage(simulatedResponse);
            
            Message assistantMessage = GetLastMessage();
            NotifyResponseTarget(assistantMessage);
            
            LogAssistantResponse(simulatedResponse);
            
            await Task.CompletedTask;
        }

        private string CreateSimulatedResponse(string userMessage)
        {
            return $"I received your message: '{userMessage}'. This is a simulated response from the assistant.";
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

        private void LogConversationInitialized(string conversationId)
        {
            LoggingService.LogInfo($"[ChatController] Conversation initialized: {conversationId}");
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
