using UnityEngine;
using ChatSystem.Controllers;
using ChatSystem.Controllers.Interfaces;
using ChatSystem.Views.Chat;
using ChatSystem.Views.Interfaces;
using ChatSystem.Services.Orchestrators;
using ChatSystem.Services.Orchestrators.Interfaces;
using ChatSystem.Services.Context;
using ChatSystem.Services.Context.Interfaces;
using ChatSystem.Services.Agents;
using ChatSystem.Services.Agents.Interfaces;
using ChatSystem.Services.Tools;
using ChatSystem.Services.Tools.Interfaces;
using ChatSystem.Services.Persistence;
using ChatSystem.Services.Persistence.Interfaces;
using ChatSystem.Services.Logging;
using ChatSystem.Enums;
using ChatSystem.Debugging;

namespace ChatSystem.Bootstrap
{
    public class ChatManager : MonoBehaviour
    {
        [Header("View References")]
        [SerializeField] private ChatView chatView;
        
        [Header("Agent Configuration")]
        [SerializeField] private AgentConfig[] agentConfigurations;
        
        [Header("Configuration")]
        [SerializeField] private string defaultConversationId = "main-conversation";
        [SerializeField] private LogLevel logLevel = LogLevel.Info;
        
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private bool createDebugObjects = true;
        
        private IChatController chatController;
        private IChatOrchestrator chatOrchestrator;
        private ILLMOrchestrator llmOrchestrator;
        private IContextManager contextManager;
        private IAgentExecutor agentExecutor;
        private IPersistenceService persistenceService;
        private IToolSet userToolSet;
        private IToolSet travelToolSet;
        
        private void Start()
        {
            InitializeSystem();
        }
        
        private void InitializeSystem()
        {
            LogDebug("Starting dependency injection initialization");
            
            InitializeLogging();
            CreateCoreServices();
            CreateToolSets();
            CreateServices();
            CreateControllers();
            ConfigureServices();
            ConnectComponents();
            CreateDebugObjectsIfEnabled();
            
            LogInfo("Dependency injection completed successfully");
        }
        
        private void InitializeLogging()
        {
            LoggingService.Initialize(logLevel);
            LogDebug("Logging service initialized");
        }
        
        private void CreateCoreServices()
        {
            LogDebug("Creating core services");
            
            contextManager = new ContextManager();
            agentExecutor = new AgentExecutor();
            persistenceService = new PersistenceService();
        }
        
        private void CreateToolSets()
        {
            LogDebug("Creating tool sets");
            
            userToolSet = new UserToolSet();
            travelToolSet = new TravelToolSet();
            
            agentExecutor.RegisterToolSet(userToolSet);
            agentExecutor.RegisterToolSet(travelToolSet);
        }
        
        private void CreateServices()
        {
            LogDebug("Creating orchestration services");
            
            llmOrchestrator = new LLMOrchestrator();
            chatOrchestrator = new ChatOrchestrator(defaultConversationId);
        }
        
        private void CreateControllers()
        {
            LogDebug("Creating controllers");
            
            chatController = new ChatController(defaultConversationId);
        }
        
        private void ConfigureServices()
        {
            LogDebug("Configuring service dependencies");
            
            if (llmOrchestrator is LLMOrchestrator llmOrchestratorImpl)
            {
                llmOrchestratorImpl.SetAgentExecutor(agentExecutor);
                llmOrchestratorImpl.SetAgentConfigurations(agentConfigurations);
            }
            
            if (chatOrchestrator is ChatOrchestrator chatOrchestratorImpl)
            {
                chatOrchestratorImpl.SetLLMOrchestrator(llmOrchestrator);
                chatOrchestratorImpl.SetContextManager(contextManager);
                chatOrchestratorImpl.SetPersistenceService(persistenceService);
            }
            
            if (chatController is ChatController controller)
            {
                controller.SetChatOrchestrator(chatOrchestrator);
            }
        }
        
        private void ConnectComponents()
        {
            LogDebug("Connecting components");
            
            ConnectViewToController();
        }
        
