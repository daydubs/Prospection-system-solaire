using UnityEngine;

namespace InventoryFramework
{
    public class ItemPickupHandler : MonoBehaviour
    {
        public Hotbar hotbar;
        public Inventory inventory;

        public bool PickupItem(Item item, int amount = 1)
        {
            if (inventory == null) inventory = FindAnyObjectByType<Inventory>();
            if (hotbar == null) hotbar = FindAnyObjectByType<Hotbar>();

            bool addedToInventory = inventory != null && inventory.AddItem(item, amount);
            bool addedToHotbar = false;

            if (!addedToInventory && hotbar != null)
            {
                addedToHotbar = hotbar.AddItem(item, amount);

                if (!addedToHotbar)
                {
                    Debug.Log("Both hotbar and inventory full! Dropping item...");
                }
            }

            var hotbarUI = FindAnyObjectByType<HotbarUI>();
            if (hotbarUI != null) hotbarUI.RefreshUI();

            var invUI = FindAnyObjectByType<InventoryUI>();
            if (invUI != null) invUI.RefreshUI();

            return addedToInventory || addedToHotbar;
        }
    }
}
