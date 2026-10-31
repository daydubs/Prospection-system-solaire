using UnityEngine;

namespace BuildingSystem.Cables
{
    [RequireComponent(typeof(Collider))]
    public class MaterialConnectorSocket : CableConnectorSocket
    {
        [SerializeField] private string socketDisplayName = "Gravats";
        [SerializeField] private bool hasStoredCable;
        [SerializeField] private GameObject plugPrefab;
        [SerializeField, Min(0.01f)] private float plugVisualScale = 1f;
        [SerializeField] private FlexibleCableRenderer cableRenderer;
        [SerializeField] private MaterialConnectorSocket connectedSocket;
        [SerializeField] private SocketConnectionState state;
        [SerializeField] private GameObject plugInstance;
        public override CableKind Kind => CableKind.Rubble;
        public override SocketConnectionState State => state;
        public override bool HasStoredCable => hasStoredCable;
        public override CableConnectorSocket Peer => connectedSocket;
        public override FlexibleCableRenderer CableRenderer => cableRenderer;
        public override GameObject PlugPrefab => plugPrefab;
        public override GameObject PlugInstance => plugInstance;

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        public override string GetInteractionPrompt()
        {
            var manager = CableInteractionManager.Instance;
            if (manager != null && manager.IsCarryingCable) return manager.GetPrompt(this);
            return state == SocketConnectionState.Connected ?$"Débrancher le tuyau [{socketDisplayName}]" :
                hasStoredCable ? $"Dérouler le tuyau de gravats [{socketDisplayName}]" : $"Entrée gravats [{socketDisplayName}]";
        }

        public override void Interact()
        {
            var manager = CableInteractionManager.Instance;
            if (manager != null) manager.HandleSocketInteraction(this);
        }

        public override void StartCarryingCable(Transform anchor)
        {
            if (!hasStoredCable || anchor == null || state == SocketConnectionState.Connected) return;
            state = SocketConnectionState.Carrying;
            if (cableRenderer == null) return;
            cableRenderer.gameObject.SetActive(true);
            cableRenderer.StartPoint = transform;
            cableRenderer.EndPoint = anchor;
            cableRenderer.StartPlug = EnsureLocalPlug().transform;
            cableRenderer.EndPlug = null;
        }

        public override void ConnectTo(CableConnectorSocket target)
        {
            if (!CanConnectTo(target, out _) || !(target is MaterialConnectorSocket socket)) return;
            connectedSocket = socket;
            socket.connectedSocket = this;
            state = socket.state = SocketConnectionState.Connected;
            GameObject local = EnsureLocalPlug();
            GameObject remote = socket.EnsureLocalPlug();
            MaterialConnectorSocket owner = hasStoredCable ? this : socket;
            if (owner.cableRenderer != null)
            {
                owner.cableRenderer.gameObject.SetActive(true);
                owner.cableRenderer.StartPoint = owner.transform;
                owner.cableRenderer.EndPoint = owner.connectedSocket.transform;
                owner.cableRenderer.StartPlug = (owner == this ? local : remote).transform;
                owner.cableRenderer.EndPlug = (owner == this ? remote : local).transform;
                owner.cableRenderer.RegenerateMesh();
            }
        }

        public override void Disconnect()
        {
            MaterialConnectorSocket other = connectedSocket;
            connectedSocket = null;
            state = SocketConnectionState.Disconnected;
            if (cableRenderer != null)
            {
                cableRenderer.EndPlug = null;
                cableRenderer.EndPoint = null;
                cableRenderer.gameObject.SetActive(false);
            }
            if (plugInstance != null) plugInstance.SetActive(false);
            if (other != null && other.connectedSocket == this) other.Disconnect();
        }

        public override GameObject EnsureLocalPlug()
        {
            if (plugInstance == null && plugPrefab != null)
            {
                plugInstance = Instantiate(plugPrefab, transform);
                plugInstance.name = "ConnectorPlug";
            }
            if (plugInstance == null)
            {
                plugInstance = new GameObject("ConnectorPlug");
                plugInstance.transform.SetParent(transform, false);
            }
            plugInstance.SetActive(true);
            plugInstance.transform.localPosition = Vector3.zero;
            plugInstance.transform.localRotation = Quaternion.identity;
            Vector3 scale = transform.lossyScale;
            plugInstance.transform.localScale = new Vector3(plugVisualScale / scale.x, plugVisualScale / scale.y, plugVisualScale / scale.z);
            return plugInstance;
        }
    }
}
