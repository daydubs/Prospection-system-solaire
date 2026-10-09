using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.InputSystem;

namespace InventoryFramework
{
    public enum SlotOwner
    {
        Inventory,
        Hotbar,
        Oxygen
    }

    public class InventorySlotUI : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public Image icon;
        public TextMeshProUGUI countText;

        Inventory inventory;
        InventoryUI inventoryUI;

        Hotbar hotbar;
        HotbarUI hotbarUI;

        public int index;
        public SlotOwner owner;

        GameObject dragIcon;
        RectTransform dragRT;
        int fromSlotIndex;

        public ItemTooltip tooltip;

        private InputSystem_Actions inputActions;

        private void Awake()
        {
            inputActions = new InputSystem_Actions();
        }

        private void OnEnable()
        {
            inputActions.Player.Enable();
            inputActions.UI.Enable();
        }

        private void OnDisable()
        {
            inputActions.Player.Disable();
            inputActions.UI.Disable();
        }

        void Update()
        {
            if (tooltip != null && tooltip.gameObject.activeSelf)
            {
                tooltip.UpdatePosition(inputActions.UI.Point.ReadValue<Vector2>());
            }
        }

        public void Setup(Inventory inv, Hotbar hb, int idx, InventoryUI ui)
        {
            inventory = inv;
            hotbar = hb;
            index = idx;
            inventoryUI = ui;
            owner = SlotOwner.Inventory;
            hotbarUI = null;
        }

        public void SetupHotbar(Hotbar hb, Inventory inv, int idx, HotbarUI ui)
        {
            hotbar = hb;
            inventory = inv;
            hotbarUI = ui;
            index = idx;
            owner = SlotOwner.Hotbar;
            inventoryUI = null;
        }

        public void SetupOxygen(Inventory inv, int idx, InventoryUI ui)
        {
            inventory = inv;
            hotbar = null;
            hotbarUI = null;
            index = idx;
            inventoryUI = ui;
            owner = SlotOwner.Oxygen;
        }

        public InventorySlot GetSlot()
        {
            if (owner == SlotOwner.Inventory)
            {
                if (inventory == null || inventory.slots == null || index < 0 || index >= inventory.slots.Count) return null;
                return inventory.slots[index];
            }
            else if (owner == SlotOwner.Hotbar)
            {
                if (hotbar == null || hotbar.slots == null || index < 0 || index >= hotbar.slots.Count) return null;
                return hotbar.slots[index];
            }
            else if (owner == SlotOwner.Oxygen)
            {
                if (inventory == null || inventory.oxygenSlots == null || index < 0 || index >= inventory.oxygenSlots.Count) return null;
                return inventory.oxygenSlots[index];
            }
            return null;
        }

        void RefreshParentUI()
        {
            if (inventoryUI != null) inventoryUI.RefreshUI();
            if (hotbarUI != null) hotbarUI.RefreshUI();
        }

        public void SetSlot(InventorySlot slot)
        {
            if (this == null || icon == null) return;

            if (slot == null || slot.IsEmpty)
            {
                if (owner == SlotOwner.Oxygen)
                {
                    icon.enabled = true;
                    if (inventory != null && inventory.oxygenBottleItem != null && inventory.oxygenBottleItem.icon != null)
                    {
                        icon.sprite = inventory.oxygenBottleItem.icon;
                    }
                    icon.color = new Color(0f, 0.85f, 1f, 0.25f);
                    if (countText != null)
                    {
                        countText.text = "O2";
                        countText.color = new Color(0f, 0.85f, 1f, 0.5f);
                    }
                }
                else
                {
                    icon.enabled = false;
                    if (countText != null) countText.text = "";
                }
            }
            else
            {
                icon.enabled = true;
                icon.sprite = slot.item.icon;
                icon.color = Color.white;

                if (countText != null)
                {
                    if (inventory != null && inventory.IsOxygenBottle(slot.item))
                    {
                        int percent = Mathf.RoundToInt(slot.currentOxygen);
                        countText.text = $"{percent}%";
                        countText.color = percent > 50 ? Color.cyan : (percent > 20 ? Color.yellow : Color.red);
                    }
                    else
                    {
                        countText.text = slot.count > 1 ? slot.count.ToString() : "";
                        countText.color = Color.white;
                    }
                }
            }
        }

