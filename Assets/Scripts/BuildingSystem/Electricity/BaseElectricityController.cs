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
        private BaseBatteryModule _connectedBattery;
        private BasePowerReceiver _receiver;
        private ElectricConnectorSocket _lastOutputPeer;
        private ElectricConnectorSocket _lastBatteryPeer;
        private bool[] _servedCircuits;
        private float _servedKW;
        private float _requestedKW;
        private float _nextRefresh;
        public BaseBatteryModule ConnectedBattery => _connectedBattery;
        public BasePowerReceiver ConnectedBase => _receiver;
        public float RequestedConsumptionKW => _requestedKW;
        public float UnservedKW => Mathf.Max(0f, _requestedKW - _servedKW);
        public float BatteryCapacityKWh => _connectedBattery != null ? _connectedBattery.CapacityKWh : 0f;
        public float CurrentBatteryKWh => _connectedBattery != null ? _connectedBattery.ChargeKWh : 0f;
        public float BatteryPercentage => _connectedBattery != null ? _connectedBattery.Percentage : 0f;
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
                float production = isAuxiliaryGeneratorOn ? auxiliaryGeneratorPowerKW : 0f;
                if (inputSockets != null)
                    foreach (var socket in inputSockets)
                        if (socket != null && socket.State == SocketConnectionState.Connected && socket.IsPowered &&
                            socket.ConnectedSocket != null && socket.ConnectedSocket.PortRole == CablePortRole.Output)
                            production += solarPowerPerConnectedInput;
                return Mathf.Max(0f, production);
            }
        }
        public float TotalConsumptionKW => _servedKW;
        public float NetPowerKW => TotalProductionKW - TotalConsumptionKW;
        public bool IsCircuitSupplied(int index) => _servedCircuits != null && index >= 0 && index < _servedCircuits.Length && _servedCircuits[index];

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
            if (outputSocket != null) outputSocket.SetPowered(false);
            if (_receiver != null) _receiver.SetSupply(false, false);
            if (_connectedBattery != null)
            {
                _connectedBattery.ClearFlow();
                if (_connectedBattery.OutputSocket != null) _connectedBattery.OutputSocket.SetPowered(false);
            }
        }

        private void Start()
        {
            EnsureTerminalUI();
            NotifyGridChanged();
        }

        private void Update()
        {
            SimulateGrid(Time.deltaTime);
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

        private void ResolveRoute()
        {
            ElectricConnectorSocket peer = outputSocket != null ? outputSocket.ConnectedSocket : null;
            ElectricConnectorSocket batteryPeer = _connectedBattery != null && _connectedBattery.OutputSocket != null ? _connectedBattery.OutputSocket.ConnectedSocket : null;
            if (peer == _lastOutputPeer && batteryPeer == _lastBatteryPeer &&
                (_connectedBattery == null || _connectedBattery.isActiveAndEnabled) &&
                (peer != null || (_connectedBattery == null && _receiver == null))) return;
            if (_receiver != null) _receiver.SetSupply(false, false);
            if (_connectedBattery != null)
            {
                _connectedBattery.ClearFlow();
                if (_connectedBattery.OutputSocket != null) _connectedBattery.OutputSocket.SetPowered(false);
            }
            _connectedBattery = peer != null ? peer.GetComponentInParent<BaseBatteryModule>() : null;
            bool inactiveBattery = _connectedBattery != null && !_connectedBattery.isActiveAndEnabled;
            if (_connectedBattery != null && (_connectedBattery.InputSocket != peer || inactiveBattery)) _connectedBattery = null;
            ElectricConnectorSocket end = _connectedBattery != null && _connectedBattery.OutputSocket != null ? _connectedBattery.OutputSocket.ConnectedSocket : peer;
            _receiver = end != null ? end.GetComponentInParent<BasePowerReceiver>() : null;
            if (_receiver != null && _receiver.InputSocket != end) _receiver = null;
            _lastOutputPeer = inactiveBattery ? null : peer;
            _lastBatteryPeer = _connectedBattery != null && _connectedBattery.OutputSocket != null ? _connectedBattery.OutputSocket.ConnectedSocket : null;
        }

        public void SimulateGrid(float dt)
        {
            if (float.IsNaN(dt) || float.IsInfinity(dt) || dt <= 0f) return;
            ResolveRoute();
            if (_servedCircuits == null || _servedCircuits.Length != circuits.Count) _servedCircuits = new bool[circuits.Count];
            Array.Clear(_servedCircuits, 0, _servedCircuits.Length);
            _servedKW = 0f;
            _requestedKW = 0f;
            float production = TotalProductionKW;
            float available = isMainBreakerOn ? production + (_connectedBattery != null ? _connectedBattery.AvailableDischargeKW(dt) : 0f) : 0f;
            bool connected = _receiver != null && _receiver.isActiveAndEnabled;
            if (connected && isMainBreakerOn)
            {
                foreach (var circuit in circuits) _requestedKW += Mathf.Max(0f, circuit.GetCurrentConsumption(currentMode));
                // Stable priority: essential circuits first, then secondary circuits.
                for (int pass = 0; pass < 2; pass++)
                    for (int i = 0; i < circuits.Count; i++)
                    {
                        var circuit = circuits[i];
                        if (circuit.isEssential != (pass == 0)) continue;
                        float demand = Mathf.Max(0f, circuit.GetCurrentConsumption(currentMode));
                        if (!circuit.isActive || (currentMode == PowerGridMode.Emergency && !circuit.isEssential)) continue;
                        if (demand <= available)
                        {
                            _servedCircuits[i] = true;
                            _servedKW += demand;
                            available -= demand;
                        }
                    }
            }
            if (_connectedBattery != null) _connectedBattery.ApplyEnergyBalance(this, production, _servedKW, dt);
            bool supplied = isMainBreakerOn && connected && (_servedKW > 0f || production > 0f || (_connectedBattery != null && _connectedBattery.DischargingAllowed && _connectedBattery.ChargeKWh > 0f));
            // Controller output denotes upstream generation, never battery energy flowing backwards.
            if (outputSocket != null) outputSocket.SetPowered(isMainBreakerOn && production > 0f);
            if (_connectedBattery != null && _connectedBattery.OutputSocket != null) _connectedBattery.OutputSocket.SetPowered(supplied);
            bool scrubberSupplied = false;
            for (int i = 0; i < circuits.Count; i++) if (circuits[i].circuitId == "scrubber") scrubberSupplied = _servedCircuits[i];
            if (_receiver != null) _receiver.SetSupply(supplied, scrubberSupplied);
            if (Time.unscaledTime >= _nextRefresh)
            {
                _nextRefresh = Time.unscaledTime + 0.2f;
                NotifyGridChanged();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
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
