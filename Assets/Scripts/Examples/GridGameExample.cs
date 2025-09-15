using UnityEngine;
using GridSystem.Models.Grid;
using GridSystem.Grid;

namespace GridSystem.Examples
{
    public class GridGameExample : MonoBehaviour
    {
        [Header("Grid Dependencies")]
        [SerializeField] private GridSystem.Grid.GridSystem gridSystem;
        
        [Header("Game Objects")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private GameObject obstaclePrefab;
        [SerializeField] private GameObject collectiblePrefab;
        
        [Header("Game Settings")]
        [SerializeField] private int obstacleCount = 5;
        [SerializeField] private int collectibleCount = 10;
        
        private GameObject currentPlayer;
        private GridCell playerCell;
        
        private void Start()
        {
            InitializeGame();
        }
        
        private void Update()
        {
            HandleInput();
        }
        
        private void InitializeGame()
        {
            if (gridSystem == null)
            {
                gridSystem = FindObjectOfType<GridSystem.Grid.GridSystem>();
            }
            
            SpawnPlayer();
            SpawnObstacles();
            SpawnCollectibles();
        }
        
        private void SpawnPlayer()
        {
            playerCell = new GridCell(0, 0);
            Vector3 playerPosition = gridSystem.GetCellCenterWorldPosition(playerCell);
            
            if (playerPrefab != null)
            {
                currentPlayer = Instantiate(playerPrefab, playerPosition, Quaternion.identity);
                currentPlayer.name = "Player";
            }
        }
        
        private void SpawnObstacles()
        {
            GridConfiguration config = gridSystem.GetGridConfiguration();
            
            for (int i = 0; i < obstacleCount; i++)
            {
                GridCell randomCell = GetRandomEmptyCell();
                Vector3 obstaclePosition = gridSystem.GetCellCenterWorldPosition(randomCell);
                
                if (obstaclePrefab != null)
                {
                    GameObject obstacle = Instantiate(obstaclePrefab, obstaclePosition, Quaternion.identity);
                    obstacle.name = $"Obstacle_{i}";
                }
            }
        }
        
        private void SpawnCollectibles()
        {
            for (int i = 0; i < collectibleCount; i++)
            {
                GridCell randomCell = GetRandomEmptyCell();
                Vector3 collectiblePosition = gridSystem.GetCellCenterWorldPosition(randomCell);
                
                if (collectiblePrefab != null)
                {
                    GameObject collectible = Instantiate(collectiblePrefab, collectiblePosition, Quaternion.identity);
                    collectible.name = $"Collectible_{i}";
                }
            }
        }
        
        private GridCell GetRandomEmptyCell()
        {
            GridConfiguration config = gridSystem.GetGridConfiguration();
            GridCell randomCell;
            
            do
            {
                int randomRow = Random.Range(0, config.gridHeight);
                int randomColumn = Random.Range(0, config.gridWidth);
                randomCell = new GridCell(randomRow, randomColumn);
            }
            while (randomCell == playerCell);
            
            return randomCell;
        }
        
        private void HandleInput()
        {
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            {
                TryMovePlayer(gridSystem.GetTopCell(playerCell));
            }
            else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            {
                TryMovePlayer(gridSystem.GetBottomCell(playerCell));
            }
            else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            {
                TryMovePlayer(gridSystem.GetLeftCell(playerCell));
            }
            else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                TryMovePlayer(gridSystem.GetRightCell(playerCell));
            }
            
            if (Input.GetMouseButtonDown(0))
            {
                HandleMouseClick();
            }
        }
        
        private void TryMovePlayer(GridResult<GridCell> moveResult)
        {
            if (moveResult.hasValue && IsValidMove(moveResult.value))
            {
                MovePlayerToCell(moveResult.value);
            }
        }
        
        private bool IsValidMove(GridCell targetCell)
        {
            Vector3 targetPosition = gridSystem.GetCellCenterWorldPosition(targetCell);
            Collider2D collider = Physics2D.OverlapCircle(targetPosition, 0.4f);
            
            return collider == null || !collider.CompareTag("Obstacle");
        }
        
        private void MovePlayerToCell(GridCell newCell)
        {
            playerCell = newCell;
            Vector3 newPosition = gridSystem.GetCellCenterWorldPosition(playerCell);
            
            if (currentPlayer != null)
            {
                currentPlayer.transform.position = newPosition;
            }
            
            CheckForCollectibles();
        }
        
        private void CheckForCollectibles()
        {
            Vector3 playerPosition = gridSystem.GetCellCenterWorldPosition(playerCell);
            Collider2D collider = Physics2D.OverlapCircle(playerPosition, 0.4f);
            
            if (collider != null && collider.CompareTag("Collectible"))
            {
                Destroy(collider.gameObject);
                Debug.Log($"Collected item at cell: {playerCell}");
            }
        }
        
        private void HandleMouseClick()
        {
            Vector3 mousePosition = Input.mousePosition;
            GridResult<GridCell> clickResult = gridSystem.ScreenPointToGridCell(mousePosition);
            
            if (clickResult.hasValue)
            {
                Debug.Log($"Clicked on cell: {clickResult.value}");
                
                if (IsValidMove(clickResult.value))
                {
                    MovePlayerToCell(clickResult.value);
                }
            }
        }
        
        [ContextMenu("Highlight Player Adjacent Cells")]
        public void HighlightAdjacentCells()
        {
            GridResult<GridCell>[] adjacentCells = gridSystem.GetAllAdjacentCells(playerCell);
            
            foreach (GridResult<GridCell> result in adjacentCells)
            {
                if (result.hasValue)
                {
                    Debug.Log($"Adjacent cell: {result.value}");
                }
            }
        }
        
        [ContextMenu("Show Cells in Radius")]
        public void ShowCellsInRadius()
        {
            var cellsInRadius = gridSystem.GetCellsInRadius(playerCell, 2);
            Debug.Log($"Cells within radius 2 of player: {cellsInRadius.Count}");
            
            foreach (GridCell cell in cellsInRadius)
            {
                Debug.Log($"Radius cell: {cell}");
            }
        }
    }
}