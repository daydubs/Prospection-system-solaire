using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace CraftingSystem
{
    public class CraftingRecipeRowUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Button button;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI categoryText;
        [SerializeField] private Image statusBadge;
        [SerializeField] private TextMeshProUGUI statusBadgeText;

        [Header("Colors")]
        [SerializeField] private Color normalColor = new Color(0.12f, 0.16f, 0.22f, 0.85f);
        [SerializeField] private Color selectedColor = new Color(0.15f, 0.35f, 0.55f, 1f);
        [SerializeField] private Color readyBadgeColor = new Color(0.18f, 0.8f, 0.44f, 0.9f);
        [SerializeField] private Color missingBadgeColor = new Color(0.9f, 0.3f, 0.2f, 0.8f);

        public void Setup(CraftingRecipe recipe, bool isSelected, bool canCraft, Action onClick)
        {
            if (recipe == null) return;

            if (titleText != null)
            {
                string qtySuffix = recipe.outputAmount > 1 ? $" x{recipe.outputAmount}" : "";
                titleText.text = recipe.GetDisplayName() + qtySuffix;
            }

            if (categoryText != null)
            {
                categoryText.text = recipe.category;
            }

            if (iconImage != null)
            {
                Sprite icon = recipe.GetIcon();
                iconImage.sprite = icon;
                iconImage.color = icon != null ? Color.white : new Color(1, 1, 1, 0.2f);
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = isSelected ? selectedColor : normalColor;
            }

            if (statusBadge != null)
            {
                statusBadge.color = canCraft ? readyBadgeColor : missingBadgeColor;
            }

            if (statusBadgeText != null)
            {
                statusBadgeText.text = canCraft ? "PRÊT" : "MANQUE";
                statusBadgeText.color = Color.white;
            }

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                if (onClick != null)
                {
                    button.onClick.AddListener(() => onClick());
                }
            }
        }
    }
}
