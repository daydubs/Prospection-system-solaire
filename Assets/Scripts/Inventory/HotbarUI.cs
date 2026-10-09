using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace InventoryFramework
{
    public class HotbarUI : MonoBehaviour
    {
        public Hotbar hotbar;
        public Inventory inventory;
        public Transform slotParent;
        public GameObject slotPrefab;
        public ItemTooltip tooltip;
        public Transform toolsParent;

        private List<InventorySlotUI> slotUIs = new();
        private int selectedIndex = 0;

        public RectTransform dragLayer;
        public Canvas rootCanvas;

        private InputSystem_Actions inputActions;

        private void Awake()
        {
            inputActions = new InputSystem_Actions();
        }

        private void OnEnable()
        {
            inputActions.Player.Enable();
            inputActions.Player.Hotbar.performed += OnHotbarPerformed;
            inputActions.Player.Next.performed += OnNextPerformed;
            inputActions.Player.Previous.performed += OnPreviousPerformed;
        }

        private void OnDisable()
        {
            inputActions.Player.Hotbar.performed -= OnHotbarPerformed;
            inputActions.Player.Next.performed -= OnNextPerformed;
            inputActions.Player.Previous.performed -= OnPreviousPerformed;
            inputActions.Player.Disable();
        }

        void Start()
        {
            foreach (Transform child in slotParent) Destroy(child.gameObject);

            for (int i = 0; i < hotbar.size; i++)
            {
                var go = Instantiate(slotPrefab, slotParent);
                var ui = go.GetComponent<InventorySlotUI>();
                ui.tooltip = tooltip;
                ui.SetupHotbar(hotbar, inventory, i, this);
                slotUIs.Add(ui);
            }

            RefreshUI();
        }

        private void OnHotbarPerformed(InputAction.CallbackContext context)
        {
            // Parse control name to get the digit
            string controlName = context.control.name;
            if (int.TryParse(controlName, out int number))
            {
                // Number is 1-9 (or 0 for 10). Let's adjust to 0-based index.
                int newIndex = -1;
                if (number >= 1 && number <= 9)
                {
                    newIndex = number - 1;
                }
                else if (number == 0) // Key '0' maps to index 9
                {
                    newIndex = 9;
                }

                if (newIndex != -1 && newIndex < hotbar.size)
                {
                    selectedIndex = newIndex;
                    RefreshUI();
                }
            }
        }

        private void OnNextPerformed(InputAction.CallbackContext context)
        {
            selectedIndex = (selectedIndex + 1) % hotbar.size;
            RefreshUI();
        }

        private void OnPreviousPerformed(InputAction.CallbackContext context)
        {
            selectedIndex = (selectedIndex - 1 + hotbar.size) % hotbar.size;
            RefreshUI();
        }

        public void RefreshUI()
        {
            if (slotUIs == null || hotbar == null || hotbar.slots == null) return;

            for (int i = 0; i < hotbar.size; i++)
            {
                if (i >= slotUIs.Count || slotUIs[i] == null) continue;
                if (i < hotbar.slots.Count)
                {
                    slotUIs[i].SetSlot(hotbar.slots[i]);
                }

                var bg = slotUIs[i].transform.GetChild(0).GetComponent<Image>();
                if (bg != null)
                {
                    bg.color = (i == selectedIndex) ? Color.yellow : Color.white;
                }
            }

            if (toolsParent != null)
            {
                for (int x = 0; x < toolsParent.childCount; x++)
                {
                    Destroy(toolsParent.GetChild(x).gameObject);
                }
            }

            InventorySlot slot = null;
            if (selectedIndex >= 0 && selectedIndex < hotbar.slots.Count)
            {
                slot = hotbar.slots[selectedIndex];
            }

            if (slot == null || slot.IsEmpty) return;

            if (slot.item is BuildingSystem.BlueprintItem)
            {
                // If a blueprint is selected, visually show the equipped Welder model in hand so the player holds their welding tool
                Item welder = GetEquippedWelder();
                if (welder != null && welder.model != null && toolsParent != null)
                {
                    Instantiate(welder.model, toolsParent);
                }
                return;
            }

            if (slot.item.model == null || toolsParent == null) return;

            Instantiate(slot.item.model, toolsParent);
        }

        public Item GetEquippedWelder()
        {
            if (hotbar == null || hotbar.slots == null) return null;
            foreach (var s in hotbar.slots)
            {
                if (s != null && !s.IsEmpty && s.item != null)
                {
                    if (string.Equals(s.item.itemName?.Trim(), "Welder", System.StringComparison.OrdinalIgnoreCase) || s.item.id == 5)
                    {
                        return s.item;
                    }
                }
            }
            return null;
        }

        public void SelectSlot(int index)
        {
            if (hotbar != null && index >= 0 && index < hotbar.size)
            {
                selectedIndex = index;
                RefreshUI();
            }
        }

        public int SelectedIndex => selectedIndex;

        public Item GetSelectedItem()
        {
            if (hotbar != null && selectedIndex >= 0 && selectedIndex < hotbar.size)
            {
                var slot = hotbar.GetSlot(selectedIndex);
                if (slot != null && !slot.IsEmpty)
                {
                    return slot.item;
                }
            }

            if (slotUIs != null && selectedIndex >= 0 && selectedIndex < slotUIs.Count && slotUIs[selectedIndex] != null)
            {
                InventorySlot slot = slotUIs[selectedIndex].GetComponent<InventorySlotUI>()?.GetSlot();
                return slot?.item;
            }

            return null;
        }
    }



}
