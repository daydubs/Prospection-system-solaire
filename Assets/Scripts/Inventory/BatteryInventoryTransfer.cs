using System.Collections.Generic;
namespace InventoryFramework
{
    public static class BatteryInventoryTransfer
    {
        public static bool TryCraft(CraftingSystem.CraftingRecipe recipe, int quantity, Inventory inventory, bool commit)
        {
            if (recipe == null || recipe.outputItem == null || !recipe.outputItem.IsBattery || quantity <= 0 || recipe.outputAmount <= 0 || recipe.ingredients == null || inventory == null) return false;
            long produced = (long)quantity * recipe.outputAmount;
            if (produced > int.MaxValue) return false;
            var originals = new List<InventorySlot>();
            if (inventory.slots != null) originals.AddRange(inventory.slots);
            if (inventory.hotbar != null && inventory.hotbar.slots != null) originals.AddRange(inventory.hotbar.slots);
            var simulated = new List<InventorySlot>();
            foreach (var slot in originals)
            {
                var copy = new InventorySlot();
                if (slot != null) copy.CopyFrom(slot);
                simulated.Add(copy);
            }
            foreach (var ingredient in recipe.ingredients)
            {
                if (ingredient == null || ingredient.item == null) continue;
                long remaining = (long)ingredient.amount * quantity;
                if (remaining <= 0 || remaining > int.MaxValue) return false;
                foreach (var slot in simulated)
                {
                    if (slot.item == null || !(slot.item == ingredient.item || (ingredient.item.id != 0 && slot.item.id == ingredient.item.id))) continue;
                    int take = (int)System.Math.Min(remaining, slot.count);
                    slot.count -= take;
                    remaining -= take;
                    if (slot.count == 0) slot.Clear();
                    if (remaining == 0) break;
                }
                if (remaining != 0) return false;
            }
            int free = 0;
            for (int i = 0; i < originals.Count; i++) if (originals[i] != null && simulated[i].IsEmpty) free++;
            if (free < produced) return false;
            if (!commit) return true;
            // No callbacks between validation and commit: existing states stay attached to their owners.
            for (int i = 0; i < originals.Count; i++) if (originals[i] != null) originals[i].CopyFrom(simulated[i]);
            return TryCreate(recipe.outputItem, (int)produced, inventory.slots, inventory.hotbar != null ? inventory.hotbar.slots : null);
        }

        public static int FreeSlots(List<InventorySlot> slots)
        {
            int free = 0;
            if (slots != null) foreach (var slot in slots) if (slot != null && slot.IsEmpty) free++;
            return free;
        }
        public static bool TryCreate(Item item, int amount, List<InventorySlot> primary, List<InventorySlot> secondary = null)
        {
            if (item == null || !item.IsBattery || amount <= 0 || FreeSlots(primary) + (primary == secondary ? 0 : FreeSlots(secondary)) < amount) return false;
            for (int i = 0; i < amount; i++)
                TryReturn(item, item.BatteryDefinition.CreateInstance(), primary, secondary);
            return true;
        }
        public static bool TryReturn(Item item, BatteryInstanceState state, List<InventorySlot> primary, List<InventorySlot> secondary = null)
        {
            if (item == null || !item.IsBattery || state == null) return false;
            var destination = FirstEmpty(primary) ?? FirstEmpty(secondary);
            if (destination == null) return false;
            destination.Clear();
            destination.item = item;
            destination.count = 1;
            destination.battery = state;
            return true;
        }
        private static InventorySlot FirstEmpty(List<InventorySlot> slots)
        {
            if (slots != null) foreach (var slot in slots) if (slot != null && slot.IsEmpty) return slot;
            return null;
        }
        public static InventorySlot Find(Item expected, Inventory inventory, Hotbar hotbar, int selectedIndex = -1)
        {
            if (hotbar != null && hotbar.slots != null && selectedIndex >= 0 && selectedIndex < hotbar.slots.Count && Matches(hotbar.slots[selectedIndex], expected)) return hotbar.slots[selectedIndex];
            if (inventory != null && inventory.slots != null) foreach (var slot in inventory.slots) if (Matches(slot, expected)) return slot;
            if (hotbar != null && hotbar.slots != null) foreach (var slot in hotbar.slots) if (Matches(slot, expected)) return slot;
            return null;
        }
        public static bool Matches(InventorySlot slot, Item expected) => slot != null && slot.item != null && slot.item.IsBattery && slot.count == 1 && slot.battery != null && expected != null && (slot.item == expected || (expected.id != 0 && slot.item.id == expected.id));
    }
}
