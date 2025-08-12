using System;
using System.Threading.Tasks;
using ChatSystem.Models.Context;
using ChatSystem.Models.LLM;
using ChatSystem.Services.Orchestrators.Interfaces;
using ChatSystem.Services.Context.Interfaces;
using ChatSystem.Services.Persistence.Interfaces;
using ChatSystem.Services.Logging;

namespace ChatSystem.Services.Orchestrators
{
    public class ChatOrchestrator : IChatOrchestrator
    {
        private readonly ILLMOrchestrator llmOrchestrator;
        private readonly IContextManager contextManager;
        private readonly IPersistenceService persistenceService;
        
        public ChatOrchestrator(
            ILLMOrchestrator llmOrchestrator, 
            IContextManager contextManager, 
            IPersistenceService persistenceService)
        {
            this.llmOrchestrator = llmOrchestrator ?? throw new ArgumentNullException(nameof(llmOrchestrator));
            this.contextManager = contextManager ?? throw new ArgumentNullException(nameof(contextManager));
            this.persistenceService = persistenceService;
            
            LoggingService.LogInfo("ChatOrchestrator initialized with dependencies");
        }
        
        public async Task<LLMResponse> ProcessUserMessageAsync(string conversationId, string userMessage)
        {
            LoggingService.LogInfo($"Processing user message for conversation: {conversationId}");
            
            try
            {
                await contextManager.AddUserMessageAsync(conversationId, userMessage);
                ConversationContext context = await contextManager.GetContextAsync(conversationId);
                
                LLMResponse response = await ExecuteLLMProcessing(context);
                await ProcessLLMResponse(conversationId, response);
                
                if (persistenceService != null)
                {
                    await persistenceService.SaveConversationAsync(context);
                }
                
                LoggingService.LogInfo($"Message processing completed. Success: {response.success}");
                return response;
            }
            catch (Exception ex)
            {
                LoggingService.Error($"ProcessUserMessageAsync failed: {ex.Message}");
                return CreateErrorResponse(ex);
            }
        }
        
        public string GetConversationContext(string conversationId)
        {
            try
            {
                ConversationContext context = contextManager.GetContextAsync(conversationId).Result;
                return context?.ToString() ?? string.Empty;
            }
            catch (Exception ex)
            {
                LoggingService.LogWarning($"GetConversationContext failed: {ex.Message}");
                return string.Empty;
            }
        }
        
        public void ClearConversation(string conversationId)
        {
            try
            {
                LoggingService.LogInfo($"Clearing conversation: {conversationId}");
                contextManager.ClearContextAsync(conversationId).Wait();
                
                if (persistenceService != null)
                {
                    persistenceService.DeleteConversationAsync(conversationId).Wait();
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"ClearConversation failed: {ex.Message}");
            }
        }
        
        private async Task<LLMResponse> ExecuteLLMProcessing(ConversationContext context)
        {
            try
            {
                LoggingService.LogDebug("Executing LLM processing");
                LLMResponse response = await llmOrchestrator.ProcessMessageAsync(context);
                
                LoggingService.LogDebug($"LLM processing completed. Success: {response.success}, " +
                                      $"Content length: {response.content?.Length ?? 0}, " +
                                      $"Error: {response.errorMessage ?? "None"}");
                
                return response;
            }
            catch (Exception ex)
            {
                LoggingService.Error($"LLM processing failed: {ex.Message}");
                return CreateErrorResponse(ex);
            }
        }
        
        private async Task ProcessLLMResponse(string conversationId, LLMResponse response)
        {
            if (response.success && !string.IsNullOrEmpty(response.content))
            {
                await contextManager.AddAssistantMessageAsync(conversationId, response.content);
                LoggingService.LogDebug("Assistant message added to context");
            }
            
            if (response.toolResponses != null)
            {
                foreach (var toolResponse in response.toolResponses)
                {
                    await contextManager.AddToolMessageAsync(conversationId, toolResponse.content, toolResponse.toolCallId);
                }
                LoggingService.LogDebug($"Added {response.toolResponses.Count} tool responses to context");
            }
        }
        
        private LLMResponse CreateErrorResponse(Exception exception)
        {
            return new LLMResponse
            {
                success = false,
                errorMessage = exception.Message,
                content = "I apologize, but I encountered an error processing your request.",
                timestamp = DateTime.UtcNow
            };
        }
    }
}
