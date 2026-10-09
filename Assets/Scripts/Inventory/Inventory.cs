using System.Collections.Generic;
using UnityEngine;

namespace InventoryFramework
{
    public class Inventory : MonoBehaviour
    {
        public static Inventory Instance { get; private set; }

        public int size = 36;
        public List<InventorySlot> slots;

        [Header("Oxygen Equipment Slots")]
        public int oxygenSlotCount = 4;
        public List<InventorySlot> oxygenSlots;

        [Header("Resource References")]
        public Item iceResourceItem;
        public Item oxygenBottleItem;

        public Hotbar hotbar;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.playerInventory = this;
            }

            if (slots == null || slots.Count != size)
            {
                slots = new List<InventorySlot>(new InventorySlot[size]);
                for (int i = 0; i < size; i++)
                {
                    slots[i] = new InventorySlot();
                }
            }
            else
            {
                for (int i = 0; i < slots.Count; i++)
                {
                    if (slots[i] == null) slots[i] = new InventorySlot();
                }
            }

            if (oxygenSlots == null || oxygenSlots.Count != oxygenSlotCount)
            {
                oxygenSlots = new List<InventorySlot>(new InventorySlot[oxygenSlotCount]);
                for (int i = 0; i < oxygenSlotCount; i++)
                {
                    oxygenSlots[i] = new InventorySlot();
                }
            }
            else
            {
                for (int i = 0; i < oxygenSlots.Count; i++)
                {
                    if (oxygenSlots[i] == null) oxygenSlots[i] = new InventorySlot();
                }
            }

            if (hotbar == null)
            {
                hotbar = GetComponent<Hotbar>() ?? FindAnyObjectByType<Hotbar>();
            }

