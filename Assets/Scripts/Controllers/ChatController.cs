using System;
using System.Threading.Tasks;
using UnityEngine;
using ChatSystem.Controllers.Interfaces;
using ChatSystem.Models.Context;
using ChatSystem.Views.Interfaces;
using ChatSystem.Services.Orchestrators.Interfaces;

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
        }

        public async Task ProcessUserMessageAsync(string messageText)
        {
            if (string.IsNullOrWhiteSpace(messageText) || currentContext == null)
                return;

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
                await ProcessWithSimulation(messageText);
            }
        }

        private async Task ProcessWithOrchestrator(string messageText)
        {
            try
            {
                var response = await chatOrchestrator.ProcessUserMessageAsync(
                    currentContext.conversationId, 
                    messageText
                );

                if (response.success && !string.IsNullOrEmpty(response.content))
                {
                    currentContext.AddAssistantMessage(response.content);
                    NotifyResponseTarget(GetLastMessage());
                    LogAssistantResponse(response.content);
                }
                else
                {
                    HandleOrchestratorError(response.errorMessage);
                }
            }
            catch (Exception ex)
            {
                HandleProcessingError(ex);
            }
        }

        private async Task ProcessWithSimulation(string messageText)
        {
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
            LogError(fullError);
        }

        private void HandleProcessingError(Exception ex)
        {
            string errorMessage = $"Error processing message: {ex.Message}";
            responseTarget?.ReceiveError(errorMessage);
            LogError(errorMessage);
        }

        private void InitializeController()
        {
            InitializeConversation(defaultConversationId);
            LogControllerInitialized();
        }

        private void LogConversationInitialized(string conversationId)
        {
            Debug.Log($"[ChatController] Conversation initialized: {conversationId}");
        }

        private void LogUserMessage(string message)
        {
            Debug.Log($"[ChatController] User message received: {message}");
        }

        private void LogAssistantResponse(string response)
        {
            Debug.Log($"[ChatController] Assistant response generated: {response}");
        }

        private void LogControllerInitialized()
        {
            Debug.Log("[ChatController] Chat Controller initialized");
        }

        private void LogError(string errorMessage)
        {
            Debug.LogError($"[ChatController] ERROR: {errorMessage}");
        }
    }
}
