using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BuildingSystem.Electricity
{
    /// <summary>
    /// Gère l'affichage en temps réel sur l'écran WorldSpace du contrôleur de sous-système.
    /// Présente les jauges, voyants et métriques énergétiques directement intégrés dans l'environnement 3D.
    /// </summary>
    public class BaseElectricityWorldScreenUI : MonoBehaviour
    {
        [Header("En-tête & Statut Réseau")]
        [SerializeField] private TextMeshProUGUI headerTitleText;
        [SerializeField] private TextMeshProUGUI gridStatusBadgeText;
        [SerializeField] private Image gridStatusBadgeBg;

        [Header("Production")]
        [SerializeField] private TextMeshProUGUI productionValueText;
        [SerializeField] private Image productionFillBar;

        [Header("Consommation")]
        [SerializeField] private TextMeshProUGUI consumptionValueText;
        [SerializeField] private Image consumptionFillBar;

        [Header("Accumulateur & Batterie")]
        [SerializeField] private TextMeshProUGUI batteryPercentText;
        [SerializeField] private TextMeshProUGUI batteryCapacityText;
        [SerializeField] private Image batteryFillBar;

        [Header("Bilan Net & Mode")]
        [SerializeField] private TextMeshProUGUI netPowerText;
        [SerializeField] private TextMeshProUGUI modeBadgeText;

        [Header("Voyants des Prises")]
        [SerializeField] private Image[] inputSocketLeds;
        [SerializeField] private Image outputSocketLed;

        [Header("Palette Couleurs")]
        [SerializeField] private Color colorStable = new Color(0f, 0.9f, 0.45f, 1f);       // Vert émeraude
        [SerializeField] private Color colorDischarging = new Color(1f, 0.72f, 0.1f, 1f);   // Ambre
        [SerializeField] private Color colorCritical = new Color(1f, 0.25f, 0.25f, 1f);      // Rouge alerte
        [SerializeField] private Color colorOffline = new Color(0.4f, 0.45f, 0.5f, 1f);       // Gris
        [SerializeField] private Color colorSurplus = new Color(0f, 0.85f, 1f, 1f);         // Cyan

        private const float MaxScaleKW = 25f; // Échelle max pour les jauges visuelles

        public void RefreshDisplay(BaseElectricityController controller)
        {
            if (controller == null) return;

            float prod = controller.TotalProductionKW;
            float cons = controller.TotalConsumptionKW;
            float net = controller.NetPowerKW;
            float batPct = controller.BatteryPercentage;
            bool mainOn = controller.IsMainBreakerOn;

            // 1. Production
            if (productionValueText != null)
            {
                productionValueText.text = $"{prod:F1} <size=65%>kW</size>";
            }
            if (productionFillBar != null)
            {
                productionFillBar.fillAmount = Mathf.Clamp01(prod / MaxScaleKW);
            }

            // 2. Consommation
            if (consumptionValueText != null)
            {
                consumptionValueText.text = $"{cons:F1} <size=65%>kW</size>";
            }
            if (consumptionFillBar != null)
            {
                consumptionFillBar.fillAmount = Mathf.Clamp01(cons / MaxScaleKW);
            }

            // 3. Batterie
            if (batteryPercentText != null)
            {
                batteryPercentText.text = $"{batPct:F0}%";
            }
            if (batteryCapacityText != null)
            {
                batteryCapacityText.text = $"{controller.CurrentBatteryKWh:F0} / {controller.BatteryCapacityKWh:F0} kWh";
            }
            if (batteryFillBar != null)
            {
                batteryFillBar.fillAmount = batPct / 100f;
                batteryFillBar.color = batPct > 40f ? colorStable : (batPct > 15f ? colorDischarging : colorCritical);
            }

            // 4. Bilan Net
            if (netPowerText != null)
            {
                if (!mainOn)
                {
                    netPowerText.text = "DISJONCTEUR COUPÉ";
                    netPowerText.color = colorOffline;
                }
                else if (net >= 0f)
                {
                    netPowerText.text = $"+{net:F1} kW (Charge)";
                    netPowerText.color = colorSurplus;
                }
                else
                {
                    netPowerText.text = $"{net:F1} kW (Décharge)";
                    netPowerText.color = colorDischarging;
                }
            }

            // 5. Statut Global
            if (gridStatusBadgeText != null)
            {
                if (!mainOn)
                {
                    gridStatusBadgeText.text = "HORS TENSION";
                    if (gridStatusBadgeBg != null) gridStatusBadgeBg.color = colorCritical;
                }
                else if (controller.CurrentMode == PowerGridMode.Emergency)
                {
                    gridStatusBadgeText.text = "URGENCE";
                    if (gridStatusBadgeBg != null) gridStatusBadgeBg.color = colorCritical;
                }
                else if (net < -0.1f)
                {
                    gridStatusBadgeText.text = "SUR BATTERIE";
                    if (gridStatusBadgeBg != null) gridStatusBadgeBg.color = colorDischarging;
                }
                else
                {
                    gridStatusBadgeText.text = "STABLE";
                    if (gridStatusBadgeBg != null) gridStatusBadgeBg.color = colorStable;
                }
            }

            // 6. Mode Badge
            if (modeBadgeText != null)
            {
                modeBadgeText.text = $"MODE : {controller.CurrentMode.ToString().ToUpper()}";
            }

            // 7. Voyants des prises d'entrée
            if (inputSocketLeds != null && controller.InputSockets != null)
            {
                for (int i = 0; i < inputSocketLeds.Length; i++)
                {
                    if (inputSocketLeds[i] == null) continue;

                    if (i < controller.InputSockets.Length && controller.InputSockets[i] != null)
                    {
                        bool isPow = controller.InputSockets[i].IsPowered;
                        inputSocketLeds[i].color = isPow ? colorStable : colorOffline;
                    }
                    else
                    {
                        inputSocketLeds[i].color = colorOffline;
                    }
                }
            }

            // 8. Voyant prise de sortie
            if (outputSocketLed != null && controller.OutputSocket != null)
            {
                outputSocketLed.color = controller.OutputSocket.IsPowered ? colorStable : colorOffline;
            }
        }
    }
}
