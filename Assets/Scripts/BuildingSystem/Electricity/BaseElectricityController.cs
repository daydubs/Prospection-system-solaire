using System;
using System.Collections.Generic;
using UnityEngine;
using BuildingSystem.Cables;

namespace BuildingSystem.Electricity
{
    /// <summary>
    /// Contrôleur central du réseau électrique de la base.
    /// Gère la production solaire, le générateur auxiliaire, le stockage par batterie,
    /// la distribution vers les circuits (Support de vie, Scrubber, Éclairage, etc.),
    /// et les interfaces de lecture et pilotage (écran 3D et terminal interactif).
    /// </summary>
    public class BaseElectricityController : MonoBehaviour, IPromptInteractable
    {
        public static BaseElectricityController Instance { get; private set; }

        [Header("Prises Électriques Matérielles")]
        [Tooltip("Prises d'entrée connectables aux générateurs/panneaux solaires (jusqu'à 4).")]
        [SerializeField] private ElectricConnectorSocket[] inputSockets = new ElectricConnectorSocket[4];

        [Tooltip("Prise de sortie principale distribuant le courant au reste de la base.")]
        [SerializeField] private ElectricConnectorSocket outputSocket;

        [Header("Production d'Énergie")]
        [Tooltip("Puissance nominale générée par chaque prise d'entrée connectée et alimentée (kW).")]
        [SerializeField] private float solarPowerPerConnectedInput = 4.5f;

        [Tooltip("Production solaire nominale de base garantie même sans câble branché (kW).")]
        [SerializeField] private float baseSolarBaselineKW = 4.0f;

        [Tooltip("État du générateur auxiliaire d'urgence.")]
        [SerializeField] private bool isAuxiliaryGeneratorOn = false;

        [Tooltip("Puissance injectée par le générateur auxiliaire lorsqu'il est actif (kW).")]
        [SerializeField] private float auxiliaryGeneratorPowerKW = 10.0f;

        [Header("Stockage Batterie")]
        [Tooltip("Capacité totale de l'accumulateur de secours de la base (kWh).")]
        [SerializeField] private float batteryCapacityKWh = 120.0f;

        [Tooltip("Charge actuelle de la batterie (kWh).")]
        [SerializeField] private float currentBatteryKWh = 88.5f;

        [Header("Gestion du Réseau")]
        [Tooltip("Disjoncteur général de la base. Si coupé, aucune alimentation n'est distribuée.")]
        [SerializeField] private bool isMainBreakerOn = true;

        [Tooltip("Mode énergétique actif.")]
        [SerializeField] private PowerGridMode currentMode = PowerGridMode.Normal;

        [Header("Circuits de la Base")]
        [SerializeField] private List<BaseCircuitData> circuits = new List<BaseCircuitData>
        {
            new BaseCircuitData("life_support", "Module de Vie & Atmosphère", 3.2f, true, true),
            new BaseCircuitData("scrubber", "Épurateur CO2 (Scrubber O2)", 1.8f, true, true),
            new BaseCircuitData("lighting", "Éclairage Base & Balises", 0.8f, false, true),
            new BaseCircuitData("airlock", "Sas & Pompes Pressurisées", 0.6f, false, true),
            new BaseCircuitData("fabricator", "Atelier & Imprimante 3D", 2.4f, false, true)
        };

        [Header("Références Interfaces")]
        [Tooltip("Interface WorldSpace fixée sur l'écran du contrôleur.")]
        [SerializeField] private BaseElectricityWorldScreenUI worldScreenUI;

        [Tooltip("Panneau de commande interactif plein écran.")]
        [SerializeField] private BaseElectricityTerminalUI terminalUI;

        [Tooltip("Prefab du terminal interactif si non présent dans la scène.")]
        [SerializeField] private GameObject terminalUIPrefab;

        // Événement déclenché lors d'un changement d'état du réseau
        public event Action OnGridStateChanged;

