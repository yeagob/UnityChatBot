using ChatSystem.Configuration.ScriptableObjects;
using UnityEngine;
using ChatSystem.Examples;
using ChatSystem.Controllers.Interfaces;
using ChatSystem.Views.Chat;
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
            InitializeLogging();
            CreateCoreServices();
            CreateToolSets();
            CreateServices();
            CreateControllers();
            ConfigureServices();
            ConnectComponents();
            CreateDebugObjectsIfEnabled();
        }
        
        private void InitializeLogging()
        {
            LoggingService.Initialize(logLevel);
        }
        
        private void CreateCoreServices()
        {
            contextManager = new ContextManager();
            agentExecutor = new AgentExecutor();
            persistenceService = new PersistenceService();
        }
        
        private void CreateToolSets()
        {
            userToolSet = new UserToolSet();
            travelToolSet = new TravelToolSet();
            
            agentExecutor.RegisterToolSet(userToolSet);
            agentExecutor.RegisterToolSet(travelToolSet);
        }
        
        private void CreateServices()
        {
            llmOrchestrator = new LLMOrchestrator(agentExecutor);
            chatOrchestrator = new ChatOrchestrator();
            
            RegisterAgentConfigurations();
        }
        
        private void RegisterAgentConfigurations()
        {
            if (agentConfigurations != null && agentConfigurations.Length > 0)
            {
                foreach (AgentConfig config in agentConfigurations)
                {
                    if (config != null)
                    {
                        llmOrchestrator.RegisterAgentConfig(config);
                    }
                }
            }
        }
        
        private void CreateControllers()
        {
            chatController = new ChatController(defaultConversationId);
        }
        
        private void ConfigureServices()
        {
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
            ConnectViewToController();
        }
        
        private void ConnectViewToController()
        {
            if (chatView != null && chatController != null)
            {
                chatView.SetController(chatController);
            }
            else
            {
                LoggingService.LogWarning("ChatView or ChatController is null - view connection skipped");
            }
        }
        
        private void CreateDebugObjectsIfEnabled()
        {
            if (!createDebugObjects) return;
            
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
    }
}