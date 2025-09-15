using UnityEngine;
using Grid.Models.Grid;
using Grid.Enums;
using Grid.Configuration;

namespace Grid
{
    public class GridSystem : MonoBehaviour
    {
        [Header("Grid Configuration")]
        [SerializeField] private GridConfiguration gridConfig;

        [Header("Input")]
        [SerializeField] private Camera targetCamera;

        private void Awake()
        {
            InitializeGrid();
        }

        private void InitializeGrid()
        {
            if (gridConfig == null)
            {
                CreateDefaultConfiguration();
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
        }

        private void CreateDefaultConfiguration()
        {
            gridConfig = new GridConfiguration
            {
                gridWidth = GridSystemConfiguration.DEFAULT_GRID_WIDTH,
                gridHeight = GridSystemConfiguration.DEFAULT_GRID_HEIGHT,
                cellWidth = GridSystemConfiguration.DEFAULT_CELL_WIDTH,
                cellHeight = GridSystemConfiguration.DEFAULT_CELL_HEIGHT,
                offsetLeft = GridSystemConfiguration.DEFAULT_OFFSET_LEFT,
                offsetTop = GridSystemConfiguration.DEFAULT_OFFSET_TOP,
                gridColor = GridSystemConfiguration.DEFAULT_GRID_COLOR,
                showGridNumbers = GridSystemConfiguration.DEFAULT_SHOW_GRID_NUMBERS,
                showGridGizmos = GridSystemConfiguration.DEFAULT_SHOW_GRID_GIZMOS
            };
        }

        public GridResult<GridCell> ScreenPointToGridCell(Vector3 screenPoint)
        {
            if (targetCamera == null)
            {
                return GridResult<GridCell>.Failure();
            }

            Vector3 worldPoint = targetCamera.ScreenToWorldPoint(screenPoint);
            return WorldPointToGridCell(worldPoint);
        }

        public GridResult<GridCell> WorldPointToGridCell(Vector3 worldPoint)
        {
            Vector3 localPoint = transform.InverseTransformPoint(worldPoint);
            
            float adjustedX = localPoint.x - gridConfig.offsetLeft;
            float adjustedY = localPoint.y + gridConfig.offsetTop;
            
            int column = Mathf.FloorToInt(adjustedX / gridConfig.cellWidth);
            int row = Mathf.FloorToInt(-adjustedY / gridConfig.cellHeight);
            
            GridCell cell = new GridCell(row, column);
            
            if (gridConfig.IsValidGridCell(cell))
            {
                return GridResult<GridCell>.Success(cell);
            }
            
            return GridResult<GridCell>.Failure();
        }

        public Vector3 GetCellCenterWorldPosition(GridCell cell)
        {
            if (!gridConfig.IsValidGridCell(cell))
            {
                return Vector3.zero;
            }

            float worldX = gridConfig.offsetLeft + (cell.column * gridConfig.cellWidth) + (gridConfig.cellWidth * 0.5f);
            float worldY = -(gridConfig.offsetTop + (cell.row * gridConfig.cellHeight) + (gridConfig.cellHeight * 0.5f));
            
            Vector3 localPosition = new Vector3(worldX, worldY, 0f);
            return transform.TransformPoint(localPosition);
        }

        public Vector3 GetCellCenterWorldPosition(int row, int column)
        {
            return GetCellCenterWorldPosition(new GridCell(row, column));
        }

        public Vector3[] GetCellMultiplePositions(GridCell cell, int objectCount)
        {
            if (!gridConfig.IsValidGridCell(cell) || objectCount < 1 || objectCount > GridSystemConfiguration.MAX_OBJECTS_PER_CELL)
            {
                return new Vector3[0];
            }

            Vector3 centerPosition = GetCellCenterWorldPosition(cell);
            Vector3[] positions = new Vector3[objectCount];

            if (objectCount == 1)
            {
                positions[0] = centerPosition;
                return positions;
            }

            float cellWorldWidth = gridConfig.cellWidth;
            float spacing = cellWorldWidth / (objectCount + 1);
            float startX = centerPosition.x - (cellWorldWidth * 0.5f) + spacing;

            for (int i = 0; i < objectCount; i++)
            {
                positions[i] = new Vector3(
                    startX + (i * spacing),
                    centerPosition.y,
                    centerPosition.z
                );
            }

            return positions;
        }

        public int GetLinearIndex(GridCell cell)
        {
            return cell.GetLinearIndex(gridConfig.gridWidth);
        }
        

        public GridConfiguration GetGridConfiguration()
        {
            return gridConfig;
        }

        public void SetGridConfiguration(GridConfiguration newConfig)
        {
            gridConfig = newConfig;
        }
    }
}