using UnityEngine;
using System.Collections;
using GridSystem.Models.Grid;
using MapSystem.Elements;
using MapSystem.Enums;
using MapSystem.Models.Vision;
using MapSystem.Vision;
using MapSystem.Navigation;

namespace MapSystem.Examples
{
    public class MapGameExample : MonoBehaviour
    {
        [Header("System Dependencies")]
        [SerializeField] private MapSystem mapSystem;
        [SerializeField] private GridSystem.Grid.GridSystem gridSystem;
        
        [Header("Element Prefabs")]
        [SerializeField] private GameObject characterPrefab;
        [SerializeField] private GameObject itemPrefab;
        [SerializeField] private GameObject obstaclePrefab;
        
        [Header("Game Configuration")]
        [SerializeField] private int characterCount = 2;
        [SerializeField] private int itemCount = 5;
        [SerializeField] private int obstacleCount = 8;
        [SerializeField] private bool autoPlay = false;
        
        private CharacterElement playerCharacter;
        private CharacterElement[] npcs;
        
        private void Start()
        {
            StartCoroutine(InitializeGameDelayed());
        }
        
        private IEnumerator InitializeGameDelayed()
        {
            yield return new WaitForSeconds(0.5f);
            
            if (mapSystem == null || !mapSystem.IsInitialized)
            {
                Debug.LogError("MapGameExample: MapSystem not ready!");
                yield break;
            }
            
            CreateGameElements();
            
            if (autoPlay)
            {
                StartCoroutine(AutoPlayDemo());
            }
        }
        
        private void CreateGameElements()
        {
            CreatePlayerCharacter();
            CreateNPCs();
            CreateItems();
            CreateObstacles();
            
            Debug.Log("MapGameExample: Game elements created successfully!");
        }
        
        private void CreatePlayerCharacter()
        {
            GridCell playerStartCell = new GridCell(1, 1);
            Vector3 playerPosition = mapSystem.GetWorldPositionFromGridCell(playerStartCell);
            
            GameObject playerObject = CreateElementObject(characterPrefab, playerPosition, "Player");
            playerCharacter = playerObject.GetComponent<CharacterElement>();
            
            if (playerCharacter == null)
            {
                playerCharacter = playerObject.AddComponent<CharacterElement>();
            }
            
            playerCharacter.SetGridPosition(playerStartCell);
            playerCharacter.SetPlayerControlled(true);
            playerCharacter.SetVisionDistance(4);
            playerCharacter.AddInteraction("Player character created and positioned");
        }
        
        private void CreateNPCs()
        {
            npcs = new CharacterElement[characterCount];
            
            for (int i = 0; i < characterCount; i++)
            {
                GridCell npcCell = GetRandomEmptyCell();
                Vector3 npcPosition = mapSystem.GetWorldPositionFromGridCell(npcCell);
                
                GameObject npcObject = CreateElementObject(characterPrefab, npcPosition, $"NPC_{i}");
                CharacterElement npc = npcObject.GetComponent<CharacterElement>();
                
                if (npc == null)
                {
                    npc = npcObject.AddComponent<CharacterElement>();
                }
                
                npc.SetGridPosition(npcCell);
                npc.SetPlayerControlled(false);
                npc.SetVisionDistance(3);
                npc.AddInteraction($"NPC {i} created and positioned");
                
                npcs[i] = npc;
            }
        }
        
        private void CreateItems()
        {
            for (int i = 0; i < itemCount; i++)
            {
                GridCell itemCell = GetRandomEmptyCell();
                Vector3 itemPosition = mapSystem.GetWorldPositionFromGridCell(itemCell);
                
                GameObject itemObject = CreateElementObject(itemPrefab, itemPosition, $"Item_{i}");
                ItemElement item = itemObject.GetComponent<ItemElement>();
                
                if (item == null)
                {
                    item = itemObject.AddComponent<ItemElement>();
                }
                
                item.SetGridPosition(itemCell);
                item.SetItemValue(Random.Range(10f, 100f));
                item.AddInteraction($"Item {i} created and positioned");
            }
        }
        
