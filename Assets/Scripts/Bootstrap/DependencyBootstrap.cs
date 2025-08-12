using UnityEngine;
using System.Collections.Generic;
using ChatSystem.Services.Orchestrators;
using ChatSystem.Services.Orchestrators.Interfaces;
using ChatSystem.Services.Agents;
using ChatSystem.Services.Agents.Interfaces;
using ChatSystem.Services.Tools;
using ChatSystem.Services.Context;
using ChatSystem.Services.Context.Interfaces;
using ChatSystem.Services.Persistence;
using ChatSystem.Services.Persistence.Interfaces;
using ChatSystem.Services.Logging;
using ChatSystem.Controllers;
using ChatSystem.Controllers.Interfaces;
using ChatSystem.Views.Chat;
using ChatSystem.Configuration.ScriptableObjects;
using ChatSystem.Debugging;

namespace ChatSystem.Bootstrap
{
    public class DependencyBootstrap : MonoBehaviour
    {
        [Header("View References")]
        [SerializeField] private ChatView chatView;
        
        [Header("Agent Configurations")]
        [SerializeField] private List<AgentConfig> agentConfigs;
        
        [Header("Default Settings")]
        [SerializeField] private string defaultConversationId = "main-conversation";
        
        [Header("Debug Settings")]
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private bool createDebugObjects = true;
        
        private ILLMOrchestrator llmOrchestrator;
        private IChatOrchestrator chatOrchestrator;
        private IAgentExecutor agentExecutor;
        private IContextManager contextManager;
        private IPersistenceService persistenceService;
        private IChatController chatController;
        
        private void Start()
        {
            InitializeSystem();
        }
        
        private void InitializeSystem()
        {
            LoggingService.Initialize(enableDebugLogs ? Enums.LogLevel.Debug : Enums.LogLevel.Info);
            LoggingService.LogInfo("Starting Dependency Bootstrap");
            
            CreateServices();
            RegisterAgents();
            CreateControllers();
            ConfigureServices();
            ConnectComponents();
            
            if (createDebugObjects)
            {
                CreateDebugObjects();
            }
            
            LoggingService.LogInfo("System initialization complete");
        }
        
        private void CreateServices()
        {
            contextManager = new ContextManager();
            persistenceService = new PersistenceService();
            agentExecutor = new AgentExecutor();
            llmOrchestrator = new LLMOrchestrator(agentExecutor);
            chatOrchestrator = new ChatOrchestrator(llmOrchestrator, contextManager, persistenceService);
            
            RegisterToolSets();
            
            LoggingService.LogInfo("Core services created");
        }
        
        private void RegisterAgents()
        {
            if (agentConfigs == null || agentConfigs.Count == 0)
            {
                LoggingService.LogWarning("No agent configurations provided");
                return;
            }
            
            foreach (AgentConfig config in agentConfigs)
            {
                if (config != null)
                {
                    llmOrchestrator.RegisterAgentConfig(config);
                }
            }
            
            LoggingService.LogInfo($"Registered {agentConfigs.Count} agent configurations");
        }
        
        private void RegisterToolSets()
        {
            agentExecutor.RegisterToolSet(new UserToolSet());
            agentExecutor.RegisterToolSet(new TravelToolSet());
            
            LoggingService.LogInfo("ToolSets registered");
        }
        
        private void CreateControllers()
        {
            chatController = new ChatController(defaultConversationId);
            LoggingService.LogInfo("Controllers created");
        }
        
        private void ConfigureServices()
        {
            chatController.SetOrchestrator(chatOrchestrator);
            LoggingService.LogInfo("Services configured");
        }
        
        private void ConnectComponents()
        {
            if (chatView != null)
            {
                chatView.SetController(chatController);
                LoggingService.LogInfo("View connected to controller");
            }
            else
            {
                LoggingService.Warning("No ChatView reference - running headless");
            }
        }
        
        private void CreateDebugObjects()
        {
            GameObject debugContainer = new GameObject("[DEBUG] Services");
            debugContainer.transform.SetParent(transform);
            
            GameObject chatOrchestratorDebug = new GameObject("[DEBUG] ChatOrchestrator");
            chatOrchestratorDebug.transform.SetParent(debugContainer.transform);
            ChatOrchestratorDebug chatDebugComponent = chatOrchestratorDebug.AddComponent<ChatOrchestratorDebug>();
            chatDebugComponent.SetOrchestrator(chatOrchestrator);
            
            GameObject llmOrchestratorDebug = new GameObject("[DEBUG] LLMOrchestrator");
            llmOrchestratorDebug.transform.SetParent(debugContainer.transform);
            LLMOrchestratorDebug llmDebugComponent = llmOrchestratorDebug.AddComponent<LLMOrchestratorDebug>();
            llmDebugComponent.SetOrchestrator(llmOrchestrator);
            
            GameObject chatControllerDebug = new GameObject("[DEBUG] ChatController");
            chatControllerDebug.transform.SetParent(debugContainer.transform);
            ChatControllerDebug controllerDebugComponent = chatControllerDebug.AddComponent<ChatControllerDebug>();
            controllerDebugComponent.SetController(chatController);
            
            GameObject serviceInfoDebug = new GameObject("[DEBUG] ServiceInfo");
            serviceInfoDebug.transform.SetParent(debugContainer.transform);
            ServiceInfoDebug serviceDebugComponent = serviceInfoDebug.AddComponent<ServiceInfoDebug>();
            serviceDebugComponent.SetServices(agentExecutor, contextManager, persistenceService);
            
            LoggingService.LogInfo("Debug objects created");
        }
    }
}