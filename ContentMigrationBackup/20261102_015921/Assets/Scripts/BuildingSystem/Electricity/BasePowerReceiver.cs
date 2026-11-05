using UnityEngine;
using BuildingSystem.Cables;

namespace BuildingSystem.Electricity
{
    [DisallowMultipleComponent]
    public class BasePowerReceiver : MonoBehaviour
    {
        [SerializeField] private ElectricConnectorSocket _inputSocket;
        private ScrubberUnit[] _scrubbers = new ScrubberUnit[0];
        private float _nextScan;
        public ElectricConnectorSocket InputSocket => _inputSocket;
        public bool IsSupplied { get; private set; }
        public bool ScrubberSupplied { get; private set; }
        public void SetSupply(bool supplied, bool scrubberSupplied)
        {
            IsSupplied = supplied;
            ScrubberSupplied = supplied && scrubberSupplied;
            ApplyConsumers();
        }
        private void OnEnable()
        {
            if (_inputSocket != null) _inputSocket.OnPowerStateChanged += OnPowerChanged;
            ScanConsumers();
            SetSupply(false, false);
        }
        private void OnDisable()
        {
            if (_inputSocket != null) _inputSocket.OnPowerStateChanged -= OnPowerChanged;
            SetSupply(false, false);
        }
        private void OnPowerChanged(bool powered) { if (!powered) SetSupply(false, false); }
        private void Update()
        {
            if (Time.unscaledTime >= _nextScan) ScanConsumers();
            if (_inputSocket == null || _inputSocket.State != SocketConnectionState.Connected || !_inputSocket.IsPowered) SetSupply(false, false);
        }
        public void RefreshMountedConsumers() => ScanConsumers();
        private void ScanConsumers()
        {
            _nextScan = Time.unscaledTime + 0.5f;
            var consumers = new System.Collections.Generic.List<ScrubberUnit>(GetComponentsInChildren<ScrubberUnit>());
            foreach (var socket in GetComponentsInChildren<EquipmentSocket>())
            {
                if (!socket.isHostSocket || !socket.isOccupied || socket.occupiedEquipment == null) continue;
                var scrubber = socket.occupiedEquipment.GetComponentInChildren<ScrubberUnit>();
                if (scrubber != null && !consumers.Contains(scrubber)) consumers.Add(scrubber);
            }
            _scrubbers = consumers.ToArray();
            ApplyConsumers();
        }
        private void ApplyConsumers()
        {
            foreach (var scrubber in _scrubbers)
                if (scrubber != null) scrubber.isOperational = ScrubberSupplied;
        }
    }
}
