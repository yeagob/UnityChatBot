using System;
using InventorySystem.Enums;

namespace InventorySystem.Models
{
    [Serializable]
    public struct InventoryItem
    {
        public string itemId;
        public ItemType itemType;
        public int quantity;
        public float itemValue;

        public InventoryItem(string itemId, ItemType itemType, int quantity, float itemValue)
        {
            this.itemId = itemId;
            this.itemType = itemType;
            this.quantity = quantity;
            this.itemValue = itemValue;
        }

        public static InventoryItem Empty()
        {
            return new InventoryItem(string.Empty, ItemType.Key, 0, 0.0f);
        }

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(itemId) && quantity > 0;
        }

        public float GetTotalValue()
        {
            return itemValue * quantity;
        }
    }
}