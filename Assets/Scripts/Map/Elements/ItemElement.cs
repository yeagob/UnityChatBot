using UnityEngine;
using MapSystem.Enums;

namespace MapSystem.Elements
{
    public class ItemElement : MapElement
    {
        [Header("Item Properties")]
        [SerializeField] private bool isCollectable = true;
        [SerializeField] private bool isStackable = false;
        [SerializeField] private int stackSize = 1;
        [SerializeField] private float itemValue = 1.0f;
        
        public bool IsCollectable => isCollectable;
        public bool IsStackable => isStackable;
        public int StackSize => stackSize;
        public float ItemValue => itemValue;
        
        protected override void InitializeMapElement()
        {
            elementType = MapElementType.Item;
            visionDistance = 1;
            base.InitializeMapElement();
        }
        
        protected override void CreateDefaultContext()
        {
            base.CreateDefaultContext();
            
            context.SetProperty("isCollectable", isCollectable);
            context.SetProperty("isStackable", isStackable);
            context.SetProperty("stackSize", stackSize);
            context.SetProperty("itemValue", itemValue);
            
            context.AddInteraction("Item created and initialized");
        }
        
        public override void OnElementInteraction(MapElement interactor)
        {
            if (interactor == null)
            {
                return;
            }
            
            string interactionMessage = $"Interacted with by {interactor.name} ({interactor.ElementType})";
            context.AddInteraction(interactionMessage);
            
            if (isCollectable && interactor.ElementType == MapElementType.Character)
            {
                OnItemCollected(interactor);
            }
        }
        
        protected virtual void OnItemCollected(MapElement collector)
        {
            context.AddInteraction($"Collected by {collector.name}");
            
            if (mapSystem != null)
            {
                mapSystem.UnregisterElement(this);
            }
            
            gameObject.SetActive(false);
        }
        
        public override bool CanBeTraversed()
        {
            return true;
        }
        
        public void SetCollectable(bool collectable)
        {
            isCollectable = collectable;
            context.SetProperty("isCollectable", isCollectable);
            context.AddInteraction($"Collectable status changed to {isCollectable}");
        }
        
        public void SetStackable(bool stackable)
        {
            isStackable = stackable;
            context.SetProperty("isStackable", isStackable);
            context.AddInteraction($"Stackable status changed to {isStackable}");
        }
        
        public void SetStackSize(int newStackSize)
        {
            stackSize = Mathf.Max(1, newStackSize);
            context.SetProperty("stackSize", stackSize);
            context.AddInteraction($"Stack size changed to {stackSize}");
        }
        
        public void SetItemValue(float newValue)
        {
            itemValue = Mathf.Max(0.0f, newValue);
            context.SetProperty("itemValue", itemValue);
            context.AddInteraction($"Item value changed to {itemValue}");
        }
        
        protected override void DrawVisionRadius()
        {
            base.DrawVisionRadius();
            
            if (isCollectable)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(transform.position, Vector3.one * 0.8f);
            }
        }
    }
}