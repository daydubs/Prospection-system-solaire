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

        public bool IsEmpty => item == null;
    }
}


