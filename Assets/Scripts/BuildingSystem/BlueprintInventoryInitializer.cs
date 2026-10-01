using UnityEngine;
using System.Collections.Generic;
using InventoryFramework;

namespace BuildingSystem {
    public class BlueprintInventoryInitializer : MonoBehaviour
    {
        [Tooltip("The separate inventory component dedicated to holding blueprints.")]
        public Inventory blueprintInventory;

        [Tooltip("The blueprints the player should start with.")]
        public List<BlueprintItem> startingBlueprints;

        private void Start()
        {
            if (blueprintInventory == null)
            {
                Debug.LogWarning("[BlueprintInventoryInitializer] No blueprint inventory assigned!");
                return;
            }

            foreach (var bp in startingBlueprints)
            {
                if (bp != null)
                {
                    // Add 1 copy of each blueprint to the dedicated inventory
                    blueprintInventory.AddItem(bp, 1);
                }
            }
        }
    }
}