        // Propriétés publiques de télémétrie
        public float BatteryCapacityKWh => batteryCapacityKWh;
        public float CurrentBatteryKWh => currentBatteryKWh;
        public float BatteryPercentage => batteryCapacityKWh > 0f ? Mathf.Clamp01(currentBatteryKWh / batteryCapacityKWh) * 100f : 0f;
        public bool IsMainBreakerOn => isMainBreakerOn;
        public bool IsAuxiliaryGeneratorOn => isAuxiliaryGeneratorOn;
        public float AuxiliaryGeneratorPowerKW => auxiliaryGeneratorPowerKW;
        public PowerGridMode CurrentMode => currentMode;
        public IReadOnlyList<BaseCircuitData> Circuits => circuits;
        public ElectricConnectorSocket[] InputSockets => inputSockets;
        public ElectricConnectorSocket OutputSocket => outputSocket;

        public float TotalProductionKW
        {
            get
            {
                float prod = baseSolarBaselineKW;

                // Ajout de la puissance des prises d'entrée connectées
                if (inputSockets != null)
                {
                    foreach (var s in inputSockets)
                    {
                        if (s != null && s.IsPowered)
                        {
                            prod += solarPowerPerConnectedInput;
                        }
                    }
                }

                // Ajout du générateur auxiliaire s'il est allumé
                if (isAuxiliaryGeneratorOn)
                {
                    prod += auxiliaryGeneratorPowerKW;
                }

                return prod;
            }
        }

        public float TotalConsumptionKW
        {
            get
            {
                if (!isMainBreakerOn) return 0f;

                // Si la batterie est vide et la production nulle, le réseau s'éteint
                if (currentBatteryKWh <= 0.05f && TotalProductionKW <= 0.1f) return 0f;

                float total = 0f;
                foreach (var c in circuits)
                {
                    total += c.GetCurrentConsumption(currentMode);
                }
                return total;
            }
        }

        public float NetPowerKW => TotalProductionKW - TotalConsumptionKW;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            InitializeDefaults();
            FindSocketReferences();
        }

        private void OnEnable()
        {
            SubscribeToSockets();
        }

        private void OnDisable()
        {
            UnsubscribeFromSockets();
        }

        private void Start()
        {
            EnsureTerminalUI();
            NotifyGridChanged();
        }

        private void Update()
        {
            SimulateBattery(Time.deltaTime);
            UpdateHardwareOutput();
        }

        private void InitializeDefaults()
        {
            if (circuits == null || circuits.Count == 0)
            {
                circuits = new List<BaseCircuitData>
                {
                    new BaseCircuitData("life_support", "Module de Vie & Atmosphère", 3.2f, true, true),
                    new BaseCircuitData("scrubber", "Épurateur CO2 (Scrubber O2)", 1.8f, true, true),
                    new BaseCircuitData("lighting", "Éclairage Base & Balises", 0.8f, false, true),
                    new BaseCircuitData("airlock", "Sas & Pompes Pressurisées", 0.6f, false, true),
                    new BaseCircuitData("fabricator", "Atelier & Imprimante 3D", 2.4f, false, true)
                };
            }

            if (worldScreenUI == null)
            {
                worldScreenUI = GetComponentInChildren<BaseElectricityWorldScreenUI>();
            }
        }

