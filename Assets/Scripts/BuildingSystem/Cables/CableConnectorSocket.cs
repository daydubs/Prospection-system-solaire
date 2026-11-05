using UnityEngine;

namespace BuildingSystem.Cables
{
    public enum CableKind { Electricity, Rubble }
    public enum CablePortRole { Legacy, Input, Output }
    public enum BaseElectricEndpoint { Unrestricted, SolarOutput, ControllerInput, ControllerOutput, BatteryInput, BatteryOutput, BaseInput }

    public abstract class CableConnectorSocket : MonoBehaviour, IPromptInteractable
    {
        [SerializeField] private BaseElectricEndpoint _baseEndpoint;
        public BaseElectricEndpoint BaseEndpoint => _baseEndpoint;
        private bool CompatibleBaseEndpoint(CableConnectorSocket other)
        {
            if (_baseEndpoint == BaseElectricEndpoint.Unrestricted && other._baseEndpoint == BaseElectricEndpoint.Unrestricted) return true;
            var output = PortRole == CablePortRole.Output ? _baseEndpoint : other._baseEndpoint;
            var input = PortRole == CablePortRole.Input ? _baseEndpoint : other._baseEndpoint;
            return (output == BaseElectricEndpoint.SolarOutput && input == BaseElectricEndpoint.ControllerInput) ||
                (output == BaseElectricEndpoint.ControllerOutput && (input == BaseElectricEndpoint.BatteryInput || input == BaseElectricEndpoint.BaseInput)) ||
                (output == BaseElectricEndpoint.BatteryOutput && input == BaseElectricEndpoint.BaseInput);
        }
        [SerializeField] private CablePortRole portRole;
        [SerializeField, Min(0.1f)] private float maximumLength = 15f;
        public CablePortRole PortRole => portRole;
        public virtual CableKind Kind => CableKind.Electricity;
        public virtual SocketConnectionState State => SocketConnectionState.Disconnected;
        public virtual bool HasStoredCable => false;
        public virtual CableConnectorSocket Peer => null;
        public virtual FlexibleCableRenderer CableRenderer => null;
        public virtual GameObject PlugPrefab => null;
        public virtual GameObject PlugInstance => null;
        public virtual string GetInteractionPrompt() => "Prise disponible";
        public virtual void Interact() { }
        public virtual void StartCarryingCable(Transform anchor) { }
        public virtual void ConnectTo(CableConnectorSocket target) { }
        public virtual void Disconnect() { }
        public virtual GameObject EnsureLocalPlug() => null;

        public bool CanConnectTo(CableConnectorSocket target, out string reason)
        {
            reason = null;
            if (target == null || target == this) reason = "Cible invalide";
            else if (!isActiveAndEnabled || !target.isActiveAndEnabled) reason = "Prise indisponible";
            else if (Kind != target.Kind) reason = "Type incompatible";
            else if ((PortRole == CablePortRole.Legacy) != (target.PortRole == CablePortRole.Legacy) ||
                     (PortRole != CablePortRole.Legacy && PortRole == target.PortRole)) reason = "Sens incompatible";
            else if (Kind == CableKind.Electricity && !CompatibleBaseEndpoint(target)) reason = "Module incompatible";
            else if ((Peer != null && Peer != target) || (target.Peer != null && target.Peer != this) ||
                     (target.State == SocketConnectionState.Carrying)) reason = "Prise occupée";
            else if (Vector3.Distance(transform.position, target.transform.position) > Mathf.Min(maximumLength, target.maximumLength)) reason = "Hors portée";
            else
            {
                FlexibleCableRenderer renderer = HasStoredCable ? CableRenderer : target.CableRenderer;
                Transform end = HasStoredCable ? target.transform : transform;
                GameObject existingPlug = HasStoredCable ? target.PlugInstance : PlugInstance;
                if (renderer != null && renderer.FollowGround &&
                    renderer.MeasureTo(end, existingPlug != null ? existingPlug.transform : null) > Mathf.Min(maximumLength, target.maximumLength))
                    reason = "Hors portée (trajet au sol)";
            }
            return reason == null;
        }

        protected virtual void OnDisable()
        {
            Disconnect();
        }
    }
}
