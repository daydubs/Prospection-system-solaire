using UnityEngine;

namespace BuildingSystem.Cables
{
    public class CableInteractionManager : MonoBehaviour
    {
        public static CableInteractionManager Instance { get; private set; }
        [SerializeField] private Transform carryAnchor;
        [SerializeField] private Vector3 carryOffset = new Vector3(0.35f, -0.3f, 0.65f);
        [SerializeField] private float maxCableLength = 15f;
        [SerializeField] private GameObject plugPrefab;
        [SerializeField] private PlayerBaseController playerController;
        [SerializeField] private BuilderController builderController;
        private CableConnectorSocket _carryingSource;
        private GameObject _tempAnchorObject;
        private GameObject _carryPlugInstance;
        private UnityEngine.InputSystem.InputAction _cancel;
        public CableConnectorSocket CarryingSourceSocket => _carryingSource;
        public bool IsCarryingCable => _carryingSource != null;
        public bool InteractionsBlocked => (playerController != null && (playerController.isInventoryOpen || playerController.isBlueprintOpen)) ||
            (builderController != null && builderController.isBuildModeActive) ||
            (MainMenuController.Instance != null && MainMenuController.Instance.isMenuOpen) ||
            (CraftingSystem.CraftingUIManager.Instance != null && CraftingSystem.CraftingUIManager.Instance.IsOpen);

        private void Awake()
        {
            if (Instance != null && Instance != this) { enabled = false; return; }
            Instance = this;
            _cancel = new UnityEngine.InputSystem.InputAction("CancelCable", UnityEngine.InputSystem.InputActionType.Button, "<Mouse>/rightButton");
        }
        private void OnEnable()
        {
            if (_cancel == null) return;
            _cancel.performed += OnCancel;
            _cancel.Enable();
        }
        private void OnDisable()
        {
            if (_cancel != null) { _cancel.performed -= OnCancel; _cancel.Disable(); }
            CancelCarrying();
        }
        private void OnDestroy()
        {
            _cancel?.Dispose();
            if (_tempAnchorObject != null) Destroy(_tempAnchorObject);
            if (_carryPlugInstance != null) Destroy(_carryPlugInstance);
            if (Instance == this) Instance = null;
        }
        private void Start()
        {
            EnsureCarryAnchor();
            if (carryAnchor != null && playerController == null) playerController = carryAnchor.GetComponentInParent<PlayerBaseController>();
            if (playerController != null && builderController == null) builderController = playerController.GetComponent<BuilderController>();
        }
        private void OnCancel(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            if (!InteractionsBlocked) CancelCarrying();
        }
        private void EnsureCarryAnchor()
        {
            if (carryAnchor != null) return;
            Camera camera = Camera.main;
            if (camera == null) return;
            _tempAnchorObject = new GameObject("CableCarryAnchor");
            _tempAnchorObject.transform.SetParent(camera.transform, false);
            carryAnchor = _tempAnchorObject.transform;
        }
        private void LateUpdate()
        {
            if (_carryingSource == null)
            {
                if (_carryPlugInstance != null) Destroy(_carryPlugInstance);
                return;
            }
            if (InteractionsBlocked) { CancelCarrying(); return; }
            if (carryAnchor == null) { CancelCarrying(); return; }
            carryAnchor.localPosition = carryOffset;
            carryAnchor.localRotation = Quaternion.identity;
            Vector3 delta = carryAnchor.position - _carryingSource.transform.position;
            if (delta.magnitude > maxCableLength) carryAnchor.position = _carryingSource.transform.position + delta.normalized * maxCableLength;
        }
        public string GetPrompt(CableConnectorSocket target)
        {
            if (_carryingSource == null) return null;
            if (target == _carryingSource) return "Rembobiner le flexible";
            if (!_carryingSource.CanConnectTo(target, out string reason)) return reason;
            return _carryingSource.Kind == CableKind.Rubble ? "Raccorder le tuyau de gravats" : "Raccorder le câble électrique";
        }
        public void HandleSocketInteraction(CableConnectorSocket clickedSocket)
        {
            if (clickedSocket == null || InteractionsBlocked) return;
            if (_carryingSource != null)
            {
                if (_carryingSource == clickedSocket) { CancelCarrying(); return; }
                if (!_carryingSource.CanConnectTo(clickedSocket, out _)) return;
                CableConnectorSocket source = _carryingSource;
                source.ConnectTo(clickedSocket);
                if (source.Peer != clickedSocket) return;
                _carryingSource = null;
                if (_carryPlugInstance != null) Destroy(_carryPlugInstance);
                return;
            }
            if (clickedSocket.State == SocketConnectionState.Connected) { clickedSocket.Disconnect(); return; }
            if (!clickedSocket.HasStoredCable) return;
            EnsureCarryAnchor();
            if (carryAnchor == null) return;
            _carryingSource = clickedSocket;
            clickedSocket.StartCarryingCable(carryAnchor);
            GameObject visual = clickedSocket.PlugPrefab != null ? clickedSocket.PlugPrefab : plugPrefab;
            if (visual != null)
            {
                _carryPlugInstance = Instantiate(visual, carryAnchor);
                _carryPlugInstance.transform.localPosition = Vector3.zero;
                _carryPlugInstance.transform.localRotation = Quaternion.identity;
                if (clickedSocket.PlugInstance != null)
                    _carryPlugInstance.transform.localScale = clickedSocket.PlugInstance.transform.lossyScale;
                if (clickedSocket.CableRenderer != null) clickedSocket.CableRenderer.EndPlug = _carryPlugInstance.transform;
            }
        }
        public void CancelCarrying()
        {
            CableConnectorSocket source = _carryingSource;
            _carryingSource = null;
            if (source != null) source.Disconnect();
            if (_carryPlugInstance != null) Destroy(_carryPlugInstance);
        }
    }
}