            #if UNITY_EDITOR
            if (iceResourceItem == null)
            {
                iceResourceItem = UnityEditor.AssetDatabase.LoadAssetAtPath<Item>("Assets/Items/IceResource.asset");
            }
            if (oxygenBottleItem == null)
            {
                oxygenBottleItem = UnityEditor.AssetDatabase.LoadAssetAtPath<Item>("Assets/Items/OxygenBottle.asset");
            }
            #endif
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            if (GameManager.Instance != null && GameManager.Instance.playerInventory == this)
            {
                GameManager.Instance.playerInventory = null;
            }
        }

        public bool IsOxygenBottle(Item item)
        {
            if (item == null) return false;
            if (item is OxygenBottleItem) return true;
            if (oxygenBottleItem != null && (item == oxygenBottleItem || (oxygenBottleItem.id != 0 && item.id == oxygenBottleItem.id))) return true;

            string name = item.itemName ?? "";
            return name.IndexOf("Oxygen", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                   name.IndexOf("Oxygène", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                   name.IndexOf("O2", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public int GetItemCount(Item item)
        {
            if (item == null) return 0;
            int found = 0;
            if (slots != null)
            {
                foreach (var slot in slots)
                {
                    if (!slot.IsEmpty && (slot.item == item || (item.id != 0 && slot.item.id == item.id)))
                    {
                        found += slot.count;
                    }
                }
            }
            return found;
        }

        public float ConsumeEquippedOxygen(float amount)
        {
            if (amount <= 0f || oxygenSlots == null) return 0f;

            float remaining = amount;
            foreach (var slot in oxygenSlots)
            {
                if (slot != null && !slot.IsEmpty && IsOxygenBottle(slot.item) && slot.currentOxygen > 0f)
                {
                    float take = Mathf.Min(remaining, slot.currentOxygen);
                    slot.currentOxygen -= take;
                    remaining -= take;
                    if (remaining <= 0.0001f) break;
                }
            }

            return amount - remaining;
        }

        public bool RefillOxygenBottleWithIce()
        {
            Item ice = iceResourceItem;
            #if UNITY_EDITOR
            if (ice == null)
            {
                ice = UnityEditor.AssetDatabase.LoadAssetAtPath<Item>("Assets/Items/IceResource.asset");
            }
            #endif

            if (ice == null)
            {
                NotifyNotification("Erreur : ressource Glace non trouvée !");
                return false;
            }

            int availableIce = GetItemCount(ice);
            if (hotbar != null)
            {
                availableIce += hotbar.GetItemCount(ice);
            }

            if (availableIce < 1)
            {
                NotifyNotification("Pas assez de glace ! Nécessite 1 unité de glace.");
                return false;
            }

            // Find the oxygen bottle with the lowest oxygen (< 100) in oxygenSlots
            InventorySlot targetSlot = null;
            float lowestOxygen = 100f;

            if (oxygenSlots != null)
            {
                foreach (var s in oxygenSlots)
                {
                    if (s != null && !s.IsEmpty && IsOxygenBottle(s.item) && s.currentOxygen < 100f)
                    {
                        if (s.currentOxygen < lowestOxygen)
                        {
                            lowestOxygen = s.currentOxygen;
                            targetSlot = s;
                        }
                    }
                }
            }

            // If none found in oxygen slots, check main slots
            if (targetSlot == null && slots != null)
            {
                foreach (var s in slots)
                {
                    if (s != null && !s.IsEmpty && IsOxygenBottle(s.item) && s.currentOxygen < 100f)
                    {
                        if (s.currentOxygen < lowestOxygen)
                        {
                            lowestOxygen = s.currentOxygen;
                            targetSlot = s;
                        }
                    }
                }
            }

            if (targetSlot == null)
            {
                bool hasAnyBottle = false;
                if (oxygenSlots != null)
                {
                    foreach (var s in oxygenSlots)
                    {
                        if (s != null && !s.IsEmpty && IsOxygenBottle(s.item))
                        {
                            hasAnyBottle = true;
                            break;
                        }
                    }
                }

                if (hasAnyBottle)
                {
                    NotifyNotification("Toutes les bouteilles d'oxygène sont déjà pleines (100%) !");
                }
                else
                {
                    NotifyNotification("Aucune bouteille d'oxygène à remplir dans les slots d'oxygène !");
                }
                return false;
            }

            // Consume 1 unit of ice
            bool consumed = ConsumeItem(ice, 1);
            if (!consumed && hotbar != null)
            {
                consumed = hotbar.ConsumeItem(ice, 1);
            }

            if (consumed)
            {
                targetSlot.currentOxygen = 100f;
                NotifyNotification("Bouteille d'oxygène rechargée à 100% (-1 Glace) !");
                return true;
            }

            return false;
        }

        private void NotifyNotification(string message)
        {
            Debug.Log($"[Inventory] {message}");
            if (MainMenuController.Instance != null)
            {
                MainMenuController.Instance.ShowNotification(message);
            }
        }

        public bool AddItem(Item newItem, int amount = 1)
        {
            if (newItem == null || amount <= 0) return false;
            if (slots == null) return false;

            int maxStack = newItem.maxStack > 0 ? newItem.maxStack : 1;

            // Stacking if possible
            if (maxStack > 1)
            {
                foreach (var slot in slots)
                {
                    if (slot != null && !slot.IsEmpty && (slot.item == newItem || (newItem.id != 0 && slot.item != null && slot.item.id == newItem.id)) && slot.count < maxStack)
                    {
                        int space = maxStack - slot.count;
                        int add = Mathf.Min(space, amount);
                        slot.count += add;
                        amount -= add;

                        if (amount <= 0) return true;
                    }
                }
            }

            // Add to empty slot
            foreach (var slot in slots)
            {
                if (slot != null && slot.IsEmpty)
                {
                    int add = Mathf.Min(maxStack, amount);
                    slot.item = newItem;
                    slot.count = add;
                    slot.currentOxygen = 100f;
                    amount -= add;

                    if (amount <= 0) return true;
                }
            }

            return false; // Inventory is full or partially full
        }

        public bool HasItem(Item item, int amount)
        {
            int found = 0;
            foreach (var slot in slots)
            {
                if (!slot.IsEmpty && (slot.item == item || (item != null && item.id != 0 && slot.item.id == item.id)))
                {
                    found += slot.count;
                    if (found >= amount) return true;
                }
            }
            return false;
        }

        public bool ConsumeItem(Item item, int amount)
        {
            if (!HasItem(item, amount)) return false;

            int remainingToConsume = amount;
            foreach (var slot in slots)
            {
                if (!slot.IsEmpty && (slot.item == item || (item != null && item.id != 0 && slot.item.id == item.id)))
                {
                    if (slot.count >= remainingToConsume)
                    {
                        slot.count -= remainingToConsume;
                        if (slot.count == 0) slot.item = null; // Clear slot if empty
                        return true;
                    }
                    else
                    {
                        remainingToConsume -= slot.count;
                        slot.count = 0;
                        slot.item = null;
                    }
                }
            }
            return true;
        }

        public void MoveOrSwap(int from, int to)
        {
            if (from == to) return;

            var slotFrom = slots[from];
            var slotTo = slots[to];

            // Move to Empty Slot
            if (slotTo.IsEmpty)
            {
                slotTo.item = slotFrom.item;
                slotTo.count = slotFrom.count;
                slotTo.currentOxygen = slotFrom.currentOxygen;

                slotFrom.item = null;
                slotFrom.count = 0;
                slotFrom.currentOxygen = 100f;
            }
            else
            {
                // Swap Items
                var tmpItem = slotFrom.item;
                var tmpCount = slotFrom.count;
                var tmpOxygen = slotFrom.currentOxygen;

                slotFrom.item = slotTo.item;
                slotFrom.count = slotTo.count;
                slotFrom.currentOxygen = slotTo.currentOxygen;

                slotTo.item = tmpItem;
                slotTo.count = tmpCount;
                slotTo.currentOxygen = tmpOxygen;
            }
        }
    }
}

