using System;
using System.Collections.Generic;
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
        private IResponsable responseTarget;
        private IChatOrchestrator chatOrchestrator;
        private int lastDisplayedMessageCount;

        public ChatController(string conversationId = "default-conversation")
        {
            defaultConversationId = conversationId;
            lastDisplayedMessageCount = 0;
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

            defaultConversationId = conversationId;
            lastDisplayedMessageCount = 0;
        }

        public void SetResponseTarget(IResponsable target)
        {
            responseTarget = target;
            LoggingService.LogDebug($"[ChatController] Response target set: {target?.GetType().Name ?? "null"}");
        }

        public async Task ProcessUserMessageAsync(string messageText)
        {
            if (string.IsNullOrWhiteSpace(messageText))
            {
                LoggingService.LogWarning("[ChatController] Invalid message");
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
            return GetContextFromOrchestrator();
        }

        public void ClearConversation()
        {
            if (chatOrchestrator != null)
            {
                chatOrchestrator.ClearConversation(defaultConversationId);
                lastDisplayedMessageCount = 0;
            }
        }

        private async Task HandleUserMessage(string messageText)
        {
            DisplayUserMessage(messageText);
            
            if (chatOrchestrator != null)
            {
                await ProcessWithOrchestrator(messageText);
            }
            else
            {
                LoggingService.LogError($"[ChatController] ChatOrchestrator is null");
            }
        }

        private void DisplayUserMessage(string messageText)
        {
            Message userMessage = new Message
            {
                id = Guid.NewGuid().ToString(),
                role = MessageRole.User,
                type = MessageType.Text,
                content = messageText,
                timestamp = DateTime.UtcNow,
                conversationId = defaultConversationId
            };
            
            responseTarget?.ReceiveResponse(userMessage);
            LogUserMessage(messageText);
        }

        private async Task ProcessWithOrchestrator(string messageText)
        {
            try
            {
                LLMResponse response = await chatOrchestrator.ProcessUserMessageAsync(
                    defaultConversationId, 
                    messageText
                );

                DisplayNewMessagesFromOrchestrator();

                if (!response.success)
                {
                    string errorMsg = response.errorMessage ?? "Unknown orchestrator error";
                    LoggingService.LogError($"[ChatController] Orchestrator failed. Error: {errorMsg}");
                    HandleOrchestratorError(errorMsg);
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"[ChatController] Exception in ProcessWithOrchestrator: {ex.Message}");
                HandleProcessingError(ex);
            }
        }

        private void DisplayNewMessagesFromOrchestrator()
        {
            if (responseTarget == null || chatOrchestrator == null) return;

            ConversationContext context = GetContextFromOrchestrator();
            if (context == null) return;

            List<Message> allMessages = context.GetAllMessages();
            int currentMessageCount = allMessages.Count;

            if (currentMessageCount > lastDisplayedMessageCount)
            {
                for (int i = lastDisplayedMessageCount; i < currentMessageCount; i++)
                {
                    Message message = allMessages[i];
                    
                    if (message.role != MessageRole.User)
                    {
                        responseTarget.ReceiveResponse(message);
                        LoggingService.LogDebug($"[ChatController] Displayed message: {message.role} - {message.content.Substring(0, Math.Min(50, message.content.Length))}...");
                    }
                }
                lastDisplayedMessageCount = currentMessageCount;
            }
        }

        private ConversationContext GetContextFromOrchestrator()
        {
            try
            {
                string contextString = chatOrchestrator?.GetConversationContext(defaultConversationId);
                if (!string.IsNullOrEmpty(contextString))
                {
                    LoggingService.LogDebug($"[ChatController] Context retrieved from orchestrator");
                    return ParseContextFromString(contextString);
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"[ChatController] Error getting context: {ex.Message}");
            }
            
            return null;
        }

        private ConversationContext ParseContextFromString(string contextString)
        {
            ConversationContext context = new ConversationContext(defaultConversationId);
            
            if (chatOrchestrator is ChatSystem.Services.Orchestrators.ChatOrchestrator concreteOrchestrator)
            {
                return GetActualContextFromOrchestrator();
            }
            
            return context;
        }

        private ConversationContext GetActualContextFromOrchestrator()
        {
            if (chatOrchestrator == null) return null;
            
            try
            {
                Task<string> contextTask = Task.Run(async () => 
                    await chatOrchestrator.GetConversationContextAsync(defaultConversationId));
                    
                string contextData = contextTask.Result;
                LoggingService.LogDebug($"[ChatController] Got context data: {contextData}");
                
                ConversationContext context = new ConversationContext(defaultConversationId);
                return context;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"[ChatController] Error getting actual context: {ex.Message}");
                return null;
            }
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

        private void LogUserMessage(string message)
        {
            LoggingService.LogInfo($"[ChatController] User message received: {message}");
        }
    }
}
