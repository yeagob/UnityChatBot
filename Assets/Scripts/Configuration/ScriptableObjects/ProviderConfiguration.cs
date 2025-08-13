using UnityEngine;
using ChatSystem.Enums;

namespace ChatSystem.Configuration.ScriptableObjects
{
    [CreateAssetMenu(fileName = "ProviderConfig", menuName = "LLM/Provider Configuration", order = 1)]
    public class ProviderConfiguration : ScriptableObject
    {
        [Header("Provider Settings")]
        public ServiceProvider provider;
        public string providerName;
        
        [Header("Authentication")]
        public string token;
        public string serviceUrl;
        
        [Header("Rate Limiting")]
        public int requestsPerMinute = 60;
        public int maxConcurrentRequests = 5;
        
        [Header("Costs")]
        public float costPer1000InputTokens = 0.0015f;
        public float costPer1000OutputTokens = 0.002f;
        
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(serviceUrl);
        }
    }
}