        // DRAG HANDLING
        public void OnBeginDrag(PointerEventData eventData)
        {
            var s = GetSlot();
            if (s == null || s.IsEmpty) return;

            // How Many To Drag
            int amount;

            if (eventData.button == PointerEventData.InputButton.Right)
            {
                amount = Mathf.CeilToInt(s.count / 2f);
            }
            else if (inputActions.Player.Sprint.IsPressed())
            {
                amount = 1;
            }
            else
            {
                amount = s.count;
            }

            DragContext.draggedItem = s.item;
            DragContext.draggedCount = amount;
            DragContext.draggedOxygen = s.currentOxygen;
            DragContext.fromSlotIndex = index;
            DragContext.fromOwner = owner;

            s.count -= amount;
            if (s.count <= 0)
            {
                s.item = null;
                s.count = 0;
            }

            Transform parentLayer = (owner == SlotOwner.Hotbar && hotbarUI != null) ? hotbarUI.dragLayer : (inventoryUI != null ? inventoryUI.dragLayer : null);

            dragIcon = new GameObject("DragIcon");
            if (parentLayer != null)
            {
                dragIcon.transform.SetParent(parentLayer, false);
            }

            dragRT = dragIcon.AddComponent<RectTransform>();
            var img = dragIcon.AddComponent<Image>();
            img.sprite = icon.sprite;
            img.raycastTarget = false;
            dragRT.sizeDelta = icon.rectTransform.sizeDelta;

            RefreshAllUIs();

            UpdateDragPosition(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (dragIcon == null) return;

            UpdateDragPosition(eventData);
        }

        void UpdateDragPosition(PointerEventData eventData)
        {
            Vector2 localPoint;
            Canvas canvas = null;
            RectTransform dragLayer = null;

            if ((owner == SlotOwner.Inventory || owner == SlotOwner.Oxygen) && inventoryUI != null)
            {
                canvas = inventoryUI.rootCanvas;
                dragLayer = inventoryUI.dragLayer;
            }
            else if (owner == SlotOwner.Hotbar && hotbarUI != null)
            {
                canvas = hotbarUI.rootCanvas;
                dragLayer = hotbarUI.dragLayer;
            }
            else
            {
                Debug.LogError("No valid UI context for drag!");
                return;
            }

            Camera cam = (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : (canvas != null ? canvas.worldCamera : null);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(dragLayer, eventData.position, cam, out localPoint);
            dragRT.anchoredPosition = localPoint;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (dragIcon != null) Destroy(dragIcon);

            if (DragContext.draggedItem != null && DragContext.draggedCount > 0)
            {
                var originalSlot = GetOriginalSlot();
                if (originalSlot != null)
                {
                    if (originalSlot.IsEmpty)
                    {
                        originalSlot.item = DragContext.draggedItem;
                        originalSlot.count = DragContext.draggedCount;
                        originalSlot.currentOxygen = DragContext.draggedOxygen;
                    }
                    else if (originalSlot.item == DragContext.draggedItem)
                    {
                        originalSlot.count += DragContext.draggedCount;
                    }
                }

                DragContext.draggedItem = null;
                DragContext.draggedCount = 0;
            }

            RefreshAllUIs();
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (DragContext.draggedItem == null || DragContext.draggedCount <= 0) return;

            var targetSlot = GetSlot();
            if (targetSlot == null) return;

            // Restrict Oxygen Slots to ONLY Oxygen Bottles
            if (owner == SlotOwner.Oxygen)
            {
                if (inventory == null || !inventory.IsOxygenBottle(DragContext.draggedItem))
                {
                    ReturnToOriginalSlot();
                    DragContext.draggedItem = null;
                    DragContext.draggedCount = 0;
                    RefreshAllUIs();
                    return;
                }
            }

            // If swapping out of Oxygen slot, target item must also be an oxygen bottle (or empty)
            if (DragContext.fromOwner == SlotOwner.Oxygen && !targetSlot.IsEmpty)
            {
                if (inventory == null || !inventory.IsOxygenBottle(targetSlot.item))
                {
                    ReturnToOriginalSlot();
                    DragContext.draggedItem = null;
                    DragContext.draggedCount = 0;
                    RefreshAllUIs();
                    return;
                }
            }

            // Empty Slot -> Move
            if (targetSlot.IsEmpty)
            {
                targetSlot.item = DragContext.draggedItem;
                targetSlot.count = DragContext.draggedCount;
                targetSlot.currentOxygen = DragContext.draggedOxygen;
            }
            // Same Item -> Merge
            else if (targetSlot.item == DragContext.draggedItem)
            {
                int space = targetSlot.item.maxStack - targetSlot.count;
                int add = Mathf.Min(space, DragContext.draggedCount);
                targetSlot.count += add;
                DragContext.draggedCount -= add;

                if (DragContext.draggedCount > 0)
                {
                    ReturnToOriginalSlot();
                }
            }
            // Different Item -> Normal Swap
            else
            {
                var originalSlot = GetOriginalSlot();

                var tmpItem = targetSlot.item;
                var tmpCount = targetSlot.count;
                var tmpOxygen = targetSlot.currentOxygen;

                targetSlot.item = DragContext.draggedItem;
                targetSlot.count = DragContext.draggedCount;
                targetSlot.currentOxygen = DragContext.draggedOxygen;

                if (originalSlot != null)
                {
                    originalSlot.item = tmpItem;
                    originalSlot.count = tmpCount;
                    originalSlot.currentOxygen = tmpOxygen;
                }
            }

            DragContext.draggedItem = null;
            DragContext.draggedCount = 0;

            RefreshAllUIs();
        }

        private InventorySlot GetOriginalSlot()
        {
            switch (DragContext.fromOwner)
            {
                case SlotOwner.Inventory:
                    if (inventory == null || inventory.slots == null || DragContext.fromSlotIndex < 0 || DragContext.fromSlotIndex >= inventory.slots.Count) return null;
                    return inventory.slots[DragContext.fromSlotIndex];

                case SlotOwner.Hotbar:
                    if (hotbar == null || hotbar.slots == null || DragContext.fromSlotIndex < 0 || DragContext.fromSlotIndex >= hotbar.slots.Count) return null;
                    return hotbar.slots[DragContext.fromSlotIndex];

                case SlotOwner.Oxygen:
                    if (inventory == null || inventory.oxygenSlots == null || DragContext.fromSlotIndex < 0 || DragContext.fromSlotIndex >= inventory.oxygenSlots.Count) return null;
                    return inventory.oxygenSlots[DragContext.fromSlotIndex];

                default:
                    Debug.LogError("Invalid DragContext.fromOwner: " + DragContext.fromOwner);
                    return null;
            }
        }

        private void ReturnToOriginalSlot()
        {
            var originalSlot = GetOriginalSlot();
            if (originalSlot == null) return;

            if (originalSlot.IsEmpty)
            {
                originalSlot.item = DragContext.draggedItem;
                originalSlot.count = DragContext.draggedCount;
                originalSlot.currentOxygen = DragContext.draggedOxygen;
            }
            else if (originalSlot.item == DragContext.draggedItem)
            {
                originalSlot.count += DragContext.draggedCount;
            }
        }

        private void RefreshAllUIs()
        {
            if (inventoryUI != null) inventoryUI.RefreshUI();
            if (hotbarUI != null) hotbarUI.RefreshUI();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            var slot = GetSlot();
            if (slot != null && !slot.IsEmpty && tooltip != null)
            {
                tooltip.Show(slot.item, eventData.position);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (tooltip != null)
            {
                tooltip.Hide();
            }
        }
    }

}

