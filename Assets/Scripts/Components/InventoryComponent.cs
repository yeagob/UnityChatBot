using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using InventorySystem.Configuration;
using InventorySystem.Enums;
using InventorySystem.Models;

namespace InventorySystem.Components
{
    public class InventoryComponent : MonoBehaviour
    {
        [Header("Inventory Configuration")]
        [SerializeField] private int maxInventorySize = InventoryConfiguration.MaxInventorySize;
        
        [Header("Current Items")]
        [SerializeField] private List<InventoryItem> items = new List<InventoryItem>();

        public int MaxInventorySize => maxInventorySize;
        public int CurrentItemCount => items.Count;
        public List<InventoryItem> Items => new List<InventoryItem>(items);

        private void Awake()
        {
            if (items == null)
            {
                items = new List<InventoryItem>();
            }
        }

        public bool CanAddItem(ItemType itemType, int quantity = 1)
        {
            if (quantity <= 0)
            {
                return false;
            }

            InventoryItem existingItem = items.FirstOrDefault(item => item.itemType == itemType);
            
            if (existingItem.IsValid())
            {
                int maxStackSize = GetMaxStackSize(itemType);
                return existingItem.quantity + quantity <= maxStackSize;
            }

            return items.Count < maxInventorySize;
        }

        public bool AddItem(string itemId, ItemType itemType, int quantity = 1, float itemValue = InventoryConfiguration.DefaultItemValue)
        {
            if (!CanAddItem(itemType, quantity))
            {
                return false;
            }

            InventoryItem existingItem = items.FirstOrDefault(item => item.itemType == itemType);
            
            if (existingItem.IsValid())
            {
                int existingIndex = items.FindIndex(item => item.itemType == itemType);
                InventoryItem updatedItem = existingItem;
                updatedItem.quantity += quantity;
                items[existingIndex] = updatedItem;
            }
            else
            {
                InventoryItem newItem = new InventoryItem(itemId, itemType, quantity, itemValue);
                items.Add(newItem);
            }

            return true;
        }

        public bool RemoveItem(ItemType itemType, int quantity = 1)
        {
            if (quantity <= 0)
            {
                return false;
            }

            InventoryItem existingItem = items.FirstOrDefault(item => item.itemType == itemType);
            
            if (!existingItem.IsValid() || existingItem.quantity < quantity)
            {
                return false;
            }

            int existingIndex = items.FindIndex(item => item.itemType == itemType);
            
            if (existingItem.quantity == quantity)
            {
                items.RemoveAt(existingIndex);
            }
            else
            {
                InventoryItem updatedItem = existingItem;
                updatedItem.quantity -= quantity;
                items[existingIndex] = updatedItem;
            }

            return true;
        }

        public bool HasItem(ItemType itemType, int quantity = 1)
        {
            InventoryItem existingItem = items.FirstOrDefault(item => item.itemType == itemType);
            return existingItem.IsValid() && existingItem.quantity >= quantity;
        }

        public int GetItemCount(ItemType itemType)
        {
            InventoryItem existingItem = items.FirstOrDefault(item => item.itemType == itemType);
            return existingItem.IsValid() ? existingItem.quantity : 0;
        }

        public InventoryItem GetItem(ItemType itemType)
        {
            return items.FirstOrDefault(item => item.itemType == itemType);
        }

        public void ClearInventory()
        {
            items.Clear();
        }

        public string GetInventoryDescription()
        {
            if (items.Count == 0)
            {
                return "Empty inventory";
            }

            List<string> itemDescriptions = new List<string>();
            
            foreach (InventoryItem item in items)
            {
                string description = $"{item.itemType}";
                if (item.quantity > 1)
                {
                    description += $" x{item.quantity}";
                }
                itemDescriptions.Add(description);
            }

            return string.Join(", ", itemDescriptions);
        }

        private int GetMaxStackSize(ItemType itemType)
        {
            switch (itemType)
            {
                case ItemType.Key:
                    return InventoryConfiguration.MaxStackSizeKey;
                case ItemType.Money:
                    return InventoryConfiguration.MaxStackSizeMoney;
                case ItemType.Apple:
                    return InventoryConfiguration.MaxStackSizeApple;
                default:
                    return 1;
            }
        }
    }
}