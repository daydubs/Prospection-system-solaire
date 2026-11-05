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
        [Header("Battery power")]
        [SerializeField] private CrusherBatterySocket batterySocket;
        [SerializeField, Min(0.001f)] private float chargePerSecond = 0.1f;
        public CrusherBatterySocket BatterySocket => batterySocket;
        [Header("Production")]
        [SerializeField, Min(0.1f)] private float crushingInterval = 5f;
        [SerializeField, Min(1)] private int inputCapacity = 100;
        [SerializeField, Min(1)] private int outputCapacity = 100;
        [SerializeField] private int rubbleStock;
        [SerializeField] private int powderStock;
        private float _progress;
        public bool HasEnergy => isActiveAndEnabled && batterySocket != null && batterySocket.HasEnergy;
        public int RubbleStock => rubbleStock;
        public int PowderStock => powderStock;
        public int RemainingInputCapacity => Mathf.Max(0, inputCapacity - rubbleStock);
        public bool CanAcceptRubble => isActiveAndEnabled && rubbleStock < inputCapacity;
        public ElectricConnectorSocket PowerOutput => powerOutput;
        public MaterialConnectorSocket RubbleInput => rubbleInput;

        private void OnEnable()
        {
            if (batterySocket != null) batterySocket.Changed += RefreshPower;
            RefreshPower();
        }
        private void OnDisable()
        {
            if (batterySocket != null) batterySocket.Changed -= RefreshPower;
            if (powerOutput != null) powerOutput.SetPowered(false);
        }
        private void Update() { AdvanceProduction(Time.deltaTime); }

        public float ConsumeWork(float seconds, float rate)
        {
            if (!HasEnergy) return 0f;
            float paid = batterySocket.ConsumeWork(seconds, rate);
            RefreshPower();
            return paid;
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
            float interval = Mathf.Max(0.1f, crushingInterval);
            float usefulTime = Mathf.Max(0f, Mathf.Min(rubbleStock, outputCapacity - powderStock) * interval - _progress);
            _progress += ConsumeWork(Mathf.Min(deltaTime, usefulTime), Mathf.Max(0.001f, chargePerSecond));
            while (_progress + 0.000001f >= interval && rubbleStock > 0 && powderStock < outputCapacity)
            {
                _progress = Mathf.Max(0f, _progress - interval);
                rubbleStock--;
                powderStock++;
            }
            if (rubbleStock == 0 || powderStock >= outputCapacity) _progress = 0f;
        }
        public int CollectPowder(InventoryFramework.Inventory inventory)
        {
            if (inventory == null || powderItem == null || powderItem.Count == 0)
                return 0;

            int collected = 0;

            while (powderStock > 0)
            {
                InventoryFramework.Item selectedItem = GetRandomPowderItem();

                if (selectedItem == null || !inventory.AddItem(selectedItem, 1))
                    break;

                powderStock--;
                collected++;
            }

            return collected;
        }

        private InventoryFramework.Item GetRandomPowderItem()
        {
            if (powderItem == null || powderItem.Count == 0)
                return null;

            // Évite de sélectionner un emplacement vide dans l'Inspector.
            List<InventoryFramework.Item> validItems = new List<InventoryFramework.Item>();

            foreach (InventoryFramework.Item item in powderItem)
            {
                if (item != null)
                    validItems.Add(item);
            }

            if (validItems.Count == 0)
                return null;

            return validItems[Random.Range(0, validItems.Count)];
        }
        public string GetInteractionPrompt()
        {
            string status = !HasEnergy ? "Sans énergie" : powderStock >= outputCapacity ? "Stock plein" : rubbleStock > 0 ? "Broyage" : "En attente de gravats";
            string energy = batterySocket == null || batterySocket.InstalledBattery == null ? "Sans batterie" : $"Batterie {batterySocket.InstalledBattery.Percent}%";
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
