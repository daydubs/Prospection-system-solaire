using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BuildingSystem.Electricity
{
    /// <summary>
    /// Élément de liste représentant un circuit électrique dans le terminal de gestion.
    /// Affiche le nom, la consommation, le statut et fournit un bouton interactif ON/OFF.
    /// </summary>
    public class CircuitRowItemUI : MonoBehaviour
    {
        [Header("Composants UI")]
        [SerializeField] private Image statusLed;
        [SerializeField] private TextMeshProUGUI circuitNameText;
        [SerializeField] private TextMeshProUGUI consumptionText;
        [SerializeField] private TextMeshProUGUI categoryBadgeText;
        [SerializeField] private Button toggleButton;
        [SerializeField] private TextMeshProUGUI toggleButtonText;
        [SerializeField] private Image toggleButtonBg;

        [Header("Couleurs")]
        [SerializeField] private Color colorActive = new Color(0f, 0.9f, 0.45f, 1f);
        [SerializeField] private Color colorInactive = new Color(0.35f, 0.4f, 0.45f, 1f);
        [SerializeField] private Color colorCut = new Color(1f, 0.25f, 0.25f, 1f);
        [SerializeField] private Color colorBtnOn = new Color(0.05f, 0.5f, 0.25f, 1f);
        [SerializeField] private Color colorBtnOff = new Color(0.4f, 0.15f, 0.15f, 1f);

        private int circuitIndex;
        private Action onToggleAction;

        public void Setup(int index, BaseElectricityController controller, Action onToggle)
        {
            circuitIndex = index;
            onToggleAction = onToggle;

            if (toggleButton != null)
            {
                toggleButton.onClick.RemoveAllListeners();
                toggleButton.onClick.AddListener(() => onToggleAction?.Invoke());
            }

            Refresh(controller);
        }

        public void Refresh(BaseElectricityController controller)
        {
            if (controller == null || circuitIndex < 0 || circuitIndex >= controller.Circuits.Count) return;

            BaseCircuitData data = controller.Circuits[circuitIndex];
            bool gridPowered = controller.IsMainBreakerOn && (controller.TotalProductionKW > 0f || controller.CurrentBatteryKWh > 0.1f);
            float currentCons = data.GetCurrentConsumption(controller.CurrentMode);

            if (circuitNameText != null)
            {
                circuitNameText.text = data.circuitName;
            }

            if (consumptionText != null)
            {
                consumptionText.text = $"{currentCons:F1} kW";
                consumptionText.color = data.isActive ? Color.white : colorInactive;
            }

            if (categoryBadgeText != null)
            {
                categoryBadgeText.text = data.isEssential ? "CRITIQUE" : "STANDARD";
                categoryBadgeText.color = data.isEssential ? new Color(1f, 0.8f, 0.2f, 1f) : new Color(0.6f, 0.8f, 1f, 1f);
            }

            bool effectivelyPowered = gridPowered && data.isActive && currentCons > 0f;

            if (statusLed != null)
            {
                if (!data.isActive)
                {
                    statusLed.color = colorInactive;
                }
                else if (!gridPowered)
                {
                    statusLed.color = colorCut;
                }
                else
                {
                    statusLed.color = colorActive;
                }
            }

            if (toggleButtonText != null)
            {
                toggleButtonText.text = data.isActive ? "ACTIF [ON]" : "COUPÉ [OFF]";
            }

            if (toggleButtonBg != null)
            {
                toggleButtonBg.color = data.isActive ? colorBtnOn : colorBtnOff;
            }
        }
    }
}
