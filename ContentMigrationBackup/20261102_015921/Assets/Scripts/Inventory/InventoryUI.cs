using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace InventoryFramework
{
    public class InventoryUI : MonoBehaviour
    {
        public Inventory inventory;
        public Hotbar hotbar;
        public Transform slotParent;
        public GameObject slotPrefab;
        public ItemTooltip tooltip;

        public RectTransform dragLayer;
        public Canvas rootCanvas;

        [Header("Oxygen Section")]
        public Transform oxygenSlotParent;
        public Button refillOxygenButton;
        public TextMeshProUGUI oxygenStatusText;

        private List<InventorySlotUI> slotUIs;
        private List<InventorySlotUI> oxygenSlotUIs;
        private float lastOxygenRefreshTime;

        void Start()
        {
            if (inventory == null) inventory = GetComponent<Inventory>() ?? FindAnyObjectByType<Inventory>();
            if (hotbar == null) hotbar = GetComponent<Hotbar>() ?? FindAnyObjectByType<Hotbar>();

            // Setup main inventory slots
            slotUIs = new List<InventorySlotUI>();
            if (slotParent != null)
            {
                foreach (Transform child in slotParent) Destroy(child.gameObject);

                int mainSize = inventory != null ? inventory.size : 36;
                for (int i = 0; i < mainSize; i++)
                {
                    var slotGO = Instantiate(slotPrefab, slotParent);
                    var slotUI = slotGO.GetComponent<InventorySlotUI>();
                    slotUI.tooltip = tooltip;
                    slotUI.Setup(inventory, hotbar, i, this);
                    slotUIs.Add(slotUI);
                }
            }

            // Setup oxygen slots (4 dedicated slots)
            oxygenSlotUIs = new List<InventorySlotUI>();
            if (oxygenSlotParent != null)
            {
                foreach (Transform child in oxygenSlotParent) Destroy(child.gameObject);

                int o2Size = inventory != null ? inventory.oxygenSlotCount : 4;
                for (int i = 0; i < o2Size; i++)
                {
                    var slotGO = Instantiate(slotPrefab, oxygenSlotParent);
                    var slotUI = slotGO.GetComponent<InventorySlotUI>();
                    slotUI.tooltip = tooltip;
                    slotUI.SetupOxygen(inventory, i, this);
                    oxygenSlotUIs.Add(slotUI);
                }
            }

            // Wire refill button
            if (refillOxygenButton != null)
            {
                refillOxygenButton.onClick.RemoveAllListeners();
                refillOxygenButton.onClick.AddListener(OnRefillOxygenClicked);
            }

            RefreshUI();
        }

        void Update()
        {
            // When inventory is visible, update oxygen slots smoothly as oxygen is consumed
            if (Time.time - lastOxygenRefreshTime > 0.25f)
            {
                lastOxygenRefreshTime = Time.time;
                RefreshOxygenSlotsOnly();
            }
        }

        public void RefreshUI()
        {
            if (inventory == null) return;

            // Refresh main slots
            if (slotUIs != null && inventory.slots != null)
            {
                for (int i = 0; i < slotUIs.Count && i < inventory.slots.Count; i++)
                {
                    if (slotUIs[i] == null) continue;
                    slotUIs[i].SetSlot(inventory.slots[i]);
                }
            }

            // Refresh oxygen slots
            RefreshOxygenSlotsOnly();

            // Refresh ice status text
            UpdateStatusText();
        }

        private void RefreshOxygenSlotsOnly()
        {
            if (inventory == null || inventory.oxygenSlots == null || oxygenSlotUIs == null) return;

            for (int i = 0; i < oxygenSlotUIs.Count && i < inventory.oxygenSlots.Count; i++)
            {
                if (oxygenSlotUIs[i] == null) continue;
                oxygenSlotUIs[i].SetSlot(inventory.oxygenSlots[i]);
            }
        }

        private void UpdateStatusText()
        {
            if (oxygenStatusText == null || inventory == null) return;

            Item ice = inventory.iceResourceItem;
            #if UNITY_EDITOR
            if (ice == null)
            {
                ice = UnityEditor.AssetDatabase.LoadAssetAtPath<Item>("Assets/Items/IceResource.asset");
            }
            #endif

            int iceCount = inventory.GetItemCount(ice);
            if (hotbar != null) iceCount += hotbar.GetItemCount(ice);

            oxygenStatusText.text = $"Glace disponible : <color=#00E5FF>{iceCount}</color> (1 = 1 Recharge O2)";
        }

        public void OnRefillOxygenClicked()
        {
            if (inventory == null) return;

            inventory.RefillOxygenBottleWithIce();
            RefreshUI();
            if (hotbar != null)
            {
                var hbUI = FindAnyObjectByType<HotbarUI>();
                if (hbUI != null) hbUI.RefreshUI();
            }
        }
    }
}

