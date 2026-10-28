using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

namespace BuildingSystem.Electricity
{
    /// <summary>
    /// Interface interactive complète pour la lecture et la gestion détaillée de l'électricité de la base.
    /// Permet de contrôler les circuits individuels, le disjoncteur général, les modes d'alimentation,
    /// le générateur auxiliaire et d'analyser toute la télémétrie en temps réel.
    /// </summary>
    public class BaseElectricityTerminalUI : MonoBehaviour
    {
        public static BaseElectricityTerminalUI Instance { get; private set; }

        [Header("Conteneurs Principaux")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject darkOverlay;
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private GraphicRaycaster graphicRaycaster;
        [SerializeField] private Button closeButton;

        [Header("Télémétrie - Production")]
        [SerializeField] private TextMeshProUGUI totalProductionText;
        [SerializeField] private TextMeshProUGUI solarProductionText;
        [SerializeField] private TextMeshProUGUI auxProductionText;
        [SerializeField] private Slider productionSlider;

        [Header("Télémétrie - Consommation")]
        [SerializeField] private TextMeshProUGUI totalConsumptionText;
        [SerializeField] private TextMeshProUGUI activeCircuitsCountText;
        [SerializeField] private Slider consumptionSlider;

        [Header("Télémétrie - Batterie")]
        [SerializeField] private TextMeshProUGUI batteryPercentText;
        [SerializeField] private TextMeshProUGUI batteryCapacityText;
        [SerializeField] private TextMeshProUGUI netPowerText;
        [SerializeField] private TextMeshProUGUI estimatedTimeText;
        [SerializeField] private Slider batterySlider;
        [SerializeField] private Image batteryFillImage;

        [Header("Télémétrie - Connecteurs")]
        [SerializeField] private TextMeshProUGUI[] inputSocketStatusTexts;
        [SerializeField] private Image[] inputSocketLeds;
        [SerializeField] private TextMeshProUGUI outputSocketStatusText;
        [SerializeField] private Image outputSocketLed;

        [Header("Commandes - Disjoncteur Général")]
        [SerializeField] private Button mainBreakerButton;
        [SerializeField] private TextMeshProUGUI mainBreakerButtonText;
        [SerializeField] private Image mainBreakerStatusLed;

        [Header("Commandes - Modes d'Alimentation")]
        [SerializeField] private Button modeNormalButton;
        [SerializeField] private Button modeEcoButton;
        [SerializeField] private Button modeEmergencyButton;
        [SerializeField] private Image modeNormalHighlight;
        [SerializeField] private Image modeEcoHighlight;
        [SerializeField] private Image modeEmergencyHighlight;

        [Header("Commandes - Générateur Auxiliaire")]
        [SerializeField] private Button auxGeneratorButton;
        [SerializeField] private TextMeshProUGUI auxGeneratorButtonText;
        [SerializeField] private Image auxGeneratorLed;

        [Header("Commandes - Circuits")]
        [SerializeField] private Transform circuitRowsContainer;
        [SerializeField] private GameObject circuitRowPrefab;

        [Header("Actions Rapides")]
        [SerializeField] private Button quickSheddingButton;
        [SerializeField] private Button quickRestoreButton;

        [Header("Sons (Optionnel)")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip clickSound;

        [Header("Palette Couleurs")]
        [SerializeField] private Color colorActiveGreen = new Color(0f, 0.9f, 0.45f, 1f);
        [SerializeField] private Color colorInactiveGray = new Color(0.4f, 0.45f, 0.5f, 1f);
        [SerializeField] private Color colorWarningAmber = new Color(1f, 0.72f, 0.1f, 1f);
        [SerializeField] private Color colorAlertRed = new Color(1f, 0.25f, 0.25f, 1f);
        [SerializeField] private Color colorCyan = new Color(0f, 0.85f, 1f, 1f);

        private BaseElectricityController currentController;
        private bool isOpen = false;
        private float openCooldownTimer = 0f;
        private readonly List<CircuitRowItemUI> spawnedRows = new List<CircuitRowItemUI>();

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

            if (rootCanvas == null) rootCanvas = GetComponent<Canvas>();
            if (graphicRaycaster == null) graphicRaycaster = GetComponent<GraphicRaycaster>();

            WireButtons();
            CloseTerminal();
        }

        private void WireButtons()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(CloseTerminal);
            }

