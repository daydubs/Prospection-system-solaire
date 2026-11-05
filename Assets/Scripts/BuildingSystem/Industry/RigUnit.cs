using UnityEngine;
using BuildingSystem.Cables;

namespace BuildingSystem.Industry
{
    public class RigUnit : MonoBehaviour
    {
        [Header("Connections")]
        [SerializeField] private ElectricConnectorSocket powerInput;
        [SerializeField] private MaterialConnectorSocket rubbleOutput;
        [Header("Production")]
        [SerializeField, Min(0.1f)] private float extractionInterval = 5f;
        [SerializeField] private int totalExtracted;
        [SerializeField, Min(0.001f)] private float chargePerSecond = 0.1f;
        private ElectricConnectorSocket _lastPowerPeer;
        private CrusherUnit _powerProvider;
        private float _progress;
        private CableConnectorSocket _lastPeer;
        private CrusherUnit _destination;
        public ElectricConnectorSocket PowerInput => powerInput;
        public MaterialConnectorSocket RubbleOutput => rubbleOutput;
        public int TotalExtracted => totalExtracted;
        public bool IsPowered => powerInput != null && powerInput.State == SocketConnectionState.Connected &&
            powerInput.ConnectedSocket != null && powerInput.ConnectedSocket.IsPowered;

        private void Update() { AdvanceProduction(Time.deltaTime); }
        public void AdvanceProduction(float deltaTime)
        {
            if (!isActiveAndEnabled || rubbleOutput == null) return;
            CableConnectorSocket peer = rubbleOutput.Peer;
            if (peer != _lastPeer)
            {
                _lastPeer = peer;
                _destination = peer != null ? peer.GetComponentInParent<CrusherUnit>() : null;
            }
            var powerPeer = powerInput != null ? powerInput.ConnectedSocket : null;
            if (powerPeer != _lastPowerPeer)
            {
                _lastPowerPeer = powerPeer;
                _powerProvider = powerPeer != null ? powerPeer.GetComponentInParent<CrusherUnit>() : null;
            }
            if (!IsPowered || _powerProvider == null || _powerProvider.PowerOutput != powerPeer || peer == null || _destination == null ||
                _destination.RubbleInput != peer || !_destination.CanAcceptRubble || deltaTime <= 0f) return;
            float interval = Mathf.Max(0.1f, extractionInterval);
            float usefulTime = Mathf.Max(0f, _destination.RemainingInputCapacity * interval - _progress);
            _progress += _powerProvider.ConsumeWork(Mathf.Min(deltaTime, usefulTime), Mathf.Max(0.001f, chargePerSecond));
            while (_progress + 0.000001f >= interval && _destination.TryReceiveRubble(1))
            {
                _progress = Mathf.Max(0f, _progress - interval);
                totalExtracted++;
            }
            if (!_destination.CanAcceptRubble) _progress = 0f;
        }
    }
}
