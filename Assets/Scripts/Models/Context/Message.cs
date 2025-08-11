using System;
using ChatSystem.Enums;

namespace ChatSystem.Models.Context
{
    [Serializable]
    public struct Message
    {
        public string id;
        public MessageRole role;
        public MessageType type;
        public string content;
        public string toolCallId;
        public DateTime timestamp;
        public string conversationId;

        public Message(string id, MessageRole role, MessageType type, string content, string conversationId)
        {
            this.id = id;
            this.role = role;
            this.type = type;
            this.content = content;
            this.toolCallId = string.Empty;
            this.timestamp = DateTime.UtcNow;
            this.conversationId = conversationId;
        }

        public Message(string id, MessageRole role, MessageType type, string content, string toolCallId, string conversationId)
        {
            this.id = id;
            this.role = role;
            this.type = type;
            this.content = content;
            this.toolCallId = toolCallId;
            this.timestamp = DateTime.UtcNow;
            this.conversationId = conversationId;
        }
    }
}
