using System;
using System.Collections.Generic;

namespace InventoryFramework
{
    public static class InventoryRefundTransaction
    {
        public static bool TryApply(IEnumerable<KeyValuePair<Item, int>> amounts, Inventory inventory, Hotbar hotbar, bool commit, out string reason)
        {
            reason = null;
            var originals = new List<InventorySlot>();
            if (inventory != null && inventory.slots != null) originals.AddRange(inventory.slots);
            if (hotbar != null && hotbar.slots != null) originals.AddRange(hotbar.slots);
            var simulated = new List<InventorySlot>();
            var before = new List<InventorySlot>();
            foreach (var slot in originals)
            {
                if (slot == null) { reason = "Inventaire non initialisé"; return false; }
                var copy = new InventorySlot(); copy.CopyFrom(slot); simulated.Add(copy);
                var snapshot = new InventorySlot(); snapshot.CopyFrom(slot); before.Add(snapshot);
            }
            var totals = new Dictionary<Item, int>();
            if (amounts == null) { reason = "Reçu absent"; return false; }
            foreach (var pair in amounts)
            {
                if (pair.Key == null || pair.Value <= 0) { reason = "Remboursement invalide"; return false; }
                // A recipe specifies materials, not a charged instance. Never manufacture stateful items from a receipt.
                if (pair.Key.IsBattery || pair.Key is OxygenBottleItem) { reason = "Ingrédient à état individuel : remboursement spécifique requis"; return false; }
                try { totals[pair.Key] = checked((totals.TryGetValue(pair.Key, out int n) ? n : 0) + pair.Value); }
                catch (OverflowException) { reason = "Quantité excessive"; return false; }
            }
            foreach (var pair in totals)
            {
                foreach (var other in totals)
                    if (pair.Key != other.Key && pair.Key.id != 0 && pair.Key.id == other.Key.id) { reason = "Conflit d'ID entre matériaux"; return false; }
                foreach (var slot in originals)
                    if (!slot.IsEmpty && slot.item != pair.Key && pair.Key.id != 0 && slot.item.id == pair.Key.id) { reason = "Conflit d'ID dans l'inventaire"; return false; }
                int remaining = pair.Value;
                int maximum = Math.Max(1, pair.Key.maxStack);
                foreach (var slot in simulated)
                {
                    if (slot.item != pair.Key || slot.count >= maximum) continue;
                    int take = Math.Min(remaining, maximum - slot.count);
                    slot.count += take; remaining -= take;
                    if (remaining == 0) break;
                }
                foreach (var slot in simulated)
                {
                    if (remaining == 0) break;
                    if (!slot.IsEmpty) continue;
                    slot.Clear(); slot.item = pair.Key;
                    slot.count = Math.Min(maximum, remaining); remaining -= slot.count;
                }
                if (remaining > 0) { reason = "Inventaire et barre rapide : espace insuffisant"; return false; }
            }
            if (!commit) return true;
            try
            {
                for (int i = 0; i < originals.Count; i++)
                {
                    var a = originals[i]; var b = before[i];
                    if (a.item != b.item || a.count != b.count || a.battery != b.battery || a.currentOxygen != b.currentOxygen || a.maxOxygen != b.maxOxygen)
                    { reason = "Inventaire modifié pendant la restitution"; return false; }
                }
                for (int i = 0; i < originals.Count; i++) originals[i].CopyFrom(simulated[i]);
                return true;
            }
            catch
            {
                for (int i = 0; i < originals.Count; i++) originals[i].CopyFrom(before[i]);
                reason = "Échec de restitution : inventaire restauré"; return false;
            }
        }
    }
}