        private void ConnectViewToController()
        {
            if (chatView != null && chatController != null)
            {
                chatView.SetController(chatController);
                LogDebug("ChatView connected to ChatController");
            }
            else
            {
                LogWarning("ChatView or ChatController is null - view connection skipped");
            }
        }
        
        private void CreateDebugObjectsIfEnabled()
        {
            if (!createDebugObjects) return;
            
            LogDebug("Creating debug objects");
            
            CreateChatOrchestratorDebugObject();
            CreateLLMOrchestratorDebugObject();
            CreateControllerDebugObject();
            CreateServiceDebugObjects();
        }
        
        private void CreateChatOrchestratorDebugObject()
        {
            GameObject debugObject = new GameObject("[DEBUG] ChatOrchestrator");
            debugObject.transform.SetParent(transform);
            
            ChatOrchestratorDebug debugComponent = debugObject.AddComponent<ChatOrchestratorDebug>();
            debugComponent.SetOrchestrator(chatOrchestrator);
        }
        
        private void CreateLLMOrchestratorDebugObject()
        {
            GameObject debugObject = new GameObject("[DEBUG] LLMOrchestrator");
            debugObject.transform.SetParent(transform);
            
            LLMOrchestratorDebug debugComponent = debugObject.AddComponent<LLMOrchestratorDebug>();
            debugComponent.SetOrchestrator(llmOrchestrator);
        }
        
        private void CreateControllerDebugObject()
        {
            GameObject debugObject = new GameObject("[DEBUG] ChatController");
            debugObject.transform.SetParent(transform);
            
            ChatControllerDebug debugComponent = debugObject.AddComponent<ChatControllerDebug>();
            debugComponent.SetController(chatController);
        }
        
        private void CreateServiceDebugObjects()
        {
            GameObject servicesDebugObject = new GameObject("[DEBUG] Services");
            servicesDebugObject.transform.SetParent(transform);
            
            CreateServiceInfoComponent(servicesDebugObject, "ContextManager", contextManager.GetType().Name);
            CreateServiceInfoComponent(servicesDebugObject, "AgentExecutor", agentExecutor.GetType().Name);
            CreateServiceInfoComponent(servicesDebugObject, "PersistenceService", persistenceService.GetType().Name);
            CreateServiceInfoComponent(servicesDebugObject, "UserToolSet", userToolSet.GetType().Name);
            CreateServiceInfoComponent(servicesDebugObject, "TravelToolSet", travelToolSet.GetType().Name);
        }
        
        private void CreateServiceInfoComponent(GameObject parent, string serviceName, string serviceType)
        {
            GameObject serviceObject = new GameObject($"[INFO] {serviceName}");
            serviceObject.transform.SetParent(parent.transform);
            
            ServiceInfoDebug infoComponent = serviceObject.AddComponent<ServiceInfoDebug>();
            infoComponent.Initialize(serviceName, serviceType);
        }
        
        public IChatController GetChatController()
        {
            return chatController;
        }
        
        public IChatOrchestrator GetChatOrchestrator()
        {
            return chatOrchestrator;
        }
        
        public ILLMOrchestrator GetLLMOrchestrator()
        {
            return llmOrchestrator;
        }
        
        public IContextManager GetContextManager()
        {
            return contextManager;
        }
        
        public IAgentExecutor GetAgentExecutor()
        {
            return agentExecutor;
        }
        
        public IPersistenceService GetPersistenceService()
        {
            return persistenceService;
        }
        
        public ChatView GetChatView()
        {
            return chatView;
        }
        
        private void LogDebug(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[DependencyBootstrap] {message}");
            }
        }
        
        private void LogInfo(string message)
        {
            Debug.Log($"[DependencyBootstrap] {message}");
        }
        
        private void LogWarning(string message)
        {
            Debug.LogWarning($"[DependencyBootstrap] WARNING: {message}");
        }
        
        private void LogError(string message)
        {
            Debug.LogError($"[DependencyBootstrap] ERROR: {message}");
        }
        
        private void OnDestroy()
        {
            LogDebug("DependencyBootstrap cleanup");
        }
    }
}
