using UnityEngine;
using ChatSystem.Bootstrap;
using ChatSystem.Services.Communication;
using ChatSystem.Services.Communication.Interfaces;
using ChatSystem.Services.Audio;
using ChatSystem.Services.Audio.Interfaces;
using ChatSystem.Services.Orchestrators;
using ChatSystem.Services.Orchestrators.Interfaces;
using ChatSystem.Controllers.Voice;
using ChatSystem.Controllers.Voice.Interfaces;
using ChatSystem.Views.Voice;
using ChatSystem.Configuration.Voice;
using ChatSystem.Services.Logging;

namespace ChatSystem.Bootstrap.Voice
{
    public class VoiceSystemBootstrap : ChatManager
    {
        [Header("Voice System Configuration")]
        [SerializeField] private VoiceAgentConfig[] voiceAgentConfigs;
        [SerializeField] private VoiceView voiceView;
        [SerializeField] private string defaultConversationId = "voice-conversation";
        [SerializeField] private string defaultVoiceAgentId = "voice-agent-default";
        
        [Header("Voice System Settings")]
        [SerializeField] private bool createVoiceDebugObjects = true;
        [SerializeField] private bool enableVoiceLogging = true;
        
        private IWebSocketService webSocketService;
        private IAudioService audioService;
        private IRealtimeOrchestrator realtimeOrchestrator;
        private IVoiceController voiceController;

        protected override void CreateServices()
        {
            base.CreateServices();
            
            if (enableVoiceLogging)
            {
                LoggingService.LogInfo("Creating Voice System services");
            }
            
            CreateVoiceServices();
        }

        protected override void RegisterAgents()
        {
            base.RegisterAgents();
            RegisterVoiceAgents();
        }

        protected override void CreateControllers()
        {
            base.CreateControllers();
            CreateVoiceControllers();
        }

        protected override void ConfigureServices()
        {
            base.ConfigureServices();
            ConfigureVoiceServices();
        }

        protected override void ConnectComponents()
        {
            base.ConnectComponents();
            ConnectVoiceComponents();
        }

        protected override void CreateDebugObjects()
        {
            base.CreateDebugObjects();
            
            if (createVoiceDebugObjects)
            {
                CreateVoiceDebugObjects();
            }
        }

        private void CreateVoiceServices()
        {
            audioService = FindObjectOfType<AudioService>();
            if (audioService == null)
            {
                GameObject audioServiceGO = new GameObject("[VOICE] AudioService");
                audioServiceGO.transform.SetParent(transform);
                audioService = audioServiceGO.AddComponent<AudioService>();
            }

            webSocketService = new WebSocketService();
            
            realtimeOrchestrator = new RealtimeOrchestrator(
                webSocketService,
                audioService,
                agentExecutor,
                contextManager
            );

            LoggingService.LogInfo("Voice services created successfully");
        }

        private void RegisterVoiceAgents()
        {
            if (voiceAgentConfigs == null || voiceAgentConfigs.Length == 0)
            {
                LoggingService.LogWarning("No VoiceAgentConfigs configured");
                return;
            }

            foreach (VoiceAgentConfig voiceAgent in voiceAgentConfigs)
            {
                if (voiceAgent != null)
                {
                    llmOrchestrator.RegisterAgent(voiceAgent);
                    LoggingService.LogInfo($"Registered voice agent: {voiceAgent.AgentName}");
                }
            }

            LoggingService.LogInfo($"Registered {voiceAgentConfigs.Length} voice agents");
        }

        private void CreateVoiceControllers()
        {
            voiceController = new VoiceController();
            LoggingService.LogInfo("VoiceController created");
        }

        private void ConfigureVoiceServices()
        {
            if (voiceController == null)
            {
                LoggingService.LogError("VoiceController not created");
                return;
            }

            voiceController.SetRealtimeOrchestrator(realtimeOrchestrator);
            voiceController.SetAudioService(audioService);
            voiceController.SetContextManager(contextManager);

            LoggingService.LogInfo("Voice services configured");
        }

