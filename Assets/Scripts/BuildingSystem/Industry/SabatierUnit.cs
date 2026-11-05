using UnityEngine;

namespace BuildingSystem.Industry
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider), typeof(FrozenGroundProbe))]
    public class SabatierUnit : MonoBehaviour, IPromptInteractable, IModuleDeconstructionGuard
    {
        private InventoryFramework.Inventory _uiInventory;
        private InventoryFramework.InventoryUI _inventoryUI;
        private InventoryFramework.HotbarUI _hotbarUI;

        public string GetInteractionPrompt()
        {
            if (_groundProbe != null) _groundProbe.RefreshIfDue();
            return $"Récupérer — Sabatier | Water {_waterStock}/{_waterCapacity} · CH4 {_ch4Stock}/{_ch4Capacity} — {ProductionStatus}";
        }

        public string GetDeconstructionBlockReason()
        {
            return _waterStock > 0 || _ch4Stock > 0 ? "Vider le réacteur Sabatier (Water/CH4)" :
                "Déconstruction du Sabatier reportée : restitution de la batterie non prise en charge";
        }

        public bool TryCollect(InventoryFramework.Inventory inventory, InventoryFramework.Hotbar hotbar,
            out int waterCollected, out int ch4Collected, out string reason)
        {
            waterCollected = 0;
            ch4Collected = 0;
            reason = null;
            if (!isActiveAndEnabled || _waterItem == null || _ch4Item == null || _waterItem == _ch4Item)
            { reason = "Réacteur indisponible"; return false; }
            if (_waterStock <= 0 && _ch4Stock <= 0) { reason = "Aucun produit à récupérer"; return false; }
            var lot = new System.Collections.Generic.Dictionary<InventoryFramework.Item, int>();
            bool waterFits = _waterStock > 0, ch4Fits = _ch4Stock > 0;
            while (waterFits || ch4Fits)
            {
                if (waterFits)
                {
                    lot[_waterItem] = waterCollected + 1;
                    if (InventoryFramework.InventoryRefundTransaction.TryApply(lot, inventory, hotbar, false, out reason))
                        waterCollected++;
                    else
                    {
                        waterFits = false;
                        if (waterCollected == 0) lot.Remove(_waterItem); else lot[_waterItem] = waterCollected;
                    }
                    waterFits &= waterCollected < _waterStock;
                }
                if (ch4Fits)
                {
                    lot[_ch4Item] = ch4Collected + 1;
                    if (InventoryFramework.InventoryRefundTransaction.TryApply(lot, inventory, hotbar, false, out reason))
                        ch4Collected++;
                    else
                    {
                        ch4Fits = false;
                        if (ch4Collected == 0) lot.Remove(_ch4Item); else lot[_ch4Item] = ch4Collected;
                    }
                    ch4Fits &= ch4Collected < _ch4Stock;
                }
            }
            if (lot.Count == 0)
            { reason = reason ?? "Inventaire et barre rapide pleins"; return false; }
            if (!InventoryFramework.InventoryRefundTransaction.TryApply(lot, inventory, hotbar, true, out reason))
            { waterCollected = 0; ch4Collected = 0; return false; }
            _waterStock -= waterCollected;
            _ch4Stock -= ch4Collected;
            return true;
        }

        public void Interact()
        {
            var cables = BuildingSystem.Cables.CableInteractionManager.Instance;
            if (!isActiveAndEnabled || (cables != null && (cables.InteractionsBlocked || cables.IsCarryingCable)) ||
                (MainMenuController.Instance != null && MainMenuController.Instance.isMenuOpen) ||
                (CraftingSystem.CraftingUIManager.Instance != null && CraftingSystem.CraftingUIManager.Instance.IsOpen)) return;
            var inventory = GameManager.Instance != null ? GameManager.Instance.playerInventory : InventoryFramework.Inventory.Instance;
            var player = inventory != null ? inventory.GetComponentInParent<PlayerBaseController>() : null;
            if (player != null && (!player.canMove || player.isInventoryOpen || player.isBlueprintOpen)) return;
            bool success = TryCollect(inventory, inventory != null ? inventory.hotbar : null,
                out int water, out int ch4, out string reason);
            if (success)
            {
                CachePlayerViews(inventory);
                if (_inventoryUI != null) _inventoryUI.RefreshUI();
                if (_hotbarUI != null) _hotbarUI.RefreshUI();
            }
            if (MainMenuController.Instance != null)
                MainMenuController.Instance.ShowNotification(success ? $"Water × {water}, CH4 × {ch4} récupérés" : reason);
        }

        private void CachePlayerViews(InventoryFramework.Inventory inventory)
        {
            if (inventory == null || _uiInventory == inventory) return;
            _uiInventory = inventory;
            _inventoryUI = null;
            _hotbarUI = null;
            // The existing views can be on independent Canvas roots; resolve once per player, not per frame.
            var scene = inventory.gameObject.scene;
            if (!scene.IsValid() || !scene.isLoaded) return;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var view in root.GetComponentsInChildren<InventoryFramework.InventoryUI>(true))
                    if (view.inventory == inventory) _inventoryUI = view;
                foreach (var view in root.GetComponentsInChildren<InventoryFramework.HotbarUI>(true))
                    if (view.inventory == inventory || (inventory.hotbar != null && view.hotbar == inventory.hotbar)) _hotbarUI = view;
            }
        }

        [Header("Production")]
        [SerializeField] private FrozenGroundProbe _groundProbe;
        [SerializeField] private InventoryFramework.Item _waterItem;
        [SerializeField] private InventoryFramework.Item _ch4Item;
        [SerializeField, Min(0.1f)] private float _productionInterval = 10f;
        [SerializeField, Min(1)] private int _waterPerCycle = 1;
        [SerializeField, Min(1)] private int _ch4PerCycle = 1;
        [SerializeField, Min(1)] private int _waterCapacity = 10;
        [SerializeField, Min(1)] private int _ch4Capacity = 10;
        [SerializeField] private int _waterStock;
        [SerializeField] private int _ch4Stock;
        private double _progress;
        public int WaterStock => _waterStock;
        public int CH4Stock => _ch4Stock;
        public double ProgressSeconds => _progress;
        private bool ConfigurationValid => _groundProbe != null && _waterItem != null && _ch4Item != null &&
            _waterItem != _ch4Item && !_waterItem.IsBattery && !_ch4Item.IsBattery &&
            !(_waterItem is InventoryFramework.OxygenBottleItem) && !(_ch4Item is InventoryFramework.OxygenBottleItem) &&
            !float.IsNaN(_productionInterval) && !float.IsInfinity(_productionInterval) && _productionInterval >= 0.1f &&
            _waterPerCycle > 0 && _ch4PerCycle > 0 && _waterCapacity >= _waterPerCycle && _ch4Capacity >= _ch4PerCycle &&
            _waterStock >= 0 && _ch4Stock >= 0 && _waterStock <= _waterCapacity && _ch4Stock <= _ch4Capacity;
        private bool HasRoom => _waterCapacity - _waterStock >= _waterPerCycle && _ch4Capacity - _ch4Stock >= _ch4PerCycle;
        public string ProductionStatus
        {
            get
            {
                if (!isActiveAndEnabled) return "Réacteur inactif";
                if (!ConfigurationValid) return "Configuration incomplète";
                if (!_groundProbe.IsFrozen) return _groundProbe.Status;
                return HasRoom ? "Production" : "Stock plein";
            }
        }

        private void Awake()
        {
            if (_groundProbe == null) _groundProbe = GetComponent<FrozenGroundProbe>();
        }
        private void Update() { AdvanceProduction(Time.deltaTime); }

        public void AdvanceProduction(float deltaTime)
        {
            if (!isActiveAndEnabled || !ConfigurationValid || deltaTime <= 0f ||
                float.IsNaN(deltaTime) || float.IsInfinity(deltaTime)) return;
            _groundProbe.RefreshIfDue();
            if (!_groundProbe.IsFrozen || !HasRoom) return;
            int roomCycles = System.Math.Min((_waterCapacity - _waterStock) / _waterPerCycle,
                (_ch4Capacity - _ch4Stock) / _ch4PerCycle);
            double usefulTime = roomCycles * (double)_productionInterval - _progress;
            _progress += System.Math.Min(deltaTime, System.Math.Max(0d, usefulTime));
            int cycles = (int)System.Math.Min(roomCycles, System.Math.Floor((_progress + 0.000001d) / _productionInterval));
            if (cycles <= 0) return;
            _waterStock += cycles * _waterPerCycle;
            _ch4Stock += cycles * _ch4PerCycle;
            _progress = System.Math.Max(0d, _progress - cycles * (double)_productionInterval);
        }
    }
}