            if (darkOverlay != null)
            {
                var overlayBtn = darkOverlay.GetComponent<UnityEngine.UI.Button>();
                if (overlayBtn == null) overlayBtn = darkOverlay.AddComponent<UnityEngine.UI.Button>();
                overlayBtn.transition = UnityEngine.UI.Selectable.Transition.None;
                overlayBtn.onClick.RemoveAllListeners();
                overlayBtn.onClick.AddListener(CloseTerminal);
            }

            if (mainBreakerButton != null)
            {
                mainBreakerButton.onClick.RemoveAllListeners();
                mainBreakerButton.onClick.AddListener(() =>
                {
                    PlayClickSound();
                    if (currentController != null) currentController.ToggleMainBreaker();
                });
            }

            if (modeNormalButton != null)
            {
                modeNormalButton.onClick.RemoveAllListeners();
                modeNormalButton.onClick.AddListener(() =>
                {
                    PlayClickSound();
                    if (currentController != null) currentController.SetPowerMode(PowerGridMode.Normal);
                });
            }

            if (modeEcoButton != null)
            {
                modeEcoButton.onClick.RemoveAllListeners();
                modeEcoButton.onClick.AddListener(() =>
                {
                    PlayClickSound();
                    if (currentController != null) currentController.SetPowerMode(PowerGridMode.Eco);
                });
            }

            if (modeEmergencyButton != null)
            {
                modeEmergencyButton.onClick.RemoveAllListeners();
                modeEmergencyButton.onClick.AddListener(() =>
                {
                    PlayClickSound();
                    if (currentController != null) currentController.SetPowerMode(PowerGridMode.Emergency);
                });
            }

            if (auxGeneratorButton != null)
            {
                auxGeneratorButton.onClick.RemoveAllListeners();
                auxGeneratorButton.onClick.AddListener(() =>
                {
                    PlayClickSound();
                    if (currentController != null) currentController.ToggleAuxiliaryGenerator();
                });
            }

            if (quickSheddingButton != null)
            {
                quickSheddingButton.onClick.RemoveAllListeners();
                quickSheddingButton.onClick.AddListener(() =>
                {
                    PlayClickSound();
                    if (currentController != null) currentController.EmergencyLoadShedding();
                });
            }

            if (quickRestoreButton != null)
            {
                quickRestoreButton.onClick.RemoveAllListeners();
                quickRestoreButton.onClick.AddListener(() =>
                {
                    PlayClickSound();
                    if (currentController != null) currentController.RestoreAllCircuits();
                });
            }
        }

        private void Update()
        {
            if (openCooldownTimer > 0f)
            {
                openCooldownTimer -= Time.deltaTime;
            }

            if (!isOpen) return;

            // Rafraîchissement télémétrique périodique (au cas où la batterie ou la production fluctue)
            RefreshDisplay();

            // Fermer avec E ou Échap
            if (openCooldownTimer <= 0f)
            {
                if (Keyboard.current != null)
                {
                    if (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.eKey.wasPressedThisFrame)
                    {
                        CloseTerminal();
                    }
                }
            }
        }

