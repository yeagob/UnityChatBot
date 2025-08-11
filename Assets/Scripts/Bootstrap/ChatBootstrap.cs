using UnityEngine;
using ChatSystem.Views.Chat;
using ChatSystem.Controllers;
using ChatSystem.Controllers.Interfaces;

namespace ChatSystem.Bootstrap
{
    [System.Obsolete("Use DependencyBootstrap for full system initialization. This is kept for legacy compatibility.")]
    public class ChatBootstrap : MonoBehaviour
    {
        [Header("Component References")]
        [SerializeField] private ChatView chatView;
        
        [Header("Configuration")]
        [SerializeField] private string conversationId = "legacy-chat";
        
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;

        private IChatController chatController;

        private void Start()
        {
            InitializeSystem();
        }
        
        private void InitializeSystem()
        {
            LogWarning("Using legacy ChatBootstrap. Consider using DependencyBootstrap for full functionality.");
            
            CreateController();
            ValidateComponents();
            ConnectComponents();
            LogInitializationComplete();
        }
        
        private void CreateController()
        {
            chatController = new ChatController(conversationId);
        }
        
        private void ValidateComponents()
        {
            if (chatView == null)
            {
                LogError("ChatView reference is missing");
                return;
            }
            
            if (chatController == null)
            {
                LogError("ChatController creation failed");
                return;
            }
        }
        
        private void ConnectComponents()
        {
            if (chatView != null && chatController != null)
            {
                chatView.SetController(chatController);
                LogDebug("Components connected successfully");
            }
        }
        
        private void LogInitializationComplete()
        {
            LogInfo("Legacy chat system initialized successfully");
        }
        
        private void LogDebug(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[ChatBootstrap] {message}");
            }
        }
        
        private void LogInfo(string message)
        {
            Debug.Log($"[ChatBootstrap] {message}");
        }
        
        private void LogWarning(string message)
        {
            Debug.LogWarning($"[ChatBootstrap] WARNING: {message}");
        }
        
        private void LogError(string message)
        {
            Debug.LogError($"[ChatBootstrap] ERROR: {message}");
        }
        
        public ChatView GetChatView()
        {
            return chatView;
        }
        
        public IChatController GetChatController()
        {
            return chatController;
        }
    }
}
