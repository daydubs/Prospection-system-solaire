using UnityEngine;
using BuildingSystem.Cables;

namespace BuildingSystem.Electricity
{
    public interface IBaseBatteryTerminal
    {
        void Open(BaseBatteryModule module);
        void Close();
    }

    [DisallowMultipleComponent]
    public class BaseBatteryModule : MonoBehaviour, IPromptInteractable
    {
        [Header("Stockage (sans pertes ni limite de puissance)")]
        [SerializeField, Min(0f)] private float _capacityKWh = 120f;
        [SerializeField, Min(0f)] private double _chargeKWh;
        [SerializeField] private bool _chargingAllowed = true;
        [SerializeField] private bool _dischargingAllowed = true;
        [Header("Raccordements")]
        [SerializeField] private ElectricConnectorSocket _inputSocket;
        [SerializeField] private ElectricConnectorSocket _outputSocket;
        [Header("Interface")]
        [SerializeField] private GameObject _terminalPrefab;
        private GameObject _terminalInstance;
        private IBaseBatteryTerminal _terminal;
        private int _lastFrame = -1;

        public float CapacityKWh => _capacityKWh;
        public float ChargeKWh => (float)_chargeKWh;
        public float Percentage => _capacityKWh > 0f ? (float)(100d * _chargeKWh / _capacityKWh) : 0f;
        public bool ChargingAllowed => _chargingAllowed;
        public bool DischargingAllowed => _dischargingAllowed;
        public ElectricConnectorSocket InputSocket => _inputSocket;
        public ElectricConnectorSocket OutputSocket => _outputSocket;
        public float ChargingKW { get; private set; }
        public float DischargingKW { get; private set; }
        public float WastedKW { get; private set; }
        public BaseElectricityController Controller
        {
            get
            {
                if (_inputSocket == null || _inputSocket.State != SocketConnectionState.Connected || _inputSocket.ConnectedSocket == null) return null;
                var controller = _inputSocket.ConnectedSocket.GetComponentInParent<BaseElectricityController>();
                return controller != null && controller.OutputSocket == _inputSocket.ConnectedSocket && controller.isActiveAndEnabled ? controller : null;
            }
        }
        public string Status => Controller == null ? "ISOLÉE" : !Controller.IsMainBreakerOn ? "DISJONCTEUR COUPÉ" :
            ChargingKW > 0f ? "EN CHARGE" : DischargingKW > 0f ? "DÉCHARGE / SECOURS" :
            _chargeKWh <= 0d ? "VIDE" : _chargeKWh >= _capacityKWh ? "PLEINE" : "EN VEILLE";

        private void Awake() => ValidateStorage();
        private void OnValidate() => ValidateStorage();
        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        private void ValidateStorage()
        {
            _capacityKWh = Finite(_capacityKWh) ? Mathf.Max(0f, _capacityKWh) : 0f;
            _chargeKWh = Finite(_chargeKWh) ? System.Math.Max(0d, System.Math.Min(_chargeKWh, _capacityKWh)) : 0d;
        }
        public float AvailableDischargeKW(float dt) => _dischargingAllowed && Finite(dt) && dt > 0f ? (float)(_chargeKWh * 3600d / dt) : 0f;

        // Only the physically linked controller may apply one balance per rendered frame.
        public bool ApplyEnergyBalance(BaseElectricityController caller, float productionKW, float servedLoadKW, float dt)
        {
            if (caller == null || caller != Controller || !isActiveAndEnabled || !Finite(dt) || dt <= 0f ||
                !Finite(productionKW) || !Finite(servedLoadKW) || productionKW < 0f || servedLoadKW < 0f ||
                (Application.isPlaying && _lastFrame == Time.frameCount)) return false;
            _lastFrame = Time.frameCount;
            ClearFlow();
            if (!caller.IsMainBreakerOn) return true;
            double net = (double)productionKW - servedLoadKW;
            if (net >= 0d)
            {
                double energy = _chargingAllowed ? System.Math.Min(net * dt / 3600d, _capacityKWh - _chargeKWh) : 0d;
                _chargeKWh = System.Math.Min(_capacityKWh, _chargeKWh + energy);
                ChargingKW = (float)(energy * 3600d / dt);
                WastedKW = (float)System.Math.Max(0d, net - ChargingKW);
            }
            else
            {
                double energy = _dischargingAllowed ? System.Math.Min(-net * dt / 3600d, _chargeKWh) : 0d;
                _chargeKWh = System.Math.Max(0d, _chargeKWh - energy);
                DischargingKW = (float)(energy * 3600d / dt);
            }
            return true;
        }
        public void ClearFlow() { ChargingKW = 0f; DischargingKW = 0f; WastedKW = 0f; }
        public void SetChargingAllowed(bool allowed) { _chargingAllowed = allowed; ClearFlow(); }
        public void SetDischargingAllowed(bool allowed) { _dischargingAllowed = allowed; ClearFlow(); }
        private void LateUpdate()
        {
            if (Controller == null)
            {
                ClearFlow();
                if (_outputSocket != null) _outputSocket.SetPowered(false);
            }
        }
        public string GetInteractionPrompt() => "Module Batterie [Gérer l'accumulateur]";
        public void Interact()
        {
            if (_terminal == null && _terminalPrefab != null)
            {
                _terminalInstance = Instantiate(_terminalPrefab);
                _terminal = _terminalInstance.GetComponent<IBaseBatteryTerminal>();
            }
            _terminal?.Open(this);
        }
        private void OnDisable()
        {
            ClearFlow();
            if (_outputSocket != null) _outputSocket.SetPowered(false);
            _terminal?.Close();
        }
        private void OnDestroy() { if (_terminalInstance != null) Destroy(_terminalInstance); }
    }
}
