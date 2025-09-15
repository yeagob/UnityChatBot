using UnityEngine;
using GridSystem.Models.Grid;
using GridSystem.Configuration;

namespace GridSystem.Grid
{
    public partial class GridSystem
    {
        private void OnDrawGizmos()
        {
            if (gridConfig == null || !gridConfig.showGridGizmos)
            {
                return;
            }

            DrawGridLines();
            
            if (gridConfig.showGridNumbers)
            {
                DrawGridNumbers();
            }
        }

        private void DrawGridLines()
        {
            Gizmos.color = gridConfig.gridColor;
            
            Vector3 gridStartPosition = GetGridStartPosition();
            float totalWidth = gridConfig.GetTotalWidth();
            float totalHeight = gridConfig.GetTotalHeight();

            for (int row = 0; row <= gridConfig.gridHeight; row++)
            {
                Vector3 lineStart = gridStartPosition + new Vector3(0, -row * gridConfig.cellHeight, 0);
                Vector3 lineEnd = lineStart + new Vector3(totalWidth, 0, 0);
                
                lineStart = transform.TransformPoint(lineStart);
                lineEnd = transform.TransformPoint(lineEnd);
                
                Gizmos.DrawLine(lineStart, lineEnd);
            }

            for (int column = 0; column <= gridConfig.gridWidth; column++)
            {
                Vector3 lineStart = gridStartPosition + new Vector3(column * gridConfig.cellWidth, 0, 0);
                Vector3 lineEnd = lineStart + new Vector3(0, -totalHeight, 0);
                
                lineStart = transform.TransformPoint(lineStart);
                lineEnd = transform.TransformPoint(lineEnd);
                
                Gizmos.DrawLine(lineStart, lineEnd);
            }
        }

        private void DrawGridNumbers()
        {
            for (int row = 0; row < gridConfig.gridHeight; row++)
            {
                for (int column = 0; column < gridConfig.gridWidth; column++)
                {
                    GridCell cell = new GridCell(row, column);
                    Vector3 cellCenter = GetCellCenterWorldPosition(cell);
                    int linearIndex = GetLinearIndex(cell);
                    
                    DrawGizmoText(cellCenter, linearIndex.ToString());
                }
            }
        }

        private void DrawGizmoText(Vector3 position, string text)
        {
            #if UNITY_EDITOR
            UnityEditor.Handles.color = GridSystemConfiguration.DEFAULT_CELL_NUMBER_COLOR;
            UnityEditor.Handles.Label(position, text);
            #endif
        }

        private Vector3 GetGridStartPosition()
        {
            return new Vector3(gridConfig.offsetLeft, gridConfig.offsetTop, 0);
        }

        public void DrawCellHighlight(GridCell cell, Color highlightColor)
        {
            if (!gridConfig.IsValidGridCell(cell))
            {
                return;
            }

            Gizmos.color = highlightColor;
            Vector3 cellCenter = GetCellCenterWorldPosition(cell);
            Vector3 cellSize = new Vector3(gridConfig.cellWidth, gridConfig.cellHeight, 0.1f);
            
            Gizmos.DrawWireCube(cellCenter, cellSize);
        }

        public void DrawCellFill(GridCell cell, Color fillColor)
        {
            if (!gridConfig.IsValidGridCell(cell))
            {
                return;
            }

            Gizmos.color = fillColor;
            Vector3 cellCenter = GetCellCenterWorldPosition(cell);
            Vector3 cellSize = new Vector3(gridConfig.cellWidth, gridConfig.cellHeight, 0.01f);
            
            Gizmos.DrawCube(cellCenter, cellSize);
        }

        public Bounds GetGridBounds()
        {
            Vector3 center = transform.TransformPoint(
                gridConfig.offsetLeft + gridConfig.GetTotalWidth() * 0.5f,
                gridConfig.offsetTop - gridConfig.GetTotalHeight() * 0.5f,
                0
            );
            
            Vector3 size = new Vector3(
                gridConfig.GetTotalWidth(),
                gridConfig.GetTotalHeight(),
                0.1f
            );

            return new Bounds(center, size);
        }
    }
}