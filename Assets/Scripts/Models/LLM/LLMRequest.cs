using System;
using System.Collections.Generic;
using ChatSystem.Models.Context;
using ChatSystem.Models.Tools;

namespace ChatSystem.Models.LLM
{
    [Serializable]
    public class LLMRequest
    {
        public string model;
        public List<Message> messages;
        public List<ToolConfiguration> tools;
        public float temperature;
        public int maxTokens;
        public float topP;
        public float topK;
        public string requestId;
        public string serviceUrl;
        public DateTime timestamp;
        
        public LLMRequest()
        {
            messages = new List<Message>();
            tools = new List<ToolConfiguration>();
            requestId = Guid.NewGuid().ToString();
            timestamp = DateTime.UtcNow;
        }
    }
}
