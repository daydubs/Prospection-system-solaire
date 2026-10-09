using UnityEngine;
using UnityEngine.UI;
using TMPro;
using InventoryFramework;

namespace CraftingSystem
{
    public class CraftingIngredientRowUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI amountText;
        [SerializeField] private Image checkmarkOrWarningIcon;

        [Header("Colors")]
        [SerializeField] private Color sufficientColor = new Color(0.2f, 0.85f, 0.4f, 1f);
        [SerializeField] private Color insufficientColor = new Color(0.95f, 0.35f, 0.25f, 1f);

        public void Setup(Item item, int needed, int available, bool hasEnough)
        {
            if (item == null) return;

            if (iconImage != null)
            {
                iconImage.sprite = item.icon;
                iconImage.color = item.icon != null ? Color.white : new Color(1, 1, 1, 0.2f);
            }

            if (nameText != null)
            {
                nameText.text = !string.IsNullOrEmpty(item.itemName) ? item.itemName : item.name;
            }

            if (amountText != null)
            {
                string colorHex = hasEnough ? ColorUtility.ToHtmlStringRGBA(sufficientColor) : ColorUtility.ToHtmlStringRGBA(insufficientColor);
                amountText.text = $"<color=#{colorHex}>{available}</color> / {needed}";
            }

            if (checkmarkOrWarningIcon != null)
            {
                checkmarkOrWarningIcon.color = hasEnough ? sufficientColor : insufficientColor;
            }
        }
    }
}
