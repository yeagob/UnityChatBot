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
            LoggingService.LogInfo("ChatOrchestrator set in ChatController");
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
            LoggingService.LogDebug("Response target set in ChatController");
        }

        public async Task ProcessUserMessageAsync(string messageText)
        {
            if (string.IsNullOrWhiteSpace(messageText) || currentContext == null)
            {
                LoggingService.LogWarning("ProcessUserMessageAsync: Invalid message or context");
                return;
            }

            try
            {
                LoggingService.LogInfo($"Processing user message: {messageText}");
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
                LoggingService.LogInfo("Using ChatOrchestrator for processing");
                await ProcessWithOrchestrator(messageText);
            }
            else
            {
                LoggingService.LogInfo("Using simulation for processing");
                await ProcessWithSimulation(messageText);
            }
        }

        private async Task ProcessWithOrchestrator(string messageText)
        {
            try
            {
                LoggingService.LogInfo($"Calling ChatOrchestrator.ProcessUserMessageAsync for conversation: {currentContext.conversationId}");
                
                var response = await chatOrchestrator.ProcessUserMessageAsync(
                    currentContext.conversationId, 
                    messageText
                );

                LoggingService.LogInfo($"ChatOrchestrator response - Success: {response.success}, " +
                                     $"Content: {(string.IsNullOrEmpty(response.content) ? "EMPTY" : $"[{response.content.Length} chars]")}, " +
                                     $"Error: {(string.IsNullOrEmpty(response.errorMessage) ? "NONE" : response.errorMessage)}");

                if (response.success && !string.IsNullOrEmpty(response.content))
                {
                    LoggingService.LogInfo("Response successful - adding to context");
                    currentContext.AddAssistantMessage(response.content);
                    NotifyResponseTarget(GetLastMessage());
                    LogAssistantResponse(response.content);
                }
                else
                {
                    LoggingService.LogWarning($"Response failed or empty - Success: {response.success}, " +
                                            $"Content empty: {string.IsNullOrEmpty(response.content)}");
                    HandleOrchestratorError(response.errorMessage);
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"ProcessWithOrchestrator exception: {ex.Message}");
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
            string fullError = $"Orchestrator error: {errorMessage ?? "Unknown error"}";
            LoggingService.LogError($"HandleOrchestratorError called - Error: {fullError}");
            responseTarget?.ReceiveError(fullError);
        }

        private void HandleProcessingError(Exception ex)
        {
            string errorMessage = $"Error processing message: {ex.Message}";
            LoggingService.LogError($"HandleProcessingError called - Error: {errorMessage}");
            responseTarget?.ReceiveError(errorMessage);
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