        private void CreateObstacles()
        {
            for (int i = 0; i < obstacleCount; i++)
            {
                GridCell obstacleCell = GetRandomEmptyCell();
                Vector3 obstaclePosition = mapSystem.GetWorldPositionFromGridCell(obstacleCell);
                
                GameObject obstacleObject = CreateElementObject(obstaclePrefab, obstaclePosition, $"Obstacle_{i}");
                ObstacleElement obstacle = obstacleObject.GetComponent<ObstacleElement>();
                
                if (obstacle == null)
                {
                    obstacle = obstacleObject.AddComponent<ObstacleElement>();
                }
                
                obstacle.SetGridPosition(obstacleCell);
                
                bool isDestructible = Random.value > 0.5f;
                obstacle.SetDestructible(isDestructible);
                
                if (isDestructible)
                {
                    obstacle.AddInteraction($"Destructible obstacle {i} created");
                }
                else
                {
                    obstacle.AddInteraction($"Permanent obstacle {i} created");
                }
            }
        }
        
        private GameObject CreateElementObject(GameObject prefab, Vector3 position, string elementName)
        {
            GameObject elementObject;
            
            if (prefab != null)
            {
                elementObject = Instantiate(prefab, position, Quaternion.identity, transform);
            }
            else
            {
                elementObject = new GameObject();
                elementObject.transform.position = position;
                elementObject.transform.SetParent(transform);
                
                SpriteRenderer spriteRenderer = elementObject.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = CreateDefaultSprite();
            }
            
            elementObject.name = elementName;
            return elementObject;
        }
        
        private Sprite CreateDefaultSprite()
        {
            Texture2D texture = new Texture2D(32, 32);
            Color[] pixels = new Color[32 * 32];
            
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Random.ColorHSV();
            }
            
            texture.SetPixels(pixels);
            texture.Apply();
            
            return Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
        }
        
        private GridCell GetRandomEmptyCell()
        {
            GridConfiguration gridConfig = mapSystem.GridSystem.GetGridConfiguration();
            GridCell randomCell;
            int attempts = 0;
            
            do
            {
                int randomRow = Random.Range(0, gridConfig.gridHeight);
                int randomColumn = Random.Range(0, gridConfig.gridWidth);
                randomCell = new GridCell(randomRow, randomColumn);
                attempts++;
            }
            while (!IsCellFree(randomCell) && attempts < 100);
            
            return randomCell;
        }
        
        private bool IsCellFree(GridCell cell)
        {
            if (!mapSystem.IsCellTraversable(cell))
            {
                return false;
            }
            
            var mapCell = mapSystem.GetMapCell(cell);
            return mapCell == null || mapCell.IsEmpty();
        }
        
        private void Update()
        {
            HandlePlayerInput();
        }
        
        private void HandlePlayerInput()
        {
            if (playerCharacter == null)
            {
                return;
            }
            
            GridCell currentCell = playerCharacter.CurrentGridCell;
            
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            {
                var topCell = gridSystem.GetTopCell(currentCell);
                if (topCell.hasValue)
                {
                    TryMovePlayer(topCell.value);
                }
            }
            else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            {
                var bottomCell = gridSystem.GetBottomCell(currentCell);
                if (bottomCell.hasValue)
                {
                    TryMovePlayer(bottomCell.value);
                }
            }
            else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            {
                var leftCell = gridSystem.GetLeftCell(currentCell);
                if (leftCell.hasValue)
                {
                    TryMovePlayer(leftCell.value);
                }
            }
            else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                var rightCell = gridSystem.GetRightCell(currentCell);
                if (rightCell.hasValue)
                {
                    TryMovePlayer(rightCell.value);
                }
            }
            
            if (Input.GetKeyDown(KeyCode.Space))
            {
                PerformPlayerVisionScan();
            }
            
