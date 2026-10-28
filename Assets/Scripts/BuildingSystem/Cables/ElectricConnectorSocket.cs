using System;
using UnityEngine;

namespace BuildingSystem.Cables
{
    public enum SocketConnectionState
    {
        Disconnected,
        Carrying,
        Connected
    }

    /// <summary>
    /// Interactive electrical socket for modular base elements.
    /// Can house a flexible power conduit/cable, allow the player to extend and plug it into another socket,
    /// and triggers power distribution events.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ElectricConnectorSocket : MonoBehaviour, IPromptInteractable
    {
        [Header("Socket Identification")]
        [Tooltip("Display name shown in the HUD prompt.")]
        [SerializeField] private string socketDisplayName = "Prise Électrique";

        [Tooltip("True if this socket contains the spool/cable by default.")]
        [SerializeField] private bool hasStoredCable = false;

        [Header("Linked Cable & Prefabs")]
        [Tooltip("Reference to the flexible cable renderer.")]
        [SerializeField] private FlexibleCableRenderer cableRenderer;

        [Tooltip("Prefab for the visual plug inserted into the socket.")]
        [SerializeField] private GameObject plugPrefab;

        [Tooltip("Material used for the flexible cable.")]
        [SerializeField] private Material cableMaterial;

        [Header("Connected Target")]
        [Tooltip("Socket currently connected to this one.")]
        [SerializeField] private ElectricConnectorSocket connectedSocket;

        [Header("Power State")]
        [Tooltip("Whether electrical power is actively flowing through this socket.")]
        [SerializeField] private bool isPowered = false;

        [Tooltip("Optional light or emissive indicator to show connection status (Green = connected, Off/Red = disconnected).")]
        [SerializeField] private Light statusLight;

        [SerializeField] private Color connectedColor = Color.green;
        [SerializeField] private Color disconnectedColor = Color.red;

        [Header("State (Serialized)")]
        [SerializeField] private SocketConnectionState state = SocketConnectionState.Disconnected;
        [SerializeField] private GameObject plugInstance;

        public event Action<bool> OnPowerStateChanged;

        public SocketConnectionState State => state;
        public bool IsPowered => isPowered;
        public bool HasStoredCable => hasStoredCable;
        public ElectricConnectorSocket ConnectedSocket => connectedSocket;
        public FlexibleCableRenderer CableRenderer => cableRenderer;
        public GameObject PlugInstance => plugInstance;

        public void SetPowered(bool powered)
        {
            if (isPowered != powered)
            {
                isPowered = powered;
                UpdateVisualFeedback();
                OnPowerStateChanged?.Invoke(isPowered);
            }
        }

        private void Awake()
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }

            UpdateVisualFeedback();
        }

        private void Start()
        {
            // If this socket was saved connected or has a target socket
            if (state == SocketConnectionState.Connected && connectedSocket != null)
            {
                if (hasStoredCable)
                {
                    ConnectTo(connectedSocket, false);
                }
                else
                {
                    EnsureLocalPlug();
                }
            }
        }

        public string GetInteractionPrompt()
        {
            // Check if player is currently carrying a cable from any socket
            ElectricConnectorSocket activeSource = CableInteractionManager.Instance != null 
                ? CableInteractionManager.Instance.CarryingSourceSocket 
                : null;

            if (activeSource != null)
            {
                if (activeSource == this)
                {
                    return $"Rembobiner le flexible [{socketDisplayName}]";
                }
                else
                {
                    return $"Raccorder le flexible à [{socketDisplayName}]";
                }
            }

            switch (state)
            {
                case SocketConnectionState.Connected:
                    return $"Débrancher le flexible [{socketDisplayName}]";

                case SocketConnectionState.Carrying:
                    return $"Rembobiner le flexible [{socketDisplayName}]";

                case SocketConnectionState.Disconnected:
                default:
                    return hasStoredCable 
                        ? $"Dérouler le flexible d'alimentation [{socketDisplayName}]" 
                        : $"Prise disponible [{socketDisplayName}]";
            }
        }

        public void Interact()
        {
            if (CableInteractionManager.Instance == null)
            {
                // Fallback direct toggle if no manager in scene
                ToggleDirectConnection();
                return;
            }

            CableInteractionManager.Instance.HandleSocketInteraction(this);
        }

        /// <summary>
        /// Initiates pulling the cable from this socket.
        /// </summary>
        public void StartCarryingCable(Transform carryAnchor)
        {
            state = SocketConnectionState.Carrying;
            EnsureCableRenderer();

            if (cableRenderer != null)
            {
                cableRenderer.gameObject.SetActive(true);
                cableRenderer.StartPoint = transform;
                cableRenderer.EndPoint = carryAnchor;

                EnsureLocalPlug();
                cableRenderer.StartPlug = plugInstance != null ? plugInstance.transform : null;
                cableRenderer.RegenerateMesh();
            }

            UpdateVisualFeedback();
        }

        /// <summary>
        /// Connects this socket to a target socket.
        /// </summary>
        public void ConnectTo(ElectricConnectorSocket targetSocket, bool notifyTarget = true)
        {
            if (targetSocket == null || targetSocket == this) return;

            connectedSocket = targetSocket;
            state = SocketConnectionState.Connected;
            isPowered = true;

            EnsureCableRenderer();
            EnsureLocalPlug();
            GameObject targetPlug = targetSocket.EnsureLocalPlug();

            if (cableRenderer != null)
            {
                cableRenderer.gameObject.SetActive(true);
                cableRenderer.StartPoint = transform;
                cableRenderer.EndPoint = targetSocket.transform;

                cableRenderer.StartPlug = plugInstance != null ? plugInstance.transform : null;
                cableRenderer.EndPlug = targetPlug != null ? targetPlug.transform : null;

                cableRenderer.RegenerateMesh();
            }

            if (notifyTarget && targetSocket.ConnectedSocket != this)
            {
                targetSocket.OnConnectedFrom(this);
            }

            UpdateVisualFeedback();
            OnPowerStateChanged?.Invoke(isPowered);
        }

        public void OnConnectedFrom(ElectricConnectorSocket sourceSocket)
        {
            connectedSocket = sourceSocket;
            state = SocketConnectionState.Connected;
            isPowered = true;

            EnsureLocalPlug();
            UpdateVisualFeedback();
            OnPowerStateChanged?.Invoke(isPowered);
        }

        /// <summary>
        /// Disconnects and rewinds the cable back to its resting state.
        /// </summary>
        public void Disconnect()
        {
            if (connectedSocket != null && connectedSocket.ConnectedSocket == this)
            {
                ElectricConnectorSocket other = connectedSocket;
                connectedSocket = null;
                other.Disconnect();
            }

            connectedSocket = null;
            state = SocketConnectionState.Disconnected;
            isPowered = false;

            if (cableRenderer != null)
            {
                cableRenderer.gameObject.SetActive(false);
            }

            if (plugInstance != null)
            {
                plugInstance.SetActive(false);
            }

            UpdateVisualFeedback();
            OnPowerStateChanged?.Invoke(isPowered);
        }

        public GameObject EnsureLocalPlug()
        {
            if (plugInstance == null)
            {
                // Look for existing child first
                Transform existing = transform.Find("ConnectorPlug");
                if (existing != null)
                {
                    plugInstance = existing.gameObject;
                }
                else if (plugPrefab != null)
                {
                    plugInstance = Instantiate(plugPrefab, transform.position, transform.rotation, transform);
                    plugInstance.name = "ConnectorPlug";
                }
                else
                {
                    // Create minimal visual placeholder if no prefab
                    plugInstance = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    plugInstance.name = "ConnectorPlug";
                    plugInstance.transform.SetParent(transform, false);
                    plugInstance.transform.localRotation = Quaternion.Euler(90, 0, 0);
                    DestroyImmediate(plugInstance.GetComponent<Collider>());
                }
            }

            if (plugInstance != null)
            {
                plugInstance.SetActive(true);
                // Ensure all child objects (e.g. PlugMesh) are also active
                for (int i = 0; i < plugInstance.transform.childCount; i++)
                {
                    plugInstance.transform.GetChild(i).gameObject.SetActive(true);
                }

                plugInstance.transform.position = transform.position;
                plugInstance.transform.rotation = transform.rotation;

                // Compensate for any parent scale so world scale is always (1, 1, 1)
                Vector3 parentLossy = transform.lossyScale;
                plugInstance.transform.localScale = new Vector3(
                    parentLossy.x != 0 ? 1f / parentLossy.x : 1f,
                    parentLossy.y != 0 ? 1f / parentLossy.y : 1f,
                    parentLossy.z != 0 ? 1f / parentLossy.z : 1f
                );
            }

            return plugInstance;
        }

        private void EnsureCableRenderer()
        {
            if (cableRenderer == null)
            {
                // Look for existing child or create one
                cableRenderer = GetComponentInChildren<FlexibleCableRenderer>(true);
                if (cableRenderer == null)
                {
                    GameObject cableObj = new GameObject("FlexibleCable");
                    cableObj.transform.SetParent(transform, false);
                    cableRenderer = cableObj.AddComponent<FlexibleCableRenderer>();

                    if (cableMaterial != null)
                    {
                        var mr = cableObj.GetComponent<MeshRenderer>();
                        if (mr != null) mr.sharedMaterial = cableMaterial;
                    }
                }
            }
        }

        private void ToggleDirectConnection()
        {
            if (state == SocketConnectionState.Connected)
            {
                Disconnect();
            }
            else if (connectedSocket != null)
            {
                ConnectTo(connectedSocket);
            }
        }

        private void UpdateVisualFeedback()
        {
            if (statusLight != null)
            {
                statusLight.color = isPowered ? connectedColor : disconnectedColor;
                statusLight.intensity = isPowered ? 1.5f : 0.4f;
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = isPowered ? Color.green : (hasStoredCable ? Color.cyan : Color.yellow);
            Gizmos.DrawWireSphere(transform.position, 0.15f);
            Gizmos.DrawRay(transform.position, transform.forward * 0.4f);
        }
    }
}
