using UnityEngine;

namespace InventoryFramework
{
    [System.Serializable]
    public class InventorySlot
    {
        public Item item;
        public int count;
        public float currentOxygen = 100f;
        public float maxOxygen = 100f;
        [SerializeReference] public BatteryInstanceState battery;

        public bool IsEmpty => item == null;
        public void Clear()
        {
            item = null;
            count = 0;
            currentOxygen = maxOxygen = 100f;
            battery = null;
        }
        public void CopyFrom(InventorySlot source)
        {
            item = source.item;
            count = source.count;
            currentOxygen = source.currentOxygen;
            maxOxygen = source.maxOxygen;
            battery = source.battery;
        }
        public void SwapWith(InventorySlot other)
        {
            var temporary = new InventorySlot();
            temporary.CopyFrom(this);
            CopyFrom(other);
            other.CopyFrom(temporary);
        }
    }
}


