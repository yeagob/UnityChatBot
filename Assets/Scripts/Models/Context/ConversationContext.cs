using System;
using System.Collections.Generic;
using System.Linq;
using ChatSystem.Enums;
using ChatSystem.Models.Tools;

namespace ChatSystem.Models.Context
{
    public class ConversationContext
    {
        public string conversationId { get; private set; }
        public List<Message> messages { get; private set; }
        public DateTime createdAt { get; private set; }
        public DateTime lastUpdated { get; private set; }
        
        public ConversationContext(string conversationId)
        {
            this.conversationId = conversationId;
            messages = new List<Message>();
            createdAt = DateTime.UtcNow;
            lastUpdated = DateTime.UtcNow;
        }
        
        public void AddUserMessage(string content)
        {
            AddMessage(MessageRole.User, content);
        }
        
        public void AddAssistantMessage(string content)
        {
            AddMessage(MessageRole.Assistant, content);
        }
        
        public void AddSystemMessage(string content)
        {
            AddMessage(MessageRole.System, content);
        }
        
        public void AddToolMessage(string content, string toolCallId)
        {
            messages.Add(new Message
            {
                id = Guid.NewGuid().ToString(),
                role = MessageRole.Tool,
                type = MessageType.ToolResponse,
                content = content,
                toolCallId = toolCallId,
                timestamp = DateTime.UtcNow,
                conversationId = conversationId
            });
            lastUpdated = DateTime.UtcNow;
        }
        
        public void AddToolMessages(List<ToolResponse> toolResponses)
        {
            foreach (ToolResponse response in toolResponses)
            {
                AddToolMessage(response.content, response.toolCallId);
            }
        }
        
        public List<Message> GetAllMessages()
        {
            return new List<Message>(messages);
        }
        
        public List<Message> GetMessagesByRole(MessageRole role)
        {
            return messages.Where(m => m.role == role).ToList();
        }
        
        public Message GetLastMessage()
        {
            return messages.LastOrDefault();
        }
        
        public void Clear()
        {
            messages.Clear();
            lastUpdated = DateTime.UtcNow;
        }
        
        public void AddMessage(MessageRole role, string content)
        {
            messages.Add(new Message
            {
                id = Guid.NewGuid().ToString(),
                role = role,
                type = role == MessageRole.System ? MessageType.SystemPrompt : MessageType.Text,
                content = content,
                timestamp = DateTime.UtcNow,
                conversationId = conversationId
            });
            lastUpdated = DateTime.UtcNow;
        }
    }
}