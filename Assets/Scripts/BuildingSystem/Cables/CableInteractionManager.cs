using UnityEngine;
using UnityEngine.InputSystem;

namespace BuildingSystem.Cables
{
    /// <summary>
    /// Manages the player's interaction with electrical cables:
    /// Pulling a cable from a socket, carrying it in hand (anchored to camera/hand),
    /// stretching the flexible curve in real time, plugging into a target socket, or canceling.
    /// </summary>
    public class CableInteractionManager : MonoBehaviour
    {
        private static CableInteractionManager _instance;
        public static CableInteractionManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Object.FindAnyObjectByType<CableInteractionManager>();
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [Header("Carrying Settings")]
        [Tooltip("Transform that the loose plug follows when being carried (e.g. Camera or Hand). If null, will attach to Main Camera.")]
        [SerializeField] private Transform carryAnchor;

        [Tooltip("Offset relative to the anchor transform.")]
        [SerializeField] private Vector3 carryOffset = new Vector3(0.35f, -0.3f, 0.65f);

        [Tooltip("Maximum allowed cable reach before tension blocks further pulling.")]
        [SerializeField] private float maxCableLength = 15f;

        [Tooltip("Prefab for the visual plug shown in player's hand while carrying.")]
        [SerializeField] private GameObject plugPrefab;

        private ElectricConnectorSocket _carryingSource;
        private GameObject _tempAnchorObject;
        private GameObject _carryPlugInstance;

        public ElectricConnectorSocket CarryingSourceSocket => _carryingSource;
        public bool IsCarryingCable => _carryingSource != null;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            EnsureCarryAnchor();
        }

        private void Update()
        {
            if (_carryingSource == null) return;

            // Handle Right-Click cancellation to rewind cable
            bool cancelRequested = false;
            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            {
                cancelRequested = true;
            }

            if (cancelRequested)
            {
                CancelCarrying();
                return;
            }

            // Update carry anchor position
            UpdateCarryAnchorTransform();

            // Check distance limit
            if (carryAnchor != null)
            {
                float dist = Vector3.Distance(_carryingSource.transform.position, carryAnchor.position);
                if (dist > maxCableLength)
                {
                    // Clamp anchor back towards socket to simulate cable tension
                    Vector3 dir = (carryAnchor.position - _carryingSource.transform.position).normalized;
                    carryAnchor.position = _carryingSource.transform.position + dir * maxCableLength;
                }
            }
        }

        private void EnsureCarryAnchor()
        {
            if (carryAnchor == null)
            {
                Camera cam = Camera.main;
                if (cam == null) cam = FindFirstObjectByType<Camera>();

                if (cam != null)
                {
                    _tempAnchorObject = new GameObject("CableCarryAnchor");
                    _tempAnchorObject.transform.SetParent(cam.transform, false);
                    _tempAnchorObject.transform.localPosition = carryOffset;
                    carryAnchor = _tempAnchorObject.transform;
                }
            }
        }

        private void UpdateCarryAnchorTransform()
        {
            if (carryAnchor == null)
            {
                EnsureCarryAnchor();
            }

            if (carryAnchor != null)
            {
                carryAnchor.localPosition = carryOffset;
                // Forward faces towards the camera forward
                carryAnchor.localRotation = Quaternion.identity;
            }
        }

        /// <summary>
        /// Called by a socket when interacted with by the player.
        /// </summary>
        public void HandleSocketInteraction(ElectricConnectorSocket clickedSocket)
        {
            if (clickedSocket == null) return;

            // Case 1: Player is currently carrying a cable
            if (_carryingSource != null)
            {
                if (_carryingSource == clickedSocket)
                {
                    // Clicked the same socket -> rewind
                    CancelCarrying();
                }
                else
                {
                    // Clicked a different socket -> complete connection!
                    CompleteConnection(_carryingSource, clickedSocket);
                }
                return;
            }

            // Case 2: Player is NOT carrying a cable
            if (clickedSocket.State == SocketConnectionState.Connected)
            {
                // Disconnect existing cable
                clickedSocket.Disconnect();
            }
            else if (clickedSocket.HasStoredCable)
            {
                // Start pulling cable from this socket
                StartCarryingFrom(clickedSocket);
            }
            else if (clickedSocket.ConnectedSocket != null)
            {
                // If it was linked, disconnect
                clickedSocket.Disconnect();
            }
        }

        private void StartCarryingFrom(ElectricConnectorSocket source)
        {
            EnsureCarryAnchor();
            _carryingSource = source;
            source.StartCarryingCable(carryAnchor);

            if (_carryPlugInstance == null && plugPrefab != null && carryAnchor != null)
            {
                _carryPlugInstance = Instantiate(plugPrefab, carryAnchor);
                _carryPlugInstance.name = "CarryPlug";
            }

            if (_carryPlugInstance != null)
            {
                _carryPlugInstance.SetActive(true);
                _carryPlugInstance.transform.localPosition = Vector3.zero;
                _carryPlugInstance.transform.localRotation = Quaternion.Euler(0, 180, 0);
                _carryPlugInstance.transform.localScale = Vector3.one;

                if (source.CableRenderer != null)
                {
                    source.CableRenderer.EndPlug = _carryPlugInstance.transform;
                }
            }
        }

        private void CompleteConnection(ElectricConnectorSocket source, ElectricConnectorSocket target)
        {
            if (_carryPlugInstance != null)
            {
                _carryPlugInstance.SetActive(false);
            }

            _carryingSource = null;
            source.ConnectTo(target);
        }

        public void CancelCarrying()
        {
            if (_carryPlugInstance != null)
            {
                _carryPlugInstance.SetActive(false);
            }

            if (_carryingSource != null)
            {
                ElectricConnectorSocket s = _carryingSource;
                _carryingSource = null;
                s.Disconnect();
            }
        }
    }
}