            if (Input.GetMouseButtonDown(0))
            {
                HandleMouseClick();
            }
        }
        
        private void TryMovePlayer(GridCell targetCell)
        {
            if (playerCharacter.TryMoveToCell(targetCell))
            {
                Debug.Log($"Player moved to {targetCell}");
                CheckForItemCollection();
            }
            else
            {
                Debug.Log($"Cannot move to {targetCell} - blocked or invalid");
            }
        }
        
        private void CheckForItemCollection()
        {
            var mapCell = mapSystem.GetMapCell(playerCharacter.CurrentGridCell);
            if (mapCell == null)
            {
                return;
            }
            
            ItemElement[] items = mapCell.GetElementsByType(MapElementType.Item).Cast<ItemElement>().ToArray();
            
            foreach (ItemElement item in items)
            {
                if (item.IsCollectable)
                {
                    item.OnElementInteraction(playerCharacter);
                    Debug.Log($"Player collected: {item.name} (Value: {item.ItemValue})");
                }
            }
        }
        
        private void PerformPlayerVisionScan()
        {
            VisionResult scanResult = playerCharacter.ScanSurroundings();
            
            Debug.Log("=== PLAYER VISION SCAN ===");
            Debug.Log($"Scanned {scanResult.scannedCells.Count} cells");
            Debug.Log($"Found {scanResult.GetElementCount()} elements:");
            
            foreach (MapElement element in scanResult.visibleElements)
            {
                float distance = mapSystem.GetDistanceBetweenElements(playerCharacter, element);
                Debug.Log($"- {element.name} ({element.ElementType}) at {element.CurrentGridCell}, distance: {distance:F1}");
            }
        }
        
        private void HandleMouseClick()
        {
            Vector3 mousePosition = Input.mousePosition;
            GridResult<GridCell> clickResult = gridSystem.ScreenPointToGridCell(mousePosition);
            
            if (clickResult.hasValue)
            {
                Debug.Log($"Clicked on cell: {clickResult.value}");
                
                if (mapSystem.IsNavigationViable(playerCharacter, clickResult.value))
                {
                    playerCharacter.TryMoveToCell(clickResult.value);
                }
                else
                {
                    Debug.Log("Cannot navigate to clicked cell");
                }
            }
        }
        
        private IEnumerator AutoPlayDemo()
        {
            Debug.Log("Starting AutoPlay demo...");
            
            while (true)
            {
                yield return new WaitForSeconds(2.0f);
                
                if (playerCharacter != null)
                {
                    MovePlayerRandomly();
                }
                
                yield return new WaitForSeconds(1.0f);
                
                MoveNPCsRandomly();
                
                yield return new WaitForSeconds(1.0f);
                
                PerformRandomVisionCheck();
            }
        }
        
        private void MovePlayerRandomly()
        {
            GridCell[] adjacentCells = mapSystem.GetAdjacentTraversableCells(playerCharacter.CurrentGridCell);
            
            if (adjacentCells.Length > 0)
            {
                GridCell randomCell = adjacentCells[Random.Range(0, adjacentCells.Length)];
                playerCharacter.TryMoveToCell(randomCell);
                Debug.Log($"AutoPlay: Player moved to {randomCell}");
            }
        }
        
        private void MoveNPCsRandomly()
        {
            foreach (CharacterElement npc in npcs)
            {
                if (npc != null && Random.value > 0.5f)
                {
                    GridCell[] adjacentCells = mapSystem.GetAdjacentTraversableCells(npc.CurrentGridCell);
                    
                    if (adjacentCells.Length > 0)
                    {
                        GridCell randomCell = adjacentCells[Random.Range(0, adjacentCells.Length)];
                        npc.TryMoveToCell(randomCell);
                        Debug.Log($"AutoPlay: {npc.name} moved to {randomCell}");
                    }
                }
            }
        }
        
        private void PerformRandomVisionCheck()
        {
            if (Random.value > 0.7f)
            {
                CharacterElement randomCharacter = npcs[Random.Range(0, npcs.Length)];
                if (randomCharacter != null)
                {
                    ViewDirection randomDirection = (ViewDirection)Random.Range(0, 6);
                    VisionResult visionResult = randomCharacter.LookInDirection(randomDirection);
                    
                    Debug.Log($"AutoPlay: {randomCharacter.name} looked {randomDirection} and saw {visionResult.GetElementCount()} elements");
                }
            }
        }
        
        [ContextMenu("Reset Game")]
        public void ResetGame()
        {
            foreach (Transform child in transform)
            {
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
            
            if (Application.isPlaying)
            {
                StartCoroutine(InitializeGameDelayed());
            }
        }
    }
}