        private void ConnectVoiceComponents()
        {
            if (voiceView != null && voiceController != null)
            {
                voiceView.SetController(voiceController);
                voiceController.SetVoiceView(voiceView);
                
                SetupVoiceAgentsInUI();
                LoggingService.LogInfo("Voice components connected");
            }
            else
            {
                LoggingService.LogWarning("VoiceView or VoiceController not available for connection");
            }
        }

        private void SetupVoiceAgentsInUI()
        {
            if (voiceAgentConfigs == null || voiceAgentConfigs.Length == 0 || voiceView == null)
            {
                return;
            }

            string[] agentIds = new string[voiceAgentConfigs.Length];
            string[] agentNames = new string[voiceAgentConfigs.Length];

            for (int i = 0; i < voiceAgentConfigs.Length; i++)
            {
                agentIds[i] = voiceAgentConfigs[i].AgentId;
                agentNames[i] = voiceAgentConfigs[i].AgentName;
            }

            voiceView.SetAvailableAgents(agentIds, agentNames);
            LoggingService.LogInfo($"Setup {agentNames.Length} agents in UI");
        }

        private void CreateVoiceDebugObjects()
        {
            if (webSocketService != null)
            {
                GameObject webSocketDebug = new GameObject("[DEBUG] WebSocketService");
                webSocketDebug.transform.SetParent(transform);
                WebSocketDebugComponent wsDebugComponent = webSocketDebug.AddComponent<WebSocketDebugComponent>();
                wsDebugComponent.Initialize(webSocketService);
            }

            if (audioService != null)
            {
                GameObject audioDebug = new GameObject("[DEBUG] AudioService");
                audioDebug.transform.SetParent(transform);
                AudioDebugComponent audioDebugComponent = audioDebug.AddComponent<AudioDebugComponent>();
                audioDebugComponent.Initialize(audioService);
            }

            if (realtimeOrchestrator != null)
            {
                GameObject realtimeDebug = new GameObject("[DEBUG] RealtimeOrchestrator");
                realtimeDebug.transform.SetParent(transform);
                RealtimeOrchestratorDebugComponent rtDebugComponent = realtimeDebug.AddComponent<RealtimeOrchestratorDebugComponent>();
                rtDebugComponent.Initialize(realtimeOrchestrator);
            }

            if (voiceController != null)
            {
                GameObject voiceControllerDebug = new GameObject("[DEBUG] VoiceController");
                voiceControllerDebug.transform.SetParent(transform);
                VoiceControllerDebugComponent vcDebugComponent = voiceControllerDebug.AddComponent<VoiceControllerDebugComponent>();
                vcDebugComponent.Initialize(voiceController);
            }

            LoggingService.LogInfo("Voice debug objects created");
        }

        public async void StartDefaultVoiceSession()
        {
            if (voiceController == null)
            {
                LoggingService.LogError("Cannot start voice session: VoiceController not initialized");
                return;
            }

            string agentId = (voiceAgentConfigs != null && voiceAgentConfigs.Length > 0) 
                ? voiceAgentConfigs[0].AgentId 
                : defaultVoiceAgentId;

            try
            {
                await voiceController.StartVoiceSessionAsync(defaultConversationId, agentId);
                LoggingService.LogInfo($"Default voice session started with agent: {agentId}");
            }
            catch (System.Exception ex)
            {
                LoggingService.LogError($"Failed to start default voice session: {ex.Message}");
            }
        }

        private void OnDestroy()
        {
            if (webSocketService != null)
            {
                webSocketService.Dispose();
            }

            if (realtimeOrchestrator != null && realtimeOrchestrator.IsSessionActive)
            {
                realtimeOrchestrator.EndSessionAsync();
            }
        }

        [ContextMenu("Test Voice Session")]
        private void TestVoiceSession()
        {
            StartDefaultVoiceSession();
        }

        [ContextMenu("Stop Voice Session")]
        private async void StopVoiceSession()
        {
            if (voiceController != null)
            {
                await voiceController.StopVoiceSessionAsync();
            }
        }

        [ContextMenu("Show Voice System Info")]
        private void ShowVoiceSystemInfo()
        {
            LoggingService.LogInfo("=== Voice System Information ===");
            LoggingService.LogInfo($"VoiceAgentConfigs: {(voiceAgentConfigs?.Length ?? 0)}");
            LoggingService.LogInfo($"WebSocketService: {(webSocketService != null ? "Ready" : "Not Ready")}");
            LoggingService.LogInfo($"AudioService: {(audioService != null ? "Ready" : "Not Ready")}");
            LoggingService.LogInfo($"RealtimeOrchestrator: {(realtimeOrchestrator != null ? "Ready" : "Not Ready")}");
            LoggingService.LogInfo($"VoiceController: {(voiceController != null ? "Ready" : "Not Ready")}");
            LoggingService.LogInfo($"VoiceView: {(voiceView != null ? "Connected" : "Not Connected")}");
            
            if (voiceController != null)
            {
                LoggingService.LogInfo($"Session Active: {voiceController.IsSessionActive}");
                LoggingService.LogInfo($"Current Conversation: {voiceController.CurrentConversationId ?? "None"}");
                LoggingService.LogInfo($"Current Agent: {voiceController.CurrentAgentId ?? "None"}");
            }
        }
    }

    #region Debug Components

    public class WebSocketDebugComponent : MonoBehaviour
    {
        private IWebSocketService webSocketService;

        public void Initialize(IWebSocketService service)
        {
            webSocketService = service;
        }

        [ContextMenu("Test WebSocket Connection")]
        private void TestConnection()
        {
            LoggingService.LogInfo($"WebSocket Status: {webSocketService?.ConnectionStatus ?? "Not Available"}");
        }
    }

    public class AudioDebugComponent : MonoBehaviour
    {
        private IAudioService audioService;

        public void Initialize(IAudioService service)
        {
            audioService = service;
        }

        [ContextMenu("Test Audio Recording")]
        private async void TestRecording()
        {
            if (audioService == null) return;
            
            if (audioService.IsRecording)
            {
                await audioService.StopRecordingAsync();
                LoggingService.LogInfo("Audio recording stopped");
            }
            else
            {
                bool started = await audioService.StartRecordingAsync();
                LoggingService.LogInfo($"Audio recording started: {started}");
            }
        }

        [ContextMenu("Show Audio Status")]
        private void ShowAudioStatus()
        {
            if (audioService == null)
            {
                LoggingService.LogWarning("AudioService not available");
                return;
            }

            LoggingService.LogInfo($"Recording: {audioService.IsRecording}");
            LoggingService.LogInfo($"Playing: {audioService.IsPlaying}");
            LoggingService.LogInfo($"Volume: {audioService.CurrentVolume}");
            LoggingService.LogInfo($"Available Microphones: {audioService.AvailableMicrophones?.Length ?? 0}");
        }
    }

    public class RealtimeOrchestratorDebugComponent : MonoBehaviour
    {
        private IRealtimeOrchestrator orchestrator;

        public void Initialize(IRealtimeOrchestrator service)
        {
            orchestrator = service;
        }

        [ContextMenu("Show Orchestrator Status")]
        private void ShowStatus()
        {
            if (orchestrator == null)
            {
                LoggingService.LogWarning("RealtimeOrchestrator not available");
                return;
            }

            LoggingService.LogInfo($"Session Active: {orchestrator.IsSessionActive}");
            LoggingService.LogInfo($"Session ID: {orchestrator.CurrentSessionId ?? "None"}");
            LoggingService.LogInfo($"Agent ID: {orchestrator.CurrentAgentId ?? "None"}");
        }
    }

    public class VoiceControllerDebugComponent : MonoBehaviour
    {
        private IVoiceController controller;

        public void Initialize(IVoiceController service)
        {
            controller = service;
        }

        [ContextMenu("Show Controller Status")]
        private void ShowStatus()
        {
            if (controller == null)
            {
                LoggingService.LogWarning("VoiceController not available");
                return;
            }

            LoggingService.LogInfo($"Session Active: {controller.IsSessionActive}");
            LoggingService.LogInfo($"Conversation ID: {controller.CurrentConversationId ?? "None"}");
            LoggingService.LogInfo($"Agent ID: {controller.CurrentAgentId ?? "None"}");
        }
    }

    #endregion
}