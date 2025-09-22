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
using MapSystem.Enums;
using MapSystem.Models.Map;
using TMPro;

namespace ChatSystem.Characters
{
    public enum ConextType
    {
        Vision,
        Conversation
    }
    
    public class CharacterAgent : MonoBehaviour
    {
        [Header("Misión en la vida")]
        [SerializeField]
        private string _initialMessage;
        
        [Header("Agente")]
        [SerializeField]
        private AgentConfig[] agentConfigurations;

        [Header("Game Refs")]
        [SerializeField]
        private CharacterElement _characterElement;
        
        [SerializeField]
        private MapSystem.MapSystem _mapSystem;

        public Sprite AvatarImage;

        [Header("Dialog System")]
        [SerializeField] 
        private TextMeshProUGUI _dialogText ;
        
        [SerializeField] 
        private GameObject _dialogObject;

        
        private IChatOrchestrator chatOrchestrator;
        private ILLMOrchestrator llmOrchestrator;
        private IContextManager contextManager;
        private IAgentExecutor agentExecutor;
        private IPersistenceService persistenceService;
        private IToolSet characterToolSet;
        
        private Dictionary<ConextType, PromptConfig> _contextPrompt = new Dictionary<ConextType, PromptConfig>(); 

        public void InitializeAgent()
        {
            HideDialog();
            CreateCoreServices();
            CreateToolSets();
            CreateServices();
            ConfigureServices();
            LoggingService.Initialize(LogLevel.Debug);
        }

        public async Task<LLMResponse>  ExecuteAgentCall()
        {
            LLMResponse response = null;
            
            if (agentConfigurations is { Length: > 0 })
            {
                AgentConfig firstAgent = agentConfigurations[0];
                MapCell[] map = _mapSystem.GetAllCellsWithElements();
                PromptConfig visionPromptConfig = CreatePromptMap(map);
                
                firstAgent.contextPrompts.Add(visionPromptConfig);
                
                response = await chatOrchestrator.ProcessUserMessageAsync(_characterElement.Id.ToString(), _initialMessage);

                //Clar all promtps context
                int count = firstAgent.contextPrompts.Count;
                for (int i = count-1; i > 1; i--)
                {
                    firstAgent.contextPrompts.RemoveAt(i);    
                }
                
            }
            
            return response;
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
            public int r;
            public int c;
            public MapElementJson[] e;
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

                //Sistema de Vision
                if (_characterElement.FacingDirection == ViewDirection.Left && cell.gridCell.column > _characterElement.GetGridPosition().y ||
                    _characterElement.FacingDirection == ViewDirection.Right && cell.gridCell.column < _characterElement.GetGridPosition().y )
                {
                    continue;
                }
                
                cellsJson.Add(new MapCellJson
                {
                    r = cell.gridCell.row,
                    c = cell.gridCell.column,
                    e = elementsJson.ToArray()
                });
                    

                if (cell.isTraversable)
                {
                    traversableCount++;
                }
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

            El mapa está representado como una cuadrícula de celdas, donde cada celda puede contener uno o más elementos.

            Se trata de un tablero de: {_mapSystem.GridSystem.GetGridConfiguration().gridWidth}x{_mapSystem.GridSystem.GetGridConfiguration().gridHeight}

            ESTRUCTURA DEL MAPA:
            - Cada celda tiene coordenadas de fila/columna            
            - Los elementos en las celdas tienen tipos: Item, Character, u Obstacle
            - Cada elemento tiene un ID para referencia         

            DATOS ACTUALES DEL MAPA:
            {jsonContent}

            Usa estos datos del mapa para entender las relaciones espaciales, planificar movimientos e interactuar con los elementos por sus IDs.";

            return promptConfig;
        }

        public void Talk(string message)
        {
            _dialogObject.SetActive(true);
            _dialogText.text = message;
            List<CharacterElement> nearCharacterElements = _mapSystem.GetAllCharactesAtDistance(2, _characterElement.CurrentGridCell);
            foreach (CharacterElement nearCharacterElement in nearCharacterElements)
            {
                nearCharacterElement.GetComponent<CharacterAgent>().Listen(message, agentConfigurations[0].name);
            }
        }

        private void Listen(string message, string remit)
        {
            PromptConfig promptConversations = ScriptableObject.CreateInstance<PromptConfig>();
            promptConversations.promptId = "map-system-data";
            promptConversations.promptName = "Conversaciones";
            promptConversations.category = "";
            promptConversations.description = "Conversaciones acumuladas en el turno actual";
            promptConversations.enabled = true;
            promptConversations.priority = 10;
            promptConversations.version = "1.0";

            promptConversations.content += $@" {remit} Ha dicho: {message}";

            agentConfigurations[0].contextPrompts.Add(promptConversations);
        }

        private void HideDialog()
        {
            _dialogObject.SetActive(false);
        }

        public bool Teleport(int row, int col)
        {
            return _characterElement.TryMoveTo(row, col);
        }


        public string Flip()
        {
            _characterElement.Flip();
            return CreatePromptMap(_mapSystem.GetAllCellsWithElements()).content;
        }
    }
}