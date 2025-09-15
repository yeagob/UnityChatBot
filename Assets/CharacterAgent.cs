using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChatSystem.Configuration.ScriptableObjects;
using UnityEngine;
using ChatSystem.Services.Orchestrators.Interfaces;
using ChatSystem.Services.Context.Interfaces;
using ChatSystem.Services.Agents.Interfaces;
using ChatSystem.Services.Orchestrators;
using ChatSystem.Services.Context;
using ChatSystem.Services.Agents;
using ChatSystem.Services.Tools;
using ChatSystem.Services.Persistence;
using ChatSystem.Services.Persistence.Interfaces;
using MapSystem;
using MapSystem.Elements;
using MapSystem.Models.Map;

namespace ChatSystem.Characters
{
    public class CharacterAgent : MonoBehaviour
    {
        [Header("Agent Configuration")]
        [SerializeField] private AgentConfig[] agentConfigurations;

        [SerializeField]
        private MapSystem.MapSystem mapSystem;
        
        [SerializeField]
        private CharacterElement characterElement;
        
        private IChatOrchestrator chatOrchestrator;
        private ILLMOrchestrator llmOrchestrator;
        private IContextManager contextManager;
        private IAgentExecutor agentExecutor;
        private IPersistenceService persistenceService;
      //  private IToolSet userToolSet;
        
        private void Start()
        {
            InitializeAgent();
        }
        
        private void InitializeAgent()
        {
            CreateCoreServices();
            CreateToolSets();
            CreateServices();
            ConfigureServices();
           ExecuteInitialAgentCall();
        }
        
        private void CreateCoreServices()
        {
            contextManager = new ContextManager();
            agentExecutor = new AgentExecutor();
            persistenceService = new PersistenceService();
        }
        
        private void CreateToolSets()
        {
            // userToolSet = new UserToolSet();
            // travelToolSet = new TravelToolSet();
            //
            // agentExecutor.RegisterToolSet(userToolSet);
            // agentExecutor.RegisterToolSet(travelToolSet);
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
        
        
        private void ExecuteInitialAgentCall()
        {
            if (agentConfigurations != null && agentConfigurations.Length > 0)
            {
                AgentConfig firstAgent = agentConfigurations[0];
                firstAgent.contextPrompts.Add(CreatePromptMap(mapSystem.GetAllMapCells()));
                chatOrchestrator.ProcessUserMessageAsync(characterElement.Id.ToString(), "Actúa con libertad");
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
    public Vector3 worldPosition;
    public bool isTraversable;
    public float traversalCost;
    public MapElementJson[] elements;
}

[System.Serializable]
public struct MapElementJson
{
    public int id;
    public string type;
    public string name;
    public bool canBeTraversed;
}

[System.Serializable]
public struct MapMetadata
{
    public int totalCells;
    public int traversableCells;
    public int elementsCount;
    public DateTime generatedAt;
}

private PromptConfig CreatePromptMap(MapCell[] getAllMapCells)
{
    List<MapCellJson> cellsJson = new List<MapCellJson>();
    int traversableCount = 0;
    int totalElements = 0;
    
    foreach (MapCell cell in getAllMapCells)
    {
        List<MapElementJson> elementsJson = new List<MapElementJson>();
        
        foreach (MapElement element in cell.elements)
        {
            elementsJson.Add(new MapElementJson
            {
                id = element.Id,
                type = element.ElementType.ToString(),
                name = element.name,
                canBeTraversed = element.CanBeTraversed()
            });
        }
        
        cellsJson.Add(new MapCellJson
        {
            row = cell.gridCell.row,
            col = cell.gridCell.column,
            worldPosition = cell.WorldPosition,
            isTraversable = cell.isTraversable,
            traversalCost = cell.traversalCost,
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
            totalCells = getAllMapCells.Length,
            traversableCells = traversableCount,
            elementsCount = totalElements,
            generatedAt = DateTime.UtcNow
        }
    };
    
    string jsonContent = JsonUtility.ToJson(mapData, true);
    
    PromptConfig promptConfig = ScriptableObject.CreateInstance<PromptConfig>();
    promptConfig.promptId = "map-system-data";
    promptConfig.promptName = "Current Map State";
    promptConfig.category = "Map System";
    promptConfig.description = "Current state of the map with all cells and elements";
    promptConfig.enabled = true;
    promptConfig.priority = 10;
    promptConfig.version = "1.0";
    
    promptConfig.content = $@"MAP SYSTEM DATA

You have access to the current map state. The map is represented as a grid of cells, where each cell can contain multiple elements.

MAP STRUCTURE:
- Each cell has row/col coordinates and a world position
- Cells can be traversable or blocked
- Elements in cells have types: Item, Character, or Obstacle
- Each element has an ID for reference

TRAVERSAL RULES:
- isTraversable: false means the cell cannot be entered
- traversalCost: higher values indicate harder movement
- Elements marked canBeTraversed: false block movement

CURRENT MAP DATA:
{jsonContent}

Use this map data to understand spatial relationships, plan movements, and interact with elements by their IDs.";
    
    return promptConfig;
}
    }
}