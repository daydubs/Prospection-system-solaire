using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using InventoryFramework;

namespace CraftingSystem
{
    public class CraftingUIManager : MonoBehaviour
    {
        public static CraftingUIManager Instance { get; private set; }

        [Header("Panels & Canvas")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private Canvas rootCanvas;

        [Header("Header")]
        [SerializeField] private TextMeshProUGUI stationTitleText;
        [SerializeField] private TextMeshProUGUI stationSubtitleText;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button returnToShipButton;

        [Header("Recipe List (Left Panel)")]
        [SerializeField] private Transform recipeListContent;
        [SerializeField] private GameObject recipeListItemPrefab;
        [SerializeField] private TMP_InputField searchInputField;

        [Header("Details View (Right Panel)")]
        [SerializeField] private GameObject detailsContainer;
        [SerializeField] private Image selectedItemIcon;
        [SerializeField] private TextMeshProUGUI selectedItemNameText;
        [SerializeField] private TextMeshProUGUI selectedItemCategoryText;
        [SerializeField] private TextMeshProUGUI selectedItemDescriptionText;
        [SerializeField] private Transform ingredientsListContent;
        [SerializeField] private GameObject ingredientRowPrefab;

        [Header("Crafting Controls")]
        [SerializeField] private Button decreaseQtyButton;
        [SerializeField] private Button increaseQtyButton;
        [SerializeField] private TextMeshProUGUI craftQtyText;
        [SerializeField] private Button craftButton;
        [SerializeField] private TextMeshProUGUI craftButtonText;
        [SerializeField] private TextMeshProUGUI feedbackStatusText;

        [Header("Registered Recipes")]
        public List<CraftingRecipe> globalRecipes = new List<CraftingRecipe>();

        private LanderCraftingStation currentStation;
        private List<CraftingRecipe> activeRecipeList = new List<CraftingRecipe>();
        private CraftingRecipe selectedRecipe;
        private int currentCraftQuantity = 1;
        private bool isOpen = false;

        public bool IsOpen => isOpen;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            if (mainPanel != null)
            {
                mainPanel.SetActive(false);
            }

            // Wire buttons
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(CloseUI);
            }

            if (returnToShipButton != null)
            {
                returnToShipButton.onClick.RemoveAllListeners();
                returnToShipButton.onClick.AddListener(OnReturnToShipClicked);
            }

            if (craftButton != null)
            {
                craftButton.onClick.RemoveAllListeners();
                craftButton.onClick.AddListener(CraftSelected);
            }

            if (decreaseQtyButton != null)
            {
                decreaseQtyButton.onClick.RemoveAllListeners();
                decreaseQtyButton.onClick.AddListener(() => ChangeQuantity(-1));
            }

            if (increaseQtyButton != null)
            {
                increaseQtyButton.onClick.RemoveAllListeners();
                increaseQtyButton.onClick.AddListener(() => ChangeQuantity(1));
            }

            if (searchInputField != null)
            {
                searchInputField.onValueChanged.RemoveAllListeners();
                searchInputField.onValueChanged.AddListener((s) => PopulateRecipeList());
            }

            LoadAvailableRecipes();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            if (!isOpen) return;

            // Close on Escape or E or I
            if (Keyboard.current != null)
            {
                if (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.eKey.wasPressedThisFrame)
                {
                    CloseUI();
                }
            }
        }

        public void LoadAvailableRecipes()
        {
            #if UNITY_EDITOR
            if (globalRecipes == null || globalRecipes.Count == 0)
            {
                globalRecipes = new List<CraftingRecipe>();
                string[] guids = UnityEditor.AssetDatabase.FindAssets("t:CraftingRecipe");
                foreach (string guid in guids)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                    var recipe = UnityEditor.AssetDatabase.LoadAssetAtPath<CraftingRecipe>(path);
                    if (recipe != null && !globalRecipes.Contains(recipe))
                    {
                        globalRecipes.Add(recipe);
                    }
                }
            }
            #endif
        }

        public void OpenStation(LanderCraftingStation station)
        {
            currentStation = station;
            isOpen = true;

            if (mainPanel != null)
            {
                Debug.Log("mainPanel active: " + mainPanel.activeSelf);
                if (!mainPanel.activeSelf)
                {
                    mainPanel.SetActive(true);
                }
                //mainPanel.SetActive(true);
                Debug.Log("[CraftingUIManager] Opened Crafting UI for station: " + (station != null ? station.StationName : "null"));
                Debug.Log("mainPanel active: " + mainPanel.activeSelf);
            }

            // Lock/Unlock cursor
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Disable player movement while UI is open
            var player = Object.FindAnyObjectByType<PlayerBaseController>();
            if (player != null)
            {
                player.canMove = false;
            }

            // Update Header
            if (stationTitleText != null)
            {
                stationTitleText.text = station != null ? station.StationName : "IMPRIMANTE 3D - LANDER";
            }
            if (stationSubtitleText != null)
            {
                stationSubtitleText.text = "Fabrication de composants et pièces de structure";
            }

            // Determine recipes to display
            activeRecipeList.Clear();
            if (station != null && station.stationRecipes != null && station.stationRecipes.Count > 0)
            {
                activeRecipeList.AddRange(station.stationRecipes);
            }
            else
            {
                activeRecipeList.AddRange(globalRecipes);
            }

            // Default selection
            currentCraftQuantity = 1;
            if (activeRecipeList.Count > 0)
            {
                selectedRecipe = activeRecipeList[0];
            }
            else
            {
                selectedRecipe = null;
            }

            PopulateRecipeList();
            RefreshDetails();
        }

        public void CloseUI()
        {
            isOpen = false;
            if (mainPanel != null)
            {
                mainPanel.SetActive(false);
            }

            // Restore player controls and cursor
            var player = Object.FindAnyObjectByType<PlayerBaseController>();
            if (player != null)
            {
                player.canMove = true;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void OnReturnToShipClicked()
        {
            if (currentStation != null)
            {
                currentStation.ReturnToSpaceship();
            }
            else
            {
                CloseUI();
                SolarSystemManager.initialDestinationName = "Lune";
                if (SceneTransitionManager.Instance != null)
                {
                    SceneTransitionManager.Instance.LoadSolarSystem();
                }
                else
                {
                    UnityEngine.SceneManagement.SceneManager.LoadScene(SceneTransitionManager.SCENE_SOLAR_SYSTEM);
                }
            }
        }

        public void SelectRecipe(CraftingRecipe recipe)
        {
            selectedRecipe = recipe;
            currentCraftQuantity = 1;
            RefreshDetails();
            PopulateRecipeList(); // Refresh highlighting
        }

        private void ChangeQuantity(int delta)
        {
            currentCraftQuantity = Mathf.Clamp(currentCraftQuantity + delta, 1, 99);
            RefreshDetails();
        }

        public void PopulateRecipeList()
        {
            if (recipeListContent == null || recipeListItemPrefab == null) return;

            // Clear old items
            foreach (Transform child in recipeListContent)
            {
                Destroy(child.gameObject);
            }

            string filter = searchInputField != null ? searchInputField.text.Trim().ToLowerInvariant() : "";

            foreach (var recipe in activeRecipeList)
            {
                if (recipe == null) continue;

                string displayName = recipe.GetDisplayName();
                if (!string.IsNullOrEmpty(filter) && !displayName.ToLowerInvariant().Contains(filter))
                {
                    continue;
                }

                GameObject itemGO = Instantiate(recipeListItemPrefab, recipeListContent);
                var rowUI = itemGO.GetComponent<CraftingRecipeRowUI>();
                if (rowUI != null)
                {
                    bool isSelected = (recipe == selectedRecipe);
                    bool canCraftAtLeastOne = CanCraftRecipe(recipe, 1);
                    rowUI.Setup(recipe, isSelected, canCraftAtLeastOne, () => SelectRecipe(recipe));
                }
                else
                {
                    // Fallback generic setup
                    var btn = itemGO.GetComponent<Button>();
                    if (btn != null) btn.onClick.AddListener(() => SelectRecipe(recipe));
                    var txt = itemGO.GetComponentInChildren<TextMeshProUGUI>();
                    if (txt != null) txt.text = displayName;
                }
            }
        }

        public void RefreshDetails()
        {
            if (selectedRecipe == null)
            {
                if (detailsContainer != null) detailsContainer.SetActive(false);
                return;
            }

            if (detailsContainer != null) detailsContainer.SetActive(true);

            // Icon & texts
            if (selectedItemIcon != null)
            {
                Sprite icon = selectedRecipe.GetIcon();
                selectedItemIcon.sprite = icon;
                selectedItemIcon.color = icon != null ? Color.white : new Color(1, 1, 1, 0.2f);
            }

            if (selectedItemNameText != null)
            {
                string qtySuffix = selectedRecipe.outputAmount > 1 ? $" (x{selectedRecipe.outputAmount})" : "";
                selectedItemNameText.text = selectedRecipe.GetDisplayName() + qtySuffix;
            }

            if (selectedItemCategoryText != null)
            {
                selectedItemCategoryText.text = $"Catégorie : {selectedRecipe.category}";
            }

            if (selectedItemDescriptionText != null)
            {
                selectedItemDescriptionText.text = selectedRecipe.GetDescription();
            }

            if (craftQtyText != null)
            {
                craftQtyText.text = currentCraftQuantity.ToString();
            }

            // Populate ingredients
            if (ingredientsListContent != null && ingredientRowPrefab != null)
            {
                foreach (Transform child in ingredientsListContent)
                {
                    Destroy(child.gameObject);
                }

                foreach (var ing in selectedRecipe.ingredients)
                {
                    if (ing == null || ing.item == null) continue;

                    int needed = ing.amount * currentCraftQuantity;
                    int available = GetPlayerItemTotalCount(ing.item);
                    bool hasEnough = available >= needed;

                    GameObject rowGO = Instantiate(ingredientRowPrefab, ingredientsListContent);
                    var ingUI = rowGO.GetComponent<CraftingIngredientRowUI>();
                    if (ingUI != null)
                    {
                        ingUI.Setup(ing.item, needed, available, hasEnough);
                    }
                    else
                    {
                        var txt = rowGO.GetComponentInChildren<TextMeshProUGUI>();
                        if (txt != null)
                        {
                            string colorTag = hasEnough ? "<color=#4cd137>" : "<color=#e84118>";
                            txt.text = $"{ing.item.itemName} : {colorTag}{available}</color> / {needed}";
                        }
                    }
                }
            }

            // Check craftability
            bool canCraft = CanCraftRecipe(selectedRecipe, currentCraftQuantity);
            if (craftButton != null)
            {
                craftButton.interactable = canCraft;
            }

            if (craftButtonText != null)
            {
                int totalProduced = selectedRecipe.outputAmount * currentCraftQuantity;
                craftButtonText.text = $"FABRIQUER ({totalProduced})";
            }

            if (feedbackStatusText != null)
            {
                if (canCraft)
                {
                    feedbackStatusText.text = "<color=#4cd137>Matériaux prêts pour impression 3D</color>";
                }
                else
                {
                    feedbackStatusText.text = "<color=#e84118>Matériaux insuffisants dans l'inventaire</color>";
                }
            }
        }

        public bool CanCraftRecipe(CraftingRecipe recipe, int quantity)
        {
            if (recipe == null || recipe.ingredients == null) return false;

            foreach (var ing in recipe.ingredients)
            {
                if (ing == null || ing.item == null) continue;
                int needed = ing.amount * quantity;
                if (GetPlayerItemTotalCount(ing.item) < needed)
                {
                    return false;
                }
            }

            return true;
        }

        public void CraftSelected()
        {
            if (selectedRecipe == null) return;
            if (!CanCraftRecipe(selectedRecipe, currentCraftQuantity))
            {
                Debug.LogWarning("[CraftingUIManager] Pas assez de ressources pour fabriquer cet item.");
                return;
            }

            // Consume ingredients
            foreach (var ing in selectedRecipe.ingredients)
            {
                if (ing == null || ing.item == null) continue;
                int needed = ing.amount * currentCraftQuantity;
                ConsumePlayerItem(ing.item, needed);
            }

            // Produce output item
            int totalProduced = selectedRecipe.outputAmount * currentCraftQuantity;
            bool added = AddPlayerItem(selectedRecipe.outputItem, totalProduced);

            if (!added)
            {
                string warnMsg = "Inventaire plein ! Impossible de récupérer l'objet fabriqué.";
                Debug.LogWarning($"[CraftingUIManager] {warnMsg}");
                if (MainMenuController.Instance != null)
                {
                    MainMenuController.Instance.ShowNotification(warnMsg);
                }
                RefreshDetails();
                PopulateRecipeList();
                return;
            }

            // Play sound / particle effect on station
            if (currentStation != null)
            {
                currentStation.PlayCraftEffect();
            }

            // Notification
            string msg = $"Imprimé : +{totalProduced} {selectedRecipe.GetDisplayName()} !";
            Debug.Log($"[CraftingUIManager] {msg}");
            if (MainMenuController.Instance != null)
            {
                MainMenuController.Instance.ShowNotification(msg);
            }

            // Refresh UI state
            RefreshDetails();
            PopulateRecipeList();
        }

        public int GetPlayerItemTotalCount(Item item)
        {
            if (item == null) return 0;
            int total = 0;

            if (Inventory.Instance != null && Inventory.Instance.slots != null)
            {
                foreach (var s in Inventory.Instance.slots)
                {
                    if (s != null && !s.IsEmpty && (s.item == item || (item.id != 0 && s.item.id == item.id)))
                    {
                        total += s.count;
                    }
                }
            }

            var hotbar = Object.FindAnyObjectByType<Hotbar>();
            if (hotbar != null && hotbar.slots != null)
            {
                foreach (var s in hotbar.slots)
                {
                    if (s != null && !s.IsEmpty && (s.item == item || (item.id != 0 && s.item.id == item.id)))
                    {
                        total += s.count;
                    }
                }
            }

            return total;
        }

        public bool ConsumePlayerItem(Item item, int amount)
        {
            if (GameManager.Instance != null && GameManager.Instance.ConsumePlayerItem(item, amount))
            {
                return true;
            }

            if (Inventory.Instance != null && Inventory.Instance.ConsumeItem(item, amount))
            {
                var invUI = Object.FindAnyObjectByType<InventoryUI>();
                if (invUI != null) invUI.RefreshUI();
                var hotbarUI = Object.FindAnyObjectByType<HotbarUI>();
                if (hotbarUI != null) hotbarUI.RefreshUI();
                return true;
            }

            return false;
        }

        public bool AddPlayerItem(Item item, int amount)
        {
            if (GameManager.Instance != null && GameManager.Instance.AddPlayerItem(item, amount))
            {
                return true;
            }

            if (Inventory.Instance != null)
            {
                bool added = Inventory.Instance.AddItem(item, amount);
                if (!added)
                {
                    var hotbar = Object.FindAnyObjectByType<Hotbar>();
                    if (hotbar != null)
                    {
                        added = hotbar.AddItem(item, amount);
                    }
                }

                if (added)
                {
                    var invUI = Object.FindAnyObjectByType<InventoryUI>();
                    if (invUI != null) invUI.RefreshUI();
                    var hotbarUI = Object.FindAnyObjectByType<HotbarUI>();
                    if (hotbarUI != null) hotbarUI.RefreshUI();
                    return true;
                }
            }

            return false;
        }
    }
}
