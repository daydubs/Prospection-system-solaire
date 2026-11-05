using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using InventoryFramework;

namespace BuildingSystem
{
    public class BlueprintUIManager : MonoBehaviour
    {
        [Header("Data")]
        [Tooltip("List of BlueprintItems to display. The UI will extract the module info from these.")]
        public List<BlueprintItem> availableBlueprints = new List<BlueprintItem>();

        [Header("UI References")]
        public Transform contentPanel; // The layout group where buttons will be spawned
        public GameObject blueprintButtonPrefab; // Prefab with a BlueprintButtonUI (or Image/Text components)

        private void Start()
        {
            PopulateUI();
        }

        public void PopulateUI()
        {
            if (contentPanel == null)
            {
                Debug.LogError("BlueprintUIManager: 'contentPanel' is not assigned! Blueprints cannot be displayed. Please assign the Content panel (usually a Layout Group inside a Scroll View) to this field in the Inspector.");
                return;
            }

            if (blueprintButtonPrefab == null)
            {
                Debug.LogError("BlueprintUIManager: 'blueprintButtonPrefab' is not assigned! Blueprints cannot be displayed. Please assign a UI Button prefab to this field in the Inspector.");
                return;
            }

            // Clear existing elements in case this is called multiple times
            foreach (Transform child in contentPanel)
            {
                Destroy(child.gameObject);
            }

            foreach (var blueprintItem in availableBlueprints)
            {
                if (blueprintItem == null || blueprintItem.moduleData == null) continue;

                BaseModuleData module = blueprintItem.moduleData;

                GameObject newButton = Instantiate(blueprintButtonPrefab, contentPanel);

                // Set up text
                TMPro.TMP_Text textComponent = newButton.GetComponentInChildren<TMPro.TMP_Text>();
                if (textComponent != null)
                {
                    textComponent.text = module.moduleName;
                }
                else
                {
                    Text legacyTextComponent = newButton.GetComponentInChildren<Text>();
                    if (legacyTextComponent != null)
                    {
                        legacyTextComponent.text = module.moduleName;
                    }
                }

                // Set up icon
                Image[] images = newButton.GetComponentsInChildren<Image>();
                bool iconSet = false;
                foreach(var img in images)
                {
                    if (img.gameObject.name.ToLower().Contains("icon"))
                    {
                        img.sprite = module.icon;
                        iconSet = true;
                        break;
                    }
                }

                if (!iconSet && module.icon != null && images.Length > 0)
                {
                    if (images[0].gameObject != newButton)
                    {
                        images[0].sprite = module.icon;
                    }
                    else if (images.Length > 1)
                    {
                        images[1].sprite = module.icon;
                    }
                    else
                    {
                       images[0].sprite = module.icon;
                    }
                }

                // Set up click action
                Button btn = newButton.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.AddListener(() => OnBlueprintClicked(blueprintItem));
                }
            }
        }

        private void OnBlueprintClicked(BlueprintItem item)
        {
            if (item == null) return;

            HotbarUI hotbarUI = FindAnyObjectByType<HotbarUI>();
            Hotbar hotbar = hotbarUI != null ? hotbarUI.hotbar : FindAnyObjectByType<Hotbar>();

            // Check if player already has this blueprint in belt or inventory
            bool alreadyHasItem = false;
            if (hotbar != null && hotbar.slots != null)
            {
                foreach (var s in hotbar.slots)
                {
                    if (s != null && !s.IsEmpty && (s.item == item || (s.item is BlueprintItem bp && bp.moduleData == item.moduleData)))
                    {
                        alreadyHasItem = true;
                        break;
                    }
                }
            }

            if (!alreadyHasItem && GameManager.Instance != null && GameManager.Instance.HasPlayerItem(item, 1))
            {
                alreadyHasItem = true;
            }

            // Only add item if player does not already have it, preventing inventory filling
            if (!alreadyHasItem)
            {
                bool added = false;
                // Prefer placing onto the belt (hotbar) first
                if (hotbar != null)
                {
                    added = hotbar.AddItem(item, 1);
                }

                // If hotbar full or unavailable, add to inventory
                if (!added && GameManager.Instance != null)
                {
                    added = GameManager.Instance.AddPlayerItem(item, 1);
                }

                if (added)
                {
                    Debug.Log($"[Blueprint] Added {item.itemName} to belt/inventory.");
                }
                else
                {
                    Debug.LogWarning($"[Blueprint] Impossible d'ajouter {item.itemName} (ceinture et inventaire pleins).");
                }
            }

            // If the item exists on the belt, select its slot so the player is holding it
            if (hotbar != null && hotbarUI != null && hotbar.slots != null)
            {
                for (int i = 0; i < hotbar.slots.Count; i++)
                {
                    var s = hotbar.slots[i];
                    if (s != null && !s.IsEmpty && (s.item == item || (s.item is BlueprintItem bp && bp.moduleData == item.moduleData)))
                    {
                        hotbarUI.SelectSlot(i);
                        break;
                    }
                }
                hotbarUI.RefreshUI();
            }

            BuilderController builder = FindAnyObjectByType<BuilderController>();
            if (builder != null)
            {
                if (builder.HasEquippedWelder())
                {
                    builder.EnterBuildMode(item.moduleData);
                    PlayerBaseController player = FindAnyObjectByType<PlayerBaseController>();
                    if (player != null && player.isBlueprintOpen)
                    {
                        if (player.blueprintUI != null) player.blueprintUI.SetActive(false);
                        player.isBlueprintOpen = false;
                        Cursor.lockState = CursorLockMode.Locked;
                        Cursor.visible = false;
                    }
                }
                else
                {
                    builder.NotifyWelderMissing();
                }
            }
        }
    }
}
