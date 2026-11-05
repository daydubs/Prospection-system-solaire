using UnityEngine;
using InventoryFramework;

namespace BuildingSystem {
    [CreateAssetMenu(fileName = "New Blueprint Item", menuName = "Inventory/Blueprint Item")]
    public class BlueprintItem : Item
    {
        public BaseModuleData moduleData;
    }
}