        private void FindSocketReferences()
        {
            var allSockets = GetComponentsInChildren<ElectricConnectorSocket>(true);
            var inputsList = new List<ElectricConnectorSocket>();

            foreach (var s in allSockets)
            {
                string sName = s.gameObject.name.ToLower();
                if (sName.Contains("output"))
                {
                    outputSocket = s;
                }
                else if (sName.Contains("input"))
                {
                    inputsList.Add(s);
                }
            }

            if (inputsList.Count > 0)
            {
                inputsList.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));
                inputSockets = inputsList.ToArray();
            }
        }

        private void SubscribeToSockets()
        {
            if (inputSockets != null)
            {
                foreach (var s in inputSockets)
                {
                    if (s != null)
                    {
                        s.OnPowerStateChanged += HandleSocketStateChanged;
                    }
                }
            }
        }

        private void UnsubscribeFromSockets()
        {
            if (inputSockets != null)
            {
                foreach (var s in inputSockets)
                {
                    if (s != null)
                    {
                        s.OnPowerStateChanged -= HandleSocketStateChanged;
                    }
                }
            }
        }

        private void HandleSocketStateChanged(bool isPowered)
        {
            NotifyGridChanged();
        }

        private void SimulateBattery(float dt)
        {
            float net = NetPowerKW;

            // 1 kWh = 1 kW * 3600 secondes
            float energyDeltaKWh = (net / 3600f) * dt;

            currentBatteryKWh = Mathf.Clamp(currentBatteryKWh + energyDeltaKWh, 0f, batteryCapacityKWh);

            // Si la batterie tombe à zéro et qu'il y a déficit, passage automatique en mode d'urgence
            if (currentBatteryKWh <= 0.05f && net < 0f && currentMode != PowerGridMode.Emergency)
            {
                SetPowerMode(PowerGridMode.Emergency);
            }
        }

        private void UpdateHardwareOutput()
        {
            bool hasPower = isMainBreakerOn && (TotalProductionKW > 0f || currentBatteryKWh > 0.1f);

            if (outputSocket != null)
            {
                outputSocket.SetPowered(hasPower);
            }

            // Sync Scrubber operational state with scrubber circuit
            BaseCircuitData scrubberCircuit = circuits.Find(c => c.circuitId == "scrubber");
            bool scrubberActive = hasPower && (scrubberCircuit == null || scrubberCircuit.isActive);
            var scrubbers = FindObjectsByType<ScrubberUnit>(FindObjectsInactive.Exclude);
            foreach (var sc in scrubbers)
            {
                if (sc != null && sc.isOperational != scrubberActive)
                {
                    sc.isOperational = scrubberActive;
                }
            }
        }

        public void NotifyGridChanged()
        {
            OnGridStateChanged?.Invoke();
            if (worldScreenUI != null)
            {
                worldScreenUI.RefreshDisplay(this);
            }
            if (terminalUI != null && terminalUI.IsOpen)
            {
                terminalUI.RefreshDisplay();
            }
        }

        public void ToggleMainBreaker()
        {
            SetMainBreaker(!isMainBreakerOn);
        }

        public void SetMainBreaker(bool state)
        {
            isMainBreakerOn = state;
            NotifyGridChanged();
        }

        public void ToggleAuxiliaryGenerator()
        {
            SetAuxiliaryGenerator(!isAuxiliaryGeneratorOn);
        }

        public void SetAuxiliaryGenerator(bool active)
        {
            isAuxiliaryGeneratorOn = active;
            NotifyGridChanged();
        }

        public void SetPowerMode(PowerGridMode mode)
        {
            currentMode = mode;
            NotifyGridChanged();
        }

        public void ToggleCircuit(int circuitIndex)
        {
            if (circuitIndex >= 0 && circuitIndex < circuits.Count)
            {
                SetCircuitActive(circuitIndex, !circuits[circuitIndex].isActive);
            }
        }

        public void SetCircuitActive(int circuitIndex, bool active)
        {
            if (circuitIndex >= 0 && circuitIndex < circuits.Count)
            {
                circuits[circuitIndex].isActive = active;
                NotifyGridChanged();
            }
        }

        public void EmergencyLoadShedding()
        {
            currentMode = PowerGridMode.Emergency;
            foreach (var c in circuits)
            {
                if (!c.isEssential)
                {
                    c.isActive = false;
                }
            }
            NotifyGridChanged();
        }

        public void RestoreAllCircuits()
        {
            currentMode = PowerGridMode.Normal;
            foreach (var c in circuits)
            {
                c.isActive = true;
            }
            NotifyGridChanged();
        }

        private void EnsureTerminalUI()
        {
            if (terminalUI == null)
            {
                terminalUI = FindAnyObjectByType<BaseElectricityTerminalUI>();
            }

            if (terminalUI == null && terminalUIPrefab != null)
            {
                GameObject instance = Instantiate(terminalUIPrefab);
                instance.name = "ElectricityTerminalCanvas";
                terminalUI = instance.GetComponent<BaseElectricityTerminalUI>();
            }
        }

        #region IPromptInteractable Implementation

        public string GetInteractionPrompt()
        {
            return "Terminal Électrique [Gérer l'énergie]";
        }

        public void Interact()
        {
            OpenTerminal();
        }

        public void OpenTerminal()
        {
            EnsureTerminalUI();
            if (terminalUI != null)
            {
                terminalUI.OpenTerminal(this);
            }
            else
            {
                Debug.LogWarning("[BaseElectricityController] Terminal UI introuvable !");
            }
        }

        #endregion
    }
}