        public void OpenTerminal(BaseElectricityController controller)
        {
            currentController = controller ?? BaseElectricityController.Instance;
            if (currentController == null) return;

            isOpen = true;
            openCooldownTimer = 0.25f;

            if (rootCanvas != null)
            {
                rootCanvas.enabled = true;
            }
            if (graphicRaycaster != null)
            {
                graphicRaycaster.enabled = true;
            }
            if (darkOverlay != null)
            {
                darkOverlay.SetActive(true);
            }
            if (mainPanel != null)
            {
                mainPanel.SetActive(true);
            }

            // Désactiver le contrôle de la caméra et libérer le curseur
            var player = Object.FindAnyObjectByType<PlayerBaseController>();
            if (player != null)
            {
                player.canMove = false;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            PopulateCircuits();
            RefreshDisplay();
        }

        public void CloseTerminal()
        {
            isOpen = false;

            if (mainPanel != null)
            {
                mainPanel.SetActive(false);
            }
            if (darkOverlay != null)
            {
                darkOverlay.SetActive(false);
            }
            if (graphicRaycaster != null)
            {
                graphicRaycaster.enabled = false;
            }
            if (rootCanvas != null)
            {
                rootCanvas.enabled = false;
            }

            // Rétablir le contrôle du joueur
            var player = Object.FindAnyObjectByType<PlayerBaseController>();
            if (player != null)
            {
                player.canMove = true;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void RefreshDisplay()
        {
            if (currentController == null) return;

            float prod = currentController.TotalProductionKW;
            float cons = currentController.TotalConsumptionKW;
            float net = currentController.NetPowerKW;
            float batPct = currentController.BatteryPercentage;
            bool mainOn = currentController.IsMainBreakerOn;
            PowerGridMode mode = currentController.CurrentMode;

            // 1. Production
            if (totalProductionText != null) totalProductionText.text = $"{prod:F1} kW";
            if (productionSlider != null) productionSlider.value = Mathf.Clamp01(prod / 30f);
            if (solarProductionText != null)
            {
                float solar = prod - (currentController.IsAuxiliaryGeneratorOn ? currentController.AuxiliaryGeneratorPowerKW : 0f);
                solarProductionText.text = $"Solaire : {solar:F1} kW";
            }
            if (auxProductionText != null)
            {
                auxProductionText.text = currentController.IsAuxiliaryGeneratorOn 
                    ? $"Auxiliaire : +{currentController.AuxiliaryGeneratorPowerKW:F1} kW" 
                    : "Auxiliaire : 0.0 kW (Arrêté)";
            }

            // 2. Consommation
            if (totalConsumptionText != null) totalConsumptionText.text = $"{cons:F1} kW";
            if (consumptionSlider != null) consumptionSlider.value = Mathf.Clamp01(cons / 30f);
            if (activeCircuitsCountText != null)
            {
                int activeCount = 0;
                foreach (var c in currentController.Circuits) if (c.isActive) activeCount++;
                activeCircuitsCountText.text = $"{activeCount} / {currentController.Circuits.Count} circuits actifs";
            }

            // 3. Batterie & Bilan
            if (batteryPercentText != null) batteryPercentText.text = $"{batPct:F0}%";
            if (batteryCapacityText != null) batteryCapacityText.text = $"{currentController.CurrentBatteryKWh:F1} / {currentController.BatteryCapacityKWh:F0} kWh";
            if (batterySlider != null) batterySlider.value = batPct / 100f;
            if (batteryFillImage != null)
            {
                batteryFillImage.color = batPct > 40f ? colorActiveGreen : (batPct > 15f ? colorWarningAmber : colorAlertRed);
            }

            if (netPowerText != null)
            {
                if (!mainOn)
                {
                    netPowerText.text = "DISJONCTEUR COUPÉ (Consommation nulle)";
                    netPowerText.color = colorInactiveGray;
                }
                else if (net >= 0f)
                {
                    netPowerText.text = $"+{net:F1} kW (Surplus / En charge)";
                    netPowerText.color = colorActiveGreen;
                }
                else
                {
                    netPowerText.text = $"{net:F1} kW (Déficit / Décharge)";
                    netPowerText.color = colorAlertRed;
                }
            }

            if (estimatedTimeText != null)
            {
                if (!mainOn)
                {
                    estimatedTimeText.text = "Réseau en veille";
                }
                else if (net > 0.1f)
                {
                    float hoursToFull = (currentController.BatteryCapacityKWh - currentController.CurrentBatteryKWh) / net;
                    estimatedTimeText.text = $"Pleine charge dans : {FormatHours(hoursToFull)}";
                }
                else if (net < -0.1f)
                {
                    float hoursToEmpty = currentController.CurrentBatteryKWh / Mathf.Abs(net);
                    estimatedTimeText.text = $"Autonomie estimée : {FormatHours(hoursToEmpty)}";
                }
                else
                {
                    estimatedTimeText.text = "Équilibre parfait (Autonomie stable)";
                }
            }

            // 4. Disjoncteur Général
            if (mainBreakerButtonText != null)
            {
                mainBreakerButtonText.text = mainOn ? "ALIMENTATION GÉNÉRALE : ACTIVE" : "DISJONCTEUR GÉNÉRAL : COUPÉ";
            }
            if (mainBreakerStatusLed != null)
            {
                mainBreakerStatusLed.color = mainOn ? colorActiveGreen : colorAlertRed;
            }

            // 5. Modes
            if (modeNormalHighlight != null) modeNormalHighlight.enabled = (mode == PowerGridMode.Normal);
            if (modeEcoHighlight != null) modeEcoHighlight.enabled = (mode == PowerGridMode.Eco);
            if (modeEmergencyHighlight != null) modeEmergencyHighlight.enabled = (mode == PowerGridMode.Emergency);

            // 6. Générateur Auxiliaire
            if (auxGeneratorButtonText != null)
            {
                auxGeneratorButtonText.text = currentController.IsAuxiliaryGeneratorOn 
                    ? "ARRÊTER GÉNÉRATEUR (+10 kW)" 
                    : "DÉMARRER GÉNÉRATEUR (+10 kW)";
            }
            if (auxGeneratorLed != null)
            {
                auxGeneratorLed.color = currentController.IsAuxiliaryGeneratorOn ? colorActiveGreen : colorInactiveGray;
            }

            // 7. Prises
            if (inputSocketStatusTexts != null && currentController.InputSockets != null)
            {
                for (int i = 0; i < inputSocketStatusTexts.Length; i++)
                {
                    if (inputSocketStatusTexts[i] == null) continue;

                    if (i < currentController.InputSockets.Length && currentController.InputSockets[i] != null)
                    {
                        bool isPow = currentController.InputSockets[i].IsPowered;
                        inputSocketStatusTexts[i].text = isPow ? $"Entrée {i + 1} : Raccordée" : $"Entrée {i + 1} : Débranchée";
                        if (inputSocketLeds != null && i < inputSocketLeds.Length && inputSocketLeds[i] != null)
                        {
                            inputSocketLeds[i].color = isPow ? colorActiveGreen : colorInactiveGray;
                        }
                    }
                    else
                    {
                        inputSocketStatusTexts[i].text = $"Entrée {i + 1} : Libre";
                        if (inputSocketLeds != null && i < inputSocketLeds.Length && inputSocketLeds[i] != null)
                        {
                            inputSocketLeds[i].color = colorInactiveGray;
                        }
                    }
                }
            }

            if (outputSocketStatusText != null && currentController.OutputSocket != null)
            {
                bool isPow = currentController.OutputSocket.IsPowered;
                outputSocketStatusText.text = isPow ? "Sortie Réseau : Alimentée" : "Sortie Réseau : Hors Tension";
                if (outputSocketLed != null) outputSocketLed.color = isPow ? colorActiveGreen : colorInactiveGray;
            }

            // 8. Refresh des lignes de circuits
            foreach (var row in spawnedRows)
            {
                if (row != null) row.Refresh(currentController);
            }
        }

        private void PopulateCircuits()
        {
            if (circuitRowsContainer == null || circuitRowPrefab == null || currentController == null) return;

            foreach (var r in spawnedRows)
            {
                if (r != null) Destroy(r.gameObject);
            }
            spawnedRows.Clear();

            for (int i = 0; i < currentController.Circuits.Count; i++)
            {
                int index = i;
                GameObject rowGO = Instantiate(circuitRowPrefab, circuitRowsContainer);
                var rowUI = rowGO.GetComponent<CircuitRowItemUI>();
                if (rowUI != null)
                {
                    rowUI.Setup(index, currentController, () =>
                    {
                        PlayClickSound();
                        currentController.ToggleCircuit(index);
                    });
                    spawnedRows.Add(rowUI);
                }
            }
        }

        private void PlayClickSound()
        {
            if (audioSource != null && clickSound != null)
            {
                audioSource.PlayOneShot(clickSound);
            }
        }

        private string FormatHours(float hours)
        {
            if (hours > 99f) return "> 99h";
            int h = Mathf.FloorToInt(hours);
            int m = Mathf.FloorToInt((hours - h) * 60f);
            return $"{h}h {m:D2}m";
        }
    }
}
