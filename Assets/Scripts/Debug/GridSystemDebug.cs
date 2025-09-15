using UnityEngine;
using GridSystem.Models.Grid;
using GridSystem.Enums;

namespace GridSystem.Debug
{
    public class GridSystemDebug : MonoBehaviour
    {
        [Header("Debug Settings")]
        [SerializeField] private Grid.GridSystem gridSystem;
        [SerializeField] private bool enableDebugMode = true;
        
        [Header("Test Parameters")]
        [SerializeField] private int testRow = 0;
        [SerializeField] private int testColumn = 0;
        [SerializeField] private int testLinearIndex = 0;
        [SerializeField] private int testObjectCount = 2;
        
        [Header("Debug Colors")]
        [SerializeField] private Color highlightColor = Color.red;
        [SerializeField] private Color adjacentColor = Color.yellow;
        [SerializeField] private Color positionMarkerColor = Color.green;

        private GridCell lastTestedCell;
        private GridResult<GridCell>[] lastAdjacentCells;

        private void Awake()
        {
            if (gridSystem == null)
            {
                gridSystem = GetComponent<Grid.GridSystem>();
            }
        }

        [ContextMenu("Test Cell Conversion")]
        public void TestCellConversion()
        {
            if (!enableDebugMode || gridSystem == null)
            {
                return;
            }

            GridCell testCell = new GridCell(testRow, testColumn);
            int linearIndex = gridSystem.GetLinearIndex(testCell);
            GridCell convertedBack = gridSystem.GetGridCellFromLinearIndex(linearIndex);
            
            UnityEngine.Debug.Log($"Original Cell: {testCell}, Linear Index: {linearIndex}, Converted Back: {convertedBack}");
            
            lastTestedCell = testCell;
        }

        [ContextMenu("Test Adjacent Cells")]
        public void TestAdjacentCells()
        {
            if (!enableDebugMode || gridSystem == null)
            {
                return;
            }

            GridCell testCell = new GridCell(testRow, testColumn);
            lastAdjacentCells = gridSystem.GetAllAdjacentCells(testCell);
            
            UnityEngine.Debug.Log($"Testing adjacent cells for: {testCell}");
            
            for (int i = 0; i < lastAdjacentCells.Length; i++)
            {
                GridDirection direction = (GridDirection)i;
                GridResult<GridCell> result = lastAdjacentCells[i];
                
                if (result.hasValue)
                {
                    UnityEngine.Debug.Log($"{direction}: {result.value}");
                }
                else
                {
                    UnityEngine.Debug.Log($"{direction}: OUT OF BOUNDS");
                }
            }
            
            lastTestedCell = testCell;
        }

        [ContextMenu("Test Multiple Positions")]
        public void TestMultiplePositions()
        {
            if (!enableDebugMode || gridSystem == null)
            {
                return;
            }

            GridCell testCell = new GridCell(testRow, testColumn);
            Vector3[] positions = gridSystem.GetCellMultiplePositions(testCell, testObjectCount);
            
            UnityEngine.Debug.Log($"Testing {testObjectCount} positions for cell: {testCell}");
            
            for (int i = 0; i < positions.Length; i++)
            {
                UnityEngine.Debug.Log($"Position {i}: {positions[i]}");
            }
            
            lastTestedCell = testCell;
        }

        [ContextMenu("Test Screen Point Conversion")]
        public void TestScreenPointConversion()
        {
            if (!enableDebugMode || gridSystem == null)
            {
                return;
            }

            Vector3 screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0);
            GridResult<GridCell> result = gridSystem.ScreenPointToGridCell(screenCenter);
            
            if (result.hasValue)
            {
                UnityEngine.Debug.Log($"Screen center {screenCenter} maps to cell: {result.value}");
                lastTestedCell = result.value;
            }
            else
            {
                UnityEngine.Debug.Log($"Screen center {screenCenter} is outside grid bounds");
            }
        }

        [ContextMenu("Test Linear Index Conversion")]
        public void TestLinearIndexConversion()
        {
            if (!enableDebugMode || gridSystem == null)
            {
                return;
            }

            GridCell cell = gridSystem.GetGridCellFromLinearIndex(testLinearIndex);
            Vector3 position = gridSystem.GetCellCenterWorldPosition(testLinearIndex);
            
            UnityEngine.Debug.Log($"Linear Index {testLinearIndex} maps to cell: {cell}, position: {position}");
            
            lastTestedCell = cell;
        }

        private void OnDrawGizmos()
        {
            if (!enableDebugMode || gridSystem == null)
            {
                return;
            }

            DrawDebugVisualizations();
        }

        private void DrawDebugVisualizations()
        {
            GridConfiguration config = gridSystem.GetGridConfiguration();
            if (config == null)
            {
                return;
            }

            if (config.IsValidGridCell(lastTestedCell))
            {
                gridSystem.DrawCellHighlight(lastTestedCell, highlightColor);
                
                if (lastAdjacentCells != null)
                {
                    foreach (GridResult<GridCell> result in lastAdjacentCells)
                    {
                        if (result.hasValue)
                        {
                            gridSystem.DrawCellHighlight(result.value, adjacentColor);
                        }
                    }
                }
                
                Vector3[] multiPositions = gridSystem.GetCellMultiplePositions(lastTestedCell, testObjectCount);
                foreach (Vector3 position in multiPositions)
                {
                    Gizmos.color = positionMarkerColor;
                    Gizmos.DrawWireSphere(position, 0.1f);
                }
            }
        }

        public void SetTestCell(int row, int column)
        {
            testRow = row;
            testColumn = column;
        }

        public void SetTestLinearIndex(int linearIndex)
        {
            testLinearIndex = linearIndex;
        }
    }
}