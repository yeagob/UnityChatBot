using UnityEngine;
using GridSystem.Models.Grid;
using MapSystem.Enums;
using MapSystem.Models.Context;

namespace MapSystem
{
    public abstract class MapElement : MonoBehaviour
    {
        [Header("Map Element Configuration")]
        [SerializeField] protected MapElementType elementType;
        [SerializeField] protected int visionDistance = 3;
        [SerializeField] protected GridCell currentGridCell;
        
        [Header("Visual Representation")]
        [SerializeField] protected SpriteRenderer elementSprite;
        [SerializeField] protected Sprite defaultSprite;
        
        [Header("Context Data")]
        [SerializeField] protected MapElementContext context;
        
        protected MapSystem mapSystem;
        protected bool isInitialized = false;
        
        public MapElementType ElementType => elementType;
        public int VisionDistance => visionDistance;
        public GridCell CurrentGridCell => currentGridCell;
        public MapElementContext Context => context;
        public SpriteRenderer ElementSprite => elementSprite;
        
        protected virtual void Awake()
        {
            InitializeMapElement();
        }
        
        protected virtual void Start()
        {
            RegisterWithMapSystem();
        }
        
        protected virtual void InitializeMapElement()
        {
            if (elementSprite == null)
            {
                elementSprite = GetComponent<SpriteRenderer>();
                if (elementSprite == null)
                {
                    elementSprite = gameObject.AddComponent<SpriteRenderer>();
                }
            }
            
            if (elementSprite.sprite == null && defaultSprite != null)
            {
                elementSprite.sprite = defaultSprite;
            }
            
            if (context == null)
            {
                CreateDefaultContext();
            }
            
            isInitialized = true;
        }
        
        protected virtual void CreateDefaultContext()
        {
            context = new MapElementContext(
                gameObject.name,
                $"A {elementType} element in the map"
            );
            
            context.SetProperty("elementType", elementType);
            context.SetProperty("visionDistance", visionDistance);
            context.initialPosition = transform.position;
        }
        
        protected virtual void RegisterWithMapSystem()
        {
            mapSystem = FindObjectOfType<MapSystem>();
            if (mapSystem != null)
            {
                mapSystem.RegisterElement(this);
                context.AddInteraction($"Registered with MapSystem at cell {currentGridCell}");
            }
        }
        
        public virtual void SetGridPosition(GridCell newCell)
        {
            GridCell previousCell = currentGridCell;
            currentGridCell = newCell;
            
            if (mapSystem != null)
            {
                Vector3 worldPosition = mapSystem.GetWorldPositionFromGridCell(newCell);
                transform.position = worldPosition;
                
                context.AddInteraction($"Moved from {previousCell} to {newCell}");
            }
        }
        
        public virtual void UpdateWorldPosition()
        {
            if (mapSystem != null)
            {
                Vector3 worldPosition = mapSystem.GetWorldPositionFromGridCell(currentGridCell);
                transform.position = worldPosition;
            }
        }
        
        public virtual void SetVisionDistance(int newVisionDistance)
        {
            visionDistance = Mathf.Max(0, newVisionDistance);
            context.SetProperty("visionDistance", visionDistance);
            context.AddInteraction($"Vision distance changed to {visionDistance}");
        }
        
        public virtual void SetSprite(Sprite newSprite)
        {
            if (elementSprite != null && newSprite != null)
            {
                elementSprite.sprite = newSprite;
                context.AddInteraction($"Sprite changed to {newSprite.name}");
            }
        }
        
        public virtual void AddInteraction(string interaction)
        {
            context.AddInteraction(interaction);
        }
        
        public virtual float GetDistanceToElement(MapElement otherElement)
        {
            if (otherElement == null || mapSystem == null)
            {
                return float.MaxValue;
            }
            
            return mapSystem.GetDistanceBetweenElements(this, otherElement);
        }
        
        public virtual bool CanSeeElement(MapElement otherElement)
        {
            if (otherElement == null)
            {
                return false;
            }
            
            float distance = GetDistanceToElement(otherElement);
            return distance <= visionDistance && distance <= otherElement.visionDistance;
        }
        
        public abstract void OnElementInteraction(MapElement interactor);
        
        public abstract bool CanBeTraversed();
        
        protected virtual void OnDestroy()
        {
            if (mapSystem != null)
            {
                mapSystem.UnregisterElement(this);
            }
        }
        
        protected virtual void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying)
            {
                return;
            }
            
            DrawVisionRadius();
        }
        
        protected virtual void DrawVisionRadius()
        {
            if (mapSystem == null)
            {
                return;
            }
            
            Gizmos.color = Color.cyan;
            float cellSize = 1.0f;
            float radius = visionDistance * cellSize;
            
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}