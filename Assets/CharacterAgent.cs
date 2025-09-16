using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChatSystem.Configuration.ScriptableObjects;
using ChatSystem.Models.LLM;
using UnityEngine;
using ChatSystem.Services.Orchestrators.Interfaces;
using ChatSystem.Services.Context.Interfaces;
using ChatSystem.Services.Agents.Interfaces;
using ChatSystem.Services.Orchestrators;
using ChatSystem.Services.Context;
using ChatSystem.Services.Agents;
using ChatSystem.Services.Logging;
using ChatSystem.Services.Tools;
using ChatSystem.Services.Persistence;
using ChatSystem.Services.Persistence.Interfaces;
using ChatSystem.Services.Tools.Interfaces;
using MapSystem;
using MapSystem.Elements;
using MapSystem.Models.Map;

namespace ChatSystem.Characters
{
    public class CharacterAgent : MonoBehaviour
    {
        [SerializeField]
        private AgentConfig[] agentConfigurations;

        [SerializeField]
        private CharacterElement characterElement;
        
        [SerializeField]
        private MapSystem.MapSystem _mapSystem;

        [SerializeField]
        private string _initialMessage;
        
        private IChatOrchestrator chatOrchestrator;
        private ILLMOrchestrator llmOrchestrator;
        private IContextManager contextManager;
        private IAgentExecutor agentExecutor;
        private IPersistenceService persistenceService;
        private IToolSet characterToolSet;

        private async void Start()
        {
            InitializeAgent();
            await ExecuteInitialAgentCall();
        }

        private void InitializeAgent()
        {
            CreateCoreServices();
            CreateToolSets();
            CreateServices();
            ConfigureServices();
            LoggingService.Initialize(LogLevel.Debug);
        }
        
        
        private async Task  ExecuteInitialAgentCall()
        {
            if (agentConfigurations is { Length: > 0 })
            {
                AgentConfig firstAgent = agentConfigurations[0];
                firstAgent.contextPrompts.Add(CreatePromptMap(_mapSystem.GetAllCellsWithElements()));
                LLMResponse response = await chatOrchestrator.ProcessUserMessageAsync(characterElement.Id.ToString(), _initialMessage);
                
                //Si nos hemos movido o girado, actualizamos con una nueva llamada de visión. 
            }
        }

        private void CreateCoreServices()
        {
            contextManager = new ContextManager();
            agentExecutor = new AgentExecutor();
            persistenceService = new PersistenceService();
        }

        private void CreateToolSets()
        {
            characterToolSet = new CharacterToolSet(this);
            agentExecutor.RegisterToolSet(characterToolSet);
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

        private void ConfigureServices()
        {
            if (chatOrchestrator is ChatOrchestrator chatOrchestratorImpl)
            {
                chatOrchestratorImpl.SetLLMOrchestrator(llmOrchestrator);
                chatOrchestratorImpl.SetContextManager(contextManager);
                chatOrchestratorImpl.SetPersistenceService(persistenceService);
            }
        }

        [System.Serializable]
        public struct MapDataJson
        {
            public MapCellJson[] cells;
            public MapMetadata metadata;
        }

        [System.Serializable]
        public struct MapCellJson
        {
            public int row;
            public int col;
            public MapElementJson[] elements;
        }

        [System.Serializable]
        public struct MapElementJson
        {
            public int id;
            public string type;
            public string name;
        }

        [System.Serializable]
        public struct MapMetadata
        {
            public int totalCells;
            public int traversableCells;
            public int elementsCount;
            public DateTime generatedAt;
        }

        private PromptConfig CreatePromptMap(MapCell[] mapElementsCells)
        {
            List<MapCellJson> cellsJson = new List<MapCellJson>();
            int traversableCount = 0;
            int totalElements = 0;

            foreach (MapCell cell in mapElementsCells)
            {
                List<MapElementJson> elementsJson = new List<MapElementJson>();

                foreach (MapElement element in cell.elements)
                {
                    elementsJson.Add(new MapElementJson
                    {
                        id = element.Id,
                        type = element.ElementType.ToString(),
                        name = element.name,
                    });
                }

                cellsJson.Add(new MapCellJson
                {
                    row = cell.gridCell.row,
                    col = cell.gridCell.column,
                    elements = elementsJson.ToArray()
                });

                if (cell.isTraversable) traversableCount++;
                totalElements += cell.elements.Count;
            }

            MapDataJson mapData = new MapDataJson
            {
                cells = cellsJson.ToArray(),
                metadata = new MapMetadata
                {
                    totalCells = mapElementsCells.Length,
                    traversableCells = traversableCount,
                    elementsCount = totalElements,
                    generatedAt = DateTime.UtcNow
                }
            };

            string jsonContent = JsonUtility.ToJson(mapData, true);

            PromptConfig promptConfig = ScriptableObject.CreateInstance<PromptConfig>();
            promptConfig.promptId = "map-system-data";
            promptConfig.promptName = "Estado Actual del Mapa";
            promptConfig.category = "Sistema del Mapa";
            promptConfig.description = "Estado actual del mapa con todas las celdas y elementos";
            promptConfig.enabled = true;
            promptConfig.priority = 10;
            promptConfig.version = "1.0";

            promptConfig.content = $@"DATOS DEL SISTEMA DEL MAPA

            El mapa está representado como una cuadrícula de celdas, donde cada celda puede contener múltiples elementos.

            ESTRUCTURA DEL MAPA:
            - Cada celda tiene coordenadas de fila/columna y una posición mundial
            - Las celdas pueden ser transitables o estar bloqueadas
            - Los elementos en las celdas tienen tipos: Item, Character, u Obstacle
            - Cada elemento tiene un ID para referencia

            REGLAS DE TRÁNSITO:
            - isTraversable: false significa que no se puede entrar en la celda
            - traversalCost: valores más altos indican un movimiento más difícil
            - Los elementos marcados canBeTraversed: false bloquean el movimiento

            DATOS ACTUALES DEL MAPA:
            {jsonContent}

            Usa estos datos del mapa para entender las relaciones espaciales, planificar movimientos e interactuar con los elementos por sus IDs.";

            return promptConfig;
        }

        public void Talk(string message)
        {
            Debug.LogError("NO SE HA IMPLEMENTADO EL SISTEMA DE MENSAGES: " + message);
        }

        public bool Teleport(int row, int col)
        {
            return characterElement.TryMoveTo(row, col);
        }
    }
}