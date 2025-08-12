using UnityEngine;
using ChatSystem.Enums;

namespace ChatSystem.Configuration.ScriptableObjects
{
    [CreateAssetMenu(fileName = "ModelConfig", menuName = "LLM/Model Configuration", order = 5)]
    public class ModelConfig : ScriptableObject
    {
        [Header("Model Settings")]
        public string modelName;
        public ServiceProvider provider;
        public string modelVersion;
        
        [Header("Parameters")]
        [Range(0f, 2f)]
        public float temperature = 0.7f;
        
        [Range(0, 8192)]
        public int maxTokens = 2048;
        
        [Range(0f, 1f)]
        public float topP = 0.9f;
        
        [Range(0f, 2f)]
        public float presencePenalty = 0f;
        
        [Range(0f, 2f)]
        public float frequencyPenalty = 0f;
        
        [Header("Cost Tracking")]
        public float costPerInputToken = 0.0001f;
        public float costPerOutputToken = 0.0002f;
    }
}