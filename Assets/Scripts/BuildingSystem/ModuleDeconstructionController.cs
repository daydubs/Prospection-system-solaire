using UnityEngine;
using InventoryFramework;
using System.Text;

// Driven by the existing player's Attack events; no competing input map or Update.
public sealed class ModuleDeconstructionController
{
    private readonly HotbarUI _hotbarUI;
    private readonly InventoryUI _inventoryUI;
    private readonly Item _cutter;
    private ConstructedModule _module;
    private ScrubberUnit _scrubber;
    private bool _held, _newPress, _latched;
    private float _elapsed;
    private GUIStyle _style;
    private string _prompt;
    public bool HasCutter => _cutter != null && _hotbarUI != null && _hotbarUI.GetSelectedItem() == _cutter;
    public bool HasPrompt => !string.IsNullOrEmpty(_prompt);
    public float Progress => _elapsed / (_module != null ? _module.Duration : 5f);

    public ModuleDeconstructionController(Item cutter, HotbarUI hotbar, InventoryUI inventoryUI)
    { _cutter = cutter; _hotbarUI = hotbar; _inventoryUI = inventoryUI; }

    public void SetAttack(bool held)
    {
        if (held && !_held) _newPress = true;
        _held = held;
        if (!held) { _latched = false; _elapsed = 0f; _newPress = false; }
    }

    public void Cancel()
    {
        _module = null; _scrubber = null; _prompt = null; _elapsed = 0f;
        if (_held) _latched = true;
        _newPress = false;
    }

    public void Tick(Transform camera, float range, bool blocked, float dt)
    {
        if (blocked || !HasCutter || camera == null) { Cancel(); return; }
        ConstructedModule module = null; ScrubberUnit scrubber = null;
        if (Physics.Raycast(camera.position, camera.forward, out var hit, range, ~LayerMask.GetMask("Ignore Raycast"), QueryTriggerInteraction.Collide))
        {
            if (hit.collider.GetComponentInParent<ConstructibleGhost>() == null)
            {
                scrubber = hit.collider.GetComponentInParent<ScrubberUnit>();
                if (scrubber != null && (scrubber.hostSocket == null || scrubber.hostSocket.occupiedEquipment != scrubber.gameObject)) scrubber = null;
                if (scrubber == null) module = hit.collider.GetComponentInParent<ConstructedModule>();
            }
        }
        if (module != _module || scrubber != _scrubber)
        {
            if (_held && !_newPress) _latched = true;
            _elapsed = 0f; _module = module; _scrubber = scrubber;
        }
        _newPress = false;
        if (_module == null && _scrubber == null) { _prompt = null; return; }
        var inventory = _hotbarUI.inventory;
        var hotbar = _hotbarUI.hotbar;
        string reason = _module != null ? _module.GetBlockReason() : null;
        if (reason == null && _module != null)
            InventoryRefundTransaction.TryApply(_module.Receipt.Amounts, inventory, hotbar, false, out reason);
        if (reason != null) { _elapsed = 0f; _prompt = reason + "\n[E] Récupérer / interagir avec la cible"; return; }
        string label = _module != null ? _module.Data.moduleName : "Scrubber";
        float duration = _module != null ? _module.Duration : 5f;
        if (_held && !_latched && dt > 0f && !float.IsNaN(dt) && !float.IsInfinity(dt)) _elapsed = Mathf.Min(duration, _elapsed + dt);
        var text = new StringBuilder();
        text.Append("[Clic gauche maintenu] ").Append(_module != null ? "Déconstruire : " : "Retirer : ").Append(label);
        text.Append("\nDécoupe : ").Append(Mathf.RoundToInt(_elapsed / duration * 100f)).Append(" %");
        if (_module != null)
        {
            text.Append("\nRestitution : "); int count = 0;
            foreach (var pair in _module.Receipt.Amounts)
            {
                if (count++ > 3) { text.Append("…"); break; }
                text.Append(pair.Key.itemName).Append(" × ").Append(pair.Value).Append("  ");
            }
            if (count == 0) text.Append("aucun matériau (coût nul)");
            if (_module.Receipt.Note != null) text.Append("\n").Append(_module.Receipt.Note);
            var battery = _module.GetComponentInChildren<BuildingSystem.Electricity.BaseBatteryModule>();
            if (battery != null && battery.ChargeKWh > 0f) text.Append("\nAttention : énergie stockée non restituée");
        }
        _prompt = text.ToString();
        if (_elapsed < duration) return;
        bool success = _module != null ? _module.TryDeconstruct(inventory, hotbar, out reason) : _scrubber.TryDismantle(inventory, hotbar, out reason);
        _latched = true; _elapsed = 0f;
        if (success)
        {
            _hotbarUI.RefreshUI();
            if (_inventoryUI != null) _inventoryUI.RefreshUI();
            _module = null; _scrubber = null; _prompt = null;
        }
        if (MainMenuController.Instance != null) MainMenuController.Instance.ShowNotification(success ? label + " démonté — restitution effectuée" : reason);
    }

    public void DrawPrompt(float centerX, float centerY)
    {
        if (!HasPrompt) return;
        if (_style == null) _style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 14, wordWrap = true };
        Color previous = GUI.color; GUI.color = Color.white;
        float width = Mathf.Min(Screen.width - 24f, 640f);
        GUI.Label(new Rect(centerX - width / 2f, centerY + 18f, width, 115f), _prompt, _style);
        GUI.Box(new Rect(centerX - 100f, centerY + 136f, 200f, 6f), GUIContent.none);
        GUI.color = new Color(1f, 0.6f, 0.2f);
        GUI.Box(new Rect(centerX - 100f, centerY + 136f, 200f * Mathf.Clamp01(Progress), 6f), GUIContent.none);
        GUI.color = previous;
    }
}
