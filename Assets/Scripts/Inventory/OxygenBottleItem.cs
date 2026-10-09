using UnityEngine;

namespace InventoryFramework
{
    [CreateAssetMenu(fileName = "New Oxygen Bottle", menuName = "Inventory/Oxygen Bottle")]
    public class OxygenBottleItem : Item
    {
        [Header("Oxygen Capacity")]
        public float maxOxygenCapacity = 100f;
    }
}
