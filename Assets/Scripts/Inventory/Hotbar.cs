using System.Collections.Generic;
using UnityEngine;

namespace InventoryFramework
{
    public class Hotbar : MonoBehaviour
    {
        public int size = 9;
        public List<InventorySlot> slots;

        void Awake()
        {
            slots = new List<InventorySlot>(new InventorySlot[size]);
            for (int i = 0; i < size; i++)
            {
                slots[i] = new InventorySlot();
            }
        }

        public InventorySlot GetSlot(int index)
        {
            if (index < 0 || index >= size) return null;

            return slots[index];
        }

        public int GetItemCount(Item item)
        {
            if (item == null) return 0;
            int found = 0;
            foreach (var slot in slots)
            {
                if (!slot.IsEmpty && (slot.item == item || (item.id != 0 && slot.item.id == item.id)))
                {
                    found += slot.count;
                }
            }
            return found;
        }

        public bool HasItem(Item item, int amount)
        {
            int found = 0;
            foreach (var slot in slots)
            {
                if (!slot.IsEmpty && slot.item == item)
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
                if (!slot.IsEmpty && slot.item == item)
                {
                    if (slot.count >= remainingToConsume)
                    {
                        slot.count -= remainingToConsume;
                        if (slot.count == 0) slot.item = null;
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

        public bool AddItem(Item newItem, int amount = 1)
        {
            if (newItem == null || amount <= 0) return false;
            if (slots == null) return false;

            int maxStack = newItem.maxStack > 0 ? newItem.maxStack : 1;

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

            return false;
        }
    }

}
