using System;
using UnityEngine;
using InventoryFramework;

namespace BuildingSystem.Industry
{
    [RequireComponent(typeof(Collider))]
    public sealed class CrusherBatterySocket : MonoBehaviour, IPromptInteractable
    {
        [Header("Connections")]
        [SerializeField] private CrusherUnit _crusher;
        [SerializeField] private Item _batteryItem;
        [SerializeField] private Transform _mount;
        [Header("Installed battery — session state")]
        [SerializeField] private Item _installedItem;
        [SerializeReference] private BatteryInstanceState _installedBattery;
        private GameObject _visual;
        private HotbarUI _hotbarUI;
        private InventoryUI _inventoryUI;
        public event Action Changed;
        public BatteryInstanceState InstalledBattery => _installedBattery;
        public bool HasEnergy => isActiveAndEnabled && _installedBattery != null && _installedBattery.Charge > 0f;
        private void Start()
        {
            _hotbarUI = UnityEngine.Object.FindAnyObjectByType<HotbarUI>();
            _inventoryUI = UnityEngine.Object.FindAnyObjectByType<InventoryUI>();
            RefreshVisual();
        }
        private void OnEnable() { Changed?.Invoke(); }
        private void OnDisable() { Changed?.Invoke(); }
        private Inventory PlayerInventory => GameManager.Instance != null && GameManager.Instance.playerInventory != null ? GameManager.Instance.playerInventory : Inventory.Instance;
        private Hotbar PlayerHotbar => PlayerInventory != null && PlayerInventory.hotbar != null ? PlayerInventory.hotbar : (_hotbarUI != null ? _hotbarUI.hotbar : null);
        private InventorySlot SelectedBattery() => BatteryInventoryTransfer.Find(_batteryItem, PlayerInventory, PlayerHotbar, _hotbarUI != null ? _hotbarUI.SelectedIndex : -1);
        public bool TryInstall(InventorySlot source)
        {
            if (!isActiveAndEnabled || _crusher == null || !_crusher.isActiveAndEnabled || _installedBattery != null || !BatteryInventoryTransfer.Matches(source, _batteryItem)) return false;
            _installedItem = source.item;
            _installedBattery = source.battery;
            source.Clear();
            RefreshVisual();
            Changed?.Invoke();
            return true;
        }
        public bool TryRemove(Inventory inventory, Hotbar hotbar)
        {
            if (!isActiveAndEnabled || _installedBattery == null || !BatteryInventoryTransfer.TryReturn(_installedItem, _installedBattery, inventory != null ? inventory.slots : null, hotbar != null ? hotbar.slots : null)) return false;
            _installedBattery = null;
            _installedItem = null;
            RefreshVisual();
            Changed?.Invoke();
            return true;
        }
        public float ConsumeWork(float seconds, float rate)
        {
            if (!HasEnergy || _crusher == null || !_crusher.isActiveAndEnabled) return 0f;
            float paid = _installedBattery.ConsumeWork(seconds, rate);
            if (_installedBattery.Charge <= 0f) Changed?.Invoke();
            return paid;
        }
        public string GetInteractionPrompt()
        {
            if (_installedBattery != null) return $"Retirer Batterie externe — {_installedBattery.Percent}%";
            var source = SelectedBattery();
            return source != null ? $"Installer Batterie externe — {source.battery.Percent}%" : "Batterie externe requise";
        }
        public void Interact()
        {
            var cables = BuildingSystem.Cables.CableInteractionManager.Instance;
            if ((cables != null && (cables.InteractionsBlocked || cables.IsCarryingCable)) ||
                (MainMenuController.Instance != null && MainMenuController.Instance.isMenuOpen) ||
                (CraftingSystem.CraftingUIManager.Instance != null && CraftingSystem.CraftingUIManager.Instance.IsOpen)) return;
            var inventory = PlayerInventory;
            if (inventory != null)
            {
                var player = inventory.GetComponent<PlayerBaseController>();
                var builder = inventory.GetComponent<BuilderController>();
                if ((player != null && (player.isInventoryOpen || player.isBlueprintOpen)) || (builder != null && builder.isBuildModeActive)) return;
            }
            bool removing = _installedBattery != null;
            bool success = removing ? TryRemove(inventory, PlayerHotbar) : TryInstall(SelectedBattery());
            string message = success ? (removing ? "Batterie récupérée — charge conservée" : "Batterie installée") : (removing ? "Inventaire et barre rapide pleins — batterie laissée en place" : "Aucune batterie disponible");
            if (MainMenuController.Instance != null) MainMenuController.Instance.ShowNotification(message);
            if (success)
            {
                if (_inventoryUI != null) _inventoryUI.RefreshUI();
                if (_hotbarUI != null) _hotbarUI.RefreshUI();
            }
        }
        private void RefreshVisual()
        {
            if (_visual != null) { _visual.SetActive(false); Destroy(_visual); _visual = null; }
            if (_installedBattery == null || _installedItem == null || _installedItem.model == null || _mount == null) return;
            _visual = Instantiate(_installedItem.model, _mount, false);
            _visual.transform.localPosition = Vector3.zero;
            _visual.transform.localRotation = _installedItem.model.transform.localRotation;
            // Existing battery model is mesh-only. Leave interaction exclusively on the socket collider.
            foreach (var collider in _visual.GetComponentsInChildren<Collider>()) collider.enabled = false;
        }
    }
}
