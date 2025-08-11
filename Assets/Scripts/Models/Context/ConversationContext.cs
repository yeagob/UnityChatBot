using System;
using System.Collections.Generic;
using ChatSystem.Enums;

namespace ChatSystem.Models.Context
{
    [Serializable]
    public class ConversationContext
    {
        public string ConversationId;
        public List<Message> Messages;
        public DateTime CreatedAt;
        public DateTime LastUpdated;

        public ConversationContext(string conversationId)
        {
            this.ConversationId = conversationId;
            this.Messages = new List<Message>();
            this.CreatedAt = DateTime.UtcNow;
            this.LastUpdated = DateTime.UtcNow;
        }

        public void AddMessage(Message message)
        {
            Messages.Add(message);
            LastUpdated = DateTime.UtcNow;
        }

        public void AddUserMessage(string content)
        {
            string messageId = Guid.NewGuid().ToString();
            Message message = new Message(messageId, MessageRole.User, MessageType.Text, content, ConversationId);
            AddMessage(message);
            this.LastUpdated = DateTime.UtcNow;
        }

        public void AddAssistantMessage(string content)
        {
            string messageId = Guid.NewGuid().ToString();
            Message message = new Message(messageId, MessageRole.Assistant, MessageType.Text, content, ConversationId);
            AddMessage(message);
            this.LastUpdated = DateTime.UtcNow;
        }

        public void AddToolMessage(string content, string toolCallId)
        {
            string messageId = Guid.NewGuid().ToString();
            Message message = new Message(messageId, MessageRole.Tool, MessageType.ToolResponse, content, toolCallId, ConversationId);
            AddMessage(message);
            this.LastUpdated = DateTime.UtcNow;
        }

        public List<Message> GetMessages()
        {
            return new List<Message>(Messages);
        }

		internal void ClearMessages()
		{
            Messages.Clear();
            this.LastUpdated = DateTime.UtcNow;
        }
    }
}
