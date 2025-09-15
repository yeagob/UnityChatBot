using System.Collections.Generic;
using UnityEngine;
using Grid.Models.Grid;
using Grid.Grid;
using MapSystem.Models.Map;
using MapSystem.Enums;
using MapSystem.Models.Vision;

namespace MapSystem
{
    public class MapSystem : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private GridSystem gridSystem;
        
        [Header("Map Configuration")]
        [SerializeField] private bool autoCreateCells = true;
        [SerializeField] private bool debugMode = false;
        
        private MapCell[] mapCells;
        private Dictionary<string, MapElement> registeredElements;
        private bool isInitialized = false;
        
        public GridSystem GridSystem => gridSystem;
        public bool IsInitialized => isInitialized;
        
        private void Awake()
        {
            InitializeMapSystem();
        }
        
        private void Start()
        {
            if (autoCreateCells)
            {
                CreateMapCells();
            }
        }
        
        private void InitializeMapSystem()
        {
            if (gridSystem == null)
            {
                gridSystem = GetComponent<GridSystem>();
                if (gridSystem == null)
                {
                    gridSystem = FindObjectOfType<GridSystem>();
                }
            }
            
            registeredElements = new Dictionary<string, MapElement>();
        }
        
        private void CreateMapCells()
        {
            if (gridSystem == null)
            {
                Debug.LogError("MapSystem: GridSystem dependency not found!");
                return;
            }
            
            GridConfiguration config = gridSystem.GetGridConfiguration();
            int totalCells = config.GetTotalCells();
            
            mapCells = new MapCell[totalCells];
            
            for (int i = 0; i < totalCells; i++)
            {
                GridCell gridCell = gridSystem.GetGridCellFromLinearIndex(i);
                mapCells[i] = new MapCell(gridCell);
            }
            
            isInitialized = true;
            
            if (debugMode)
            {
                Debug.Log($"MapSystem: Created {totalCells} map cells");
            }
        }
        
        public void RegisterElement(MapElement element)
        {
            if (element == null || !isInitialized)
            {
                return;
            }
            
            string elementId = element.Context.elementId;
            
            if (!registeredElements.ContainsKey(elementId))
            {
                registeredElements[elementId] = element;
                
                GridCell elementCell = element.CurrentGridCell;
                MapCell mapCell = GetMapCell(elementCell);
                
                if (mapCell != null)
                {
                    mapCell.AddElement(element);
                    
                    if (debugMode)
                    {
                        Debug.Log($"MapSystem: Registered element {element.name} at {elementCell}");
                    }
                }
            }
        }
        
        public void UnregisterElement(MapElement element)
        {
            if (element == null || !isInitialized)
            {
                return;
            }
            
            string elementId = element.Context.elementId;
            
            if (registeredElements.ContainsKey(elementId))
            {
                registeredElements.Remove(elementId);
                
                GridCell elementCell = element.CurrentGridCell;
                MapCell mapCell = GetMapCell(elementCell);
                
                if (mapCell != null)
                {
                    mapCell.RemoveElement(element);
                    
                    if (debugMode)
                    {
                        Debug.Log($"MapSystem: Unregistered element {element.name} from {elementCell}");
                    }
                }
            }
        }
        
        public void MoveElement(MapElement element, GridCell newCell)
        {
            if (element == null || !isInitialized)
            {
                return;
            }
            
            GridCell oldCell = element.CurrentGridCell;
            MapCell oldMapCell = GetMapCell(oldCell);
            MapCell newMapCell = GetMapCell(newCell);
            
            if (oldMapCell != null && newMapCell != null)
            {
                oldMapCell.RemoveElement(element);
                newMapCell.AddElement(element);
                element.SetGridPosition(newCell);
                
                if (debugMode)
                {
                    Debug.Log($"MapSystem: Moved element {element.name} from {oldCell} to {newCell}");
                }
            }
        }
        
        public MapCell GetMapCell(GridCell gridCell)
        {
            if (!isInitialized || mapCells == null)
            {
                return null;
            }
            
            GridConfiguration config = gridSystem.GetGridConfiguration();
            if (!config.IsValidGridCell(gridCell))
            {
                return null;
            }
            
            int linearIndex = gridSystem.GetLinearIndex(gridCell);
            return mapCells[linearIndex];
        }
        
        public MapCell GetMapCell(int row, int column)
        {
            return GetMapCell(new GridCell(row, column));
        }
        
        public Vector3 GetWorldPositionFromGridCell(GridCell gridCell)
        {
            if (gridSystem != null)
            {
                return gridSystem.GetCellCenterWorldPosition(gridCell);
            }
            return Vector3.zero;
        }
        
        public GridResult<GridCell> GetGridCellFromWorldPosition(Vector3 worldPosition)
        {
            if (gridSystem != null)
            {
                return gridSystem.WorldPointToGridCell(worldPosition);
            }
            return GridResult<GridCell>.Failure();
        }
        
        public float GetDistanceBetweenElements(MapElement elementA, MapElement elementB)
        {
            if (elementA == null || elementB == null || gridSystem == null)
            {
                return float.MaxValue;
            }
            
            Vector3 positionA = GetWorldPositionFromGridCell(elementA.CurrentGridCell);
            Vector3 positionB = GetWorldPositionFromGridCell(elementB.CurrentGridCell);
            
            return Vector3.Distance(positionA, positionB);
        }
        
        public int GetGridDistanceBetweenElements(MapElement elementA, MapElement elementB)
        {
            if (elementA == null || elementB == null)
            {
                return int.MaxValue;
            }
            
            return GridSystem.Grid.GridUtilities.GetManhattanDistance(
                elementA.CurrentGridCell, 
                elementB.CurrentGridCell
            );
        }
        
        public bool IsCellTraversable(GridCell gridCell)
        {
            MapCell mapCell = GetMapCell(gridCell);
            return mapCell != null && mapCell.isTraversable;
        }
        
        public bool IsCellTraversable(int row, int column)
        {
            return IsCellTraversable(new GridCell(row, column));
        }
        
        public MapElement[] GetAllElements()
        {
            MapElement[] elements = new MapElement[registeredElements.Count];
            registeredElements.Values.CopyTo(elements, 0);
            return elements;
        }
        
        public MapElement[] GetElementsByType(MapElementType elementType)
        {
            List<MapElement> filteredElements = new List<MapElement>();
            
            foreach (MapElement element in registeredElements.Values)
            {
                if (element.ElementType == elementType)
                {
                    filteredElements.Add(element);
                }
            }
            
            return filteredElements.ToArray();
        }
        
        public MapElement GetElementById(string elementId)
        {
            registeredElements.TryGetValue(elementId, out MapElement element);
            return element;
        }
        
        public int GetTotalElementCount()
        {
            return registeredElements.Count;
        }
        
        public int GetElementCountByType(MapElementType elementType)
        {
            int count = 0;
            foreach (MapElement element in registeredElements.Values)
            {
                if (element.ElementType == elementType)
                {
                    count++;
                }
            }
            return count;
        }
        
        public MapCell[] GetAllMapCells()
        {
            return mapCells;
        }
        
        public void SetDebugMode(bool enabled)
        {
            debugMode = enabled;
        }
    }
}