using UnityEngine;

namespace BuildingSystem.Electricity
{
    public class BaseBatteryTerminalUI : MonoBehaviour, IBaseBatteryTerminal
    {
        public static BaseBatteryTerminalUI OpenInstance { get; private set; }
        public static bool AnyOpen => OpenInstance != null;
        [Header("Conteneurs")]
        [SerializeField] private Canvas _canvas;
        [SerializeField] private UnityEngine.UI.GraphicRaycaster _raycaster;
        [SerializeField] private UnityEngine.UI.Button _closeButton;
        [SerializeField] private UnityEngine.UI.Button _overlayButton;
        [Header("Commandes")]
        [SerializeField] private UnityEngine.UI.Button _chargeButton;
        [SerializeField] private UnityEngine.UI.Button _dischargeButton;
        [SerializeField] private TMPro.TextMeshProUGUI _chargeButtonText;
        [SerializeField] private TMPro.TextMeshProUGUI _dischargeButtonText;
        [Header("Télémétrie")]
        [SerializeField] private TMPro.TextMeshProUGUI _statusText;
        [SerializeField] private TMPro.TextMeshProUGUI _stockText;
        [SerializeField] private TMPro.TextMeshProUGUI _percentageText;
        [SerializeField] private TMPro.TextMeshProUGUI _flowText;
        [SerializeField] private TMPro.TextMeshProUGUI _timeText;
        [SerializeField] private TMPro.TextMeshProUGUI _portsText;
        [SerializeField] private UnityEngine.UI.Image _fill;
        [SerializeField, Min(0.05f)] private float _refreshInterval = 0.2f;
        private BaseBatteryModule _module;
        private PlayerBaseController _player;
        private bool _previousCanMove;
        private CursorLockMode _previousLock;
        private bool _previousCursor;
        private float _nextRefresh;
        private float _closeAfter;
        private UnityEngine.InputSystem.InputAction _close;
        public bool IsOpen => OpenInstance == this;

        private void Awake()
        {
            if (_canvas == null) _canvas = GetComponent<Canvas>();
            if (_raycaster == null) _raycaster = GetComponent<UnityEngine.UI.GraphicRaycaster>();
            var manager = BuildingSystem.Cables.CableInteractionManager.Instance;
            _player = manager != null ? manager.PlayerController : null;
            if (_player == null && Camera.main != null) _player = Camera.main.GetComponentInParent<PlayerBaseController>();
            _close = new UnityEngine.InputSystem.InputAction("CloseBatteryTerminal", UnityEngine.InputSystem.InputActionType.Button);
            UnityEngine.InputSystem.InputActionSetupExtensions.AddBinding(_close, "<Keyboard>/escape");
            UnityEngine.InputSystem.InputActionSetupExtensions.AddBinding(_close, "<Keyboard>/e");
            _closeButton.onClick.AddListener(Close);
            _overlayButton.onClick.AddListener(Close);
            _chargeButton.onClick.AddListener(ToggleCharging);
            _dischargeButton.onClick.AddListener(ToggleDischarging);
            SetVisible(false);
        }
        private void OnEnable() { if (_close != null) _close.performed += OnCloseInput; }
        private void OnDisable()
        {
            Close();
            if (_close != null) _close.performed -= OnCloseInput;
        }
        private void OnDestroy()
        {
            Close();
            _close?.Dispose();
            _closeButton.onClick.RemoveListener(Close);
            _overlayButton.onClick.RemoveListener(Close);
            _chargeButton.onClick.RemoveListener(ToggleCharging);
            _dischargeButton.onClick.RemoveListener(ToggleDischarging);
        }
        private void OnCloseInput(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            if (IsOpen && Time.unscaledTime >= _closeAfter) Close();
        }
        public void Open(BaseBatteryModule module)
        {
            if (module == null || !module.isActiveAndEnabled || IsOpen) return;
            if (OpenInstance != null || (BaseElectricityTerminalUI.Instance != null && BaseElectricityTerminalUI.Instance.IsOpen)) return;
            if (_player != null && (!_player.canMove || _player.isInventoryOpen || _player.isBlueprintOpen)) return;
            if (MainMenuController.Instance != null && MainMenuController.Instance.isMenuOpen) return;
            if (CraftingSystem.CraftingUIManager.Instance != null && CraftingSystem.CraftingUIManager.Instance.IsOpen) return;
            if (BuildingSystem.Cables.CableInteractionManager.Instance != null &&
                (BuildingSystem.Cables.CableInteractionManager.Instance.IsCarryingCable || BuildingSystem.Cables.CableInteractionManager.Instance.InteractionsBlocked)) return;
            _module = module;
            OpenInstance = this;
            _previousLock = Cursor.lockState;
            _previousCursor = Cursor.visible;
            if (_player != null) { _previousCanMove = _player.canMove; _player.canMove = false; }
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SetVisible(true);
            _closeAfter = Time.unscaledTime + 0.25f;
            _close.Enable();
            RefreshDisplay();
        }
        public void Close()
        {
            if (!IsOpen) { SetVisible(false); return; }
            _close?.Disable();
            OpenInstance = null;
            SetVisible(false);
            _module = null;
            if (_player != null) _player.canMove = _previousCanMove;
            bool anotherMenu = (MainMenuController.Instance != null && MainMenuController.Instance.isMenuOpen) ||
                (CraftingSystem.CraftingUIManager.Instance != null && CraftingSystem.CraftingUIManager.Instance.IsOpen) ||
                (_player != null && (_player.isInventoryOpen || _player.isBlueprintOpen));
            if (!anotherMenu) { Cursor.lockState = _previousLock; Cursor.visible = _previousCursor; }
        }
        private void SetVisible(bool visible)
        {
            if (_canvas != null) _canvas.enabled = visible;
            if (_raycaster != null) _raycaster.enabled = visible;
        }
        private void Update()
        {
            if (!IsOpen) return;
            if (_module == null || !_module.isActiveAndEnabled) { Close(); return; }
            if (Time.unscaledTime >= _nextRefresh)
            {
                _nextRefresh = Time.unscaledTime + _refreshInterval;
                RefreshDisplay();
            }
        }
        private void ToggleCharging() { if (_module != null) _module.SetChargingAllowed(!_module.ChargingAllowed); RefreshDisplay(); }
        private void ToggleDischarging() { if (_module != null) _module.SetDischargingAllowed(!_module.DischargingAllowed); RefreshDisplay(); }
        public void RefreshDisplay()
        {
            if (_module == null) return;
            var controller = _module.Controller;
            float production = controller != null ? controller.TotalProductionKW : 0f;
            float demand = controller != null ? controller.RequestedConsumptionKW : 0f;
            float served = controller != null ? controller.TotalConsumptionKW : 0f;
            float unserved = controller != null ? controller.UnservedKW : 0f;
            Color green = new Color(0f, 0.9f, 0.45f);
            Color amber = new Color(1f, 0.72f, 0.1f);
            _statusText.text = _module.Status;
            _statusText.color = _module.DischargingKW > 0f || unserved > 0f ? amber : green;
            _percentageText.text = $"{_module.Percentage:F1} %";
            _stockText.text = $"{_module.ChargeKWh:F3} / {_module.CapacityKWh:F0} kWh";
            _fill.rectTransform.anchorMax = new Vector2(_module.Percentage / 100f, 1f);
            _fill.color = _module.Percentage > 15f ? green : amber;
            _flowText.text = $"Production disponible    {production:F1} kW\nDemande base                  {demand:F1} kW\nConsommation servie       {served:F1} kW\nCharge                                {_module.ChargingKW:F1} kW\nDécharge / secours          {_module.DischargingKW:F1} kW\nNon desservi                       {unserved:F1} kW\nSurplus non stocké           {_module.WastedKW:F1} kW";
            _timeText.text = "Pleine charge : " + (_module.ChargingKW > 0f ? FormatHours((_module.CapacityKWh - _module.ChargeKWh) / _module.ChargingKW) : "—") +
                "\nAutonomie au déficit actuel : " + (_module.DischargingKW > 0f ? FormatHours(_module.ChargeKWh / _module.DischargingKW) : "—");
            var input = _module.InputSocket;
            var output = _module.OutputSocket;
            string inState = input == null || input.ConnectedSocket == null ? "Débranchée" : controller == null ? "Source incompatible" : input.IsPowered ? "Contrôleur raccordé — production active" : "Contrôleur raccordé — sans production";
            string outState = output == null || output.ConnectedSocket == null ? "Débranchée" : output.IsPowered ? "Base raccordée — alimentée" : "Base raccordée — hors tension";
            _portsText.text = "ENTRÉE   " + inState + "\nSORTIE    " + outState;
            _chargeButtonText.text = _module.ChargingAllowed ? "CHARGE AUTORISÉE : ON" : "CHARGE AUTORISÉE : OFF";
            _dischargeButtonText.text = _module.DischargingAllowed ? "SECOURS AUTORISÉ : ON" : "SECOURS AUTORISÉ : OFF";
        }
        private static string FormatHours(float hours)
        {
            if (hours > 999f) return "> 999 h";
            return $"{Mathf.FloorToInt(hours)} h {Mathf.FloorToInt(hours * 60f) % 60:00} min";
        }
    }
}
