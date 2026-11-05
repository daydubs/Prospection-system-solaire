using UnityEngine;

[DisallowMultipleComponent]
public class ConstructedModule : MonoBehaviour
{
    public BaseModuleData Data { get; private set; }
    public ModuleRefundReceipt Receipt { get; private set; }
    public bool Completed { get; private set; }
    public float Duration { get; private set; }
    public string GetBlockReason()
    {
        if (Completed || !isActiveAndEnabled || Data == null || Receipt == null) return "Module non démontable";
        if (Receipt.Error != null) return Receipt.Error;
        foreach (var child in GetComponentsInChildren<ConstructedModule>(true))
            if (child != this) return "Retirer les modules enfants avant déconstruction";
        foreach (var crusher in GetComponentsInChildren<BuildingSystem.Industry.CrusherUnit>(true))
        {
            if (crusher.RubbleStock > 0 || crusher.PowderStock > 0) return "Vider le crusher (gravats/poudre)";
            if (crusher.BatterySocket != null && crusher.BatterySocket.InstalledBattery != null) return "Retirer la batterie externe";
        }
        foreach (var socket in GetComponentsInChildren<EquipmentSocket>(true))
            if (socket.isHostSocket && (socket.isOccupied || socket.occupiedEquipment != null)) return "Retirer les équipements montés";
        foreach (var inventory in GetComponentsInChildren<InventoryFramework.Inventory>(true))
        {
            if (Occupied(inventory.slots) || Occupied(inventory.oxygenSlots) || (inventory.hotbar != null && Occupied(inventory.hotbar.slots)))
                return "Vider l'inventaire du module";
        }
        foreach (var behaviour in GetComponentsInChildren<MonoBehaviour>(true))
            if (behaviour is IModuleDeconstructionGuard guard)
            {
                string reason = guard.GetDeconstructionBlockReason();
                if (!string.IsNullOrEmpty(reason)) return reason;
            }
        return null;
    }

    public bool TryDeconstruct(InventoryFramework.Inventory inventory, InventoryFramework.Hotbar hotbar, out string reason)
    {
        reason = GetBlockReason();
        if (reason != null) return false;
        // All checks precede any world change; synchronous commit cannot race with a production Update.
        if (!InventoryFramework.InventoryRefundTransaction.TryApply(Receipt.Amounts, inventory, hotbar, true, out reason)) return false;
        Completed = true;
        foreach (var port in GetComponentsInChildren<BuildingSystem.Cables.CableConnectorSocket>(true)) port.Disconnect();
        foreach (var socket in GetComponentsInChildren<ModuleSocket>(true)) socket.DisconnectConnections();
        gameObject.SetActive(false);
        Destroy(gameObject);
        return true;
    }

    private static bool Occupied(System.Collections.Generic.List<InventoryFramework.InventorySlot> slots)
    {
        if (slots == null) return false;
        foreach (var slot in slots) if (slot != null && !slot.IsEmpty) return true;
        return false;
    }

    public void Initialize(BaseModuleData data, ModuleRefundReceipt receipt)
    {
        Data = data;
        Receipt = receipt;
        Duration = data != null && !float.IsNaN(data.baseConstructionTime) && !float.IsInfinity(data.baseConstructionTime) ? Mathf.Max(0.1f, data.baseConstructionTime) : 5f;
    }
}

public interface IModuleDeconstructionGuard
{
    string GetDeconstructionBlockReason();
}
