using System;
using System.Threading.Tasks;
using ChatSystem.Models.Context;
using ChatSystem.Models.LLM;
using ChatSystem.Services.Orchestrators.Interfaces;
using ChatSystem.Services.Context.Interfaces;
using ChatSystem.Services.Persistence.Interfaces;
using ChatSystem.Services.Logging;
using ChatSystem.Enums;

namespace ChatSystem.Services.Orchestrators
{
    public class ChatOrchestrator : IChatOrchestrator
    {
        private ILLMOrchestrator llmOrchestrator;
        private IContextManager contextManager;
        private IPersistenceService persistenceService;
        private string defaultConversationId;
        
        public ChatOrchestrator()
        {
            defaultConversationId = "default-conversation";
            LoggingService.LogInfo("ChatOrchestrator initialized");
        }
        
        public ChatOrchestrator(string conversationId)
        {
            defaultConversationId = conversationId;
            LoggingService.LogInfo($"ChatOrchestrator initialized with conversation: {conversationId}");
        }
        
        public void SetLLMOrchestrator(ILLMOrchestrator orchestrator)
        {
            llmOrchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
            LoggingService.LogDebug("LLMOrchestrator set in ChatOrchestrator");
        }
        
        public void SetContextManager(IContextManager manager)
        {
            contextManager = manager ?? throw new ArgumentNullException(nameof(manager));
            LoggingService.LogDebug("ContextManager set in ChatOrchestrator");
        }
        
        public void SetPersistenceService(IPersistenceService service)
        {
            persistenceService = service ?? throw new ArgumentNullException(nameof(service));
            LoggingService.LogDebug("PersistenceService set in ChatOrchestrator");
        }
        
        public async Task<LLMResponse> ProcessUserMessageAsync(string conversationId, string userMessage)
        {
            ValidateOrchestrator();
            LoggingService.LogInfo($"Processing user message for conversation: {conversationId}");
            
            await contextManager.AddUserMessageAsync(conversationId, userMessage);
            ConversationContext context = await contextManager.GetContextAsync(conversationId);
            
            LLMResponse response = await ExecuteLLMProcessing(context);
            await ProcessLLMResponse(conversationId, response);
            
            if (persistenceService != null)
            {
                await persistenceService.SaveConversationAsync(context);
            }
            
            return response;
        }
        
        public async Task<LLMResponse> ProcessMessageAsync(string conversationId, Message message)
        {
            ValidateOrchestrator();
            ValidateMessage(message);
            LoggingService.LogInfo($"Processing message for conversation: {conversationId}");
            
            await contextManager.AddMessageAsync(conversationId, message);
            ConversationContext context = await contextManager.GetContextAsync(conversationId);
            
            LLMResponse response = await ExecuteLLMProcessing(context);
            await ProcessLLMResponse(conversationId, response);
            
            if (persistenceService != null)
            {
                await persistenceService.SaveConversationAsync(context);
            }
            
            return response;
        }
        
        public async Task<string> GetConversationContextAsync(string conversationId)
        {
            if (contextManager == null)
            {
                LoggingService.LogWarning("ContextManager not available");
                return string.Empty;
            }
            
            ConversationContext context = await contextManager.GetContextAsync(conversationId);
            return context?.ToString() ?? string.Empty;
        }
        
        public async Task ClearConversationAsync(string conversationId)
        {
            LoggingService.LogInfo($"Clearing conversation: {conversationId}");
            
            if (contextManager != null)
            {
                await contextManager.ClearContextAsync(conversationId);
            }
            
            if (persistenceService != null)
            {
                await persistenceService.DeleteConversationAsync(conversationId);
            }
        }
        
        public string GetConversationContext(string conversationId)
        {
            return GetConversationContextAsync(conversationId).Result;
        }
        
        public void ClearConversation(string conversationId)
        {
            ClearConversationAsync(conversationId).Wait();
        }
        
        private void ValidateOrchestrator()
        {
            if (llmOrchestrator == null)
            {
                throw new InvalidOperationException("LLMOrchestrator not set. Call SetLLMOrchestrator first.");
            }
            
            if (contextManager == null)
            {
                throw new InvalidOperationException("ContextManager not set. Call SetContextManager first.");
            }
        }
        
        private void ValidateMessage(Message message)
        {
            if (string.IsNullOrWhiteSpace(message.content))
            {
                throw new ArgumentException("Message content cannot be empty", nameof(message));
            }
        }
        
        private async Task<LLMResponse> ExecuteLLMProcessing(ConversationContext context)
        {
            try
            {
                LoggingService.LogDebug("Executing LLM processing");
                return await llmOrchestrator.ProcessMessageAsync(context);
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
            }
            
            if (response.toolResponses != null)
            {
                foreach (var toolResponse in response.toolResponses)
                {
                    await contextManager.AddToolMessageAsync(conversationId, toolResponse.content, toolResponse.toolCallId);
                }
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
