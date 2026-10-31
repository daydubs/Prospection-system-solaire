using UnityEngine;
using BuildingSystem.Cables;
using System.Collections.Generic;

namespace BuildingSystem.Industry
{
    [RequireComponent(typeof(Collider))]
    public class CrusherUnit : MonoBehaviour, IPromptInteractable
    {
        [Header("Connections")]
        [SerializeField] private ElectricConnectorSocket powerOutput;
        [SerializeField] private MaterialConnectorSocket rubbleInput;
        [SerializeField] private List<InventoryFramework.Item> powderItem;
        [Header("Temporary power — no battery item yet")]
        [Tooltip("Unlimited test power. Disable when a real battery provider is available.")]
        [SerializeField] private bool testPowerEnabled = true;
        private bool _batteryPowerAvailable;
        [Header("Production")]
        [SerializeField, Min(0.1f)] private float crushingInterval = 5f;
        [SerializeField, Min(1)] private int inputCapacity = 100;
        [SerializeField, Min(1)] private int outputCapacity = 100;
        [SerializeField] private int rubbleStock;
        [SerializeField] private int powderStock;
        private float _progress;
        public bool HasEnergy => isActiveAndEnabled && (testPowerEnabled || _batteryPowerAvailable);
        public int RubbleStock => rubbleStock;
        public int PowderStock => powderStock;
        public bool CanAcceptRubble => isActiveAndEnabled && rubbleStock < inputCapacity;
        public ElectricConnectorSocket PowerOutput => powerOutput;
        public MaterialConnectorSocket RubbleInput => rubbleInput;

        private void OnEnable() { RefreshPower(); }
        private void OnDisable() { if (powerOutput != null) powerOutput.SetPowered(false); }
        private void Update() { AdvanceProduction(Time.deltaTime); }

        public void SetTestPower(bool enabled)
        {
            testPowerEnabled = enabled;
            RefreshPower();
        }
        // Hook for the future battery item/provider; this does not simulate capacity or consumption.
        public void SetBatteryPowerAvailable(bool available)
        {
            _batteryPowerAvailable = available;
            RefreshPower();
        }
        private void RefreshPower()
        {
            if (powerOutput != null) powerOutput.SetPowered(HasEnergy);
        }
        public bool TryReceiveRubble(int amount)
        {
            if (amount <= 0 || !isActiveAndEnabled || amount > inputCapacity - rubbleStock) return false;
            rubbleStock += amount;
            return true;
        }
        public void AdvanceProduction(float deltaTime)
        {
            RefreshPower();
            if (!HasEnergy || rubbleStock <= 0 || powderStock >= outputCapacity || deltaTime <= 0f) return;
            _progress += deltaTime;
            float interval = Mathf.Max(0.1f, crushingInterval);
            while (_progress >= interval && rubbleStock > 0 && powderStock < outputCapacity)
            {
                _progress -= interval;
                rubbleStock--;
                powderStock++;
            }
            if (rubbleStock == 0 || powderStock >= outputCapacity) _progress = 0f;
        }
        public int CollectPowder(InventoryFramework.Inventory inventory)
        {
            if (inventory == null || powderItem == null) return 0;
            int collected = 0;
            while (powderStock > 0 && inventory.AddItem(powderItem, 1))
            {
                powderStock--;
                collected++;
            }
            return collected;
        }
        public string GetInteractionPrompt()
        {
            string status = !HasEnergy ? "Sans énergie" : powderStock >= outputCapacity ? "Stock plein" : rubbleStock > 0 ? "Broyage" : "En attente de gravats";
            string energy = testPowerEnabled ? "Énergie de test" : "Batterie";
            return $"Récupérer la poudre ({powderStock}/{outputCapacity}) — {status} [{energy}]";
        }
        public void Interact()
        {
            var manager = CableInteractionManager.Instance;
            if (manager != null && (manager.InteractionsBlocked || manager.IsCarryingCable)) return;
            var inventory = GameManager.Instance != null ? GameManager.Instance.playerInventory : InventoryFramework.Inventory.Instance;
            int collected = CollectPowder(inventory);
            if (collected > 0 && MainMenuController.Instance != null)
                MainMenuController.Instance.ShowNotification($"{collected} poudre de régolithe récupérée(s)");
            // InventoryUI refreshes on opening; no global UI search is required here.
        }
    }
}
