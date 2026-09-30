using UnityEngine;
using InventoryFramework;

public class MineableResource : MonoBehaviour, IInteractable
{
    [Header("Inventory Item Setting (Drag & Drop)")]
    [Tooltip("L'item qui sera ajouté à l'inventaire. S'il n'est pas assigné, l'ancien système de cargo sera utilisé.")]
    public Item resourceItem;
    [Tooltip("La quantité d'item à ajouter dans l'inventaire")]
    public int dropAmount = 1;

    [Header("Resource Settings (Ancien Cargo)")]
    public string resourceId = "res_water_ice";
    public string resourceName = "Glace Volatile";
    public float quantityTons = 5f;
    public double basePrice = 180;

    [Header("Mining Requirements")]
    public string requiredToolName = "Sonic Fracturer";

    public void Interact()
    {
        // Interagir par défaut (ex: touche E) ne fera rien pour le minage.
        // Le minage s'effectue via le bouton "Attack" (Clic gauche).
    }

    public void Mine()
    {
        if (GameManager.Instance != null)
        {
            if (resourceItem != null)
            {
                ItemPickupHandler pickupHandler = FindAnyObjectByType<ItemPickupHandler>();
                bool success = false;
                if(pickupHandler != null)
                {
                    pickupHandler.PickupItem(resourceItem, dropAmount);
                    success = true;
                }
                else
                {
                    success = GameManager.Instance.AddPlayerItem(resourceItem, dropAmount);
                }

                if (success)
                {
                    Debug.Log($"[MineableResource] Miné avec succès : {dropAmount}x {resourceItem.itemName}");
                    Destroy(gameObject);
                }
                else
                {
                    Debug.LogWarning($"[MineableResource] Impossible de miner {resourceItem.itemName}, inventaire plein.");
                }
            }
            else
            {
                bool success = GameManager.Instance.AddCargo(resourceId, resourceName, quantityTons, basePrice);
                if (success)
                {
                    Debug.Log($"[MineableResource] Miné avec succès : {quantityTons}t de {resourceName} (Ajouté au cargo)");
                    Destroy(gameObject);
                }
                else
                {
                    Debug.LogWarning($"[MineableResource] Impossible de miner {resourceName}, soute pleine.");
                }
            }
        }
        else
        {
            Debug.LogWarning("[MineableResource] GameManager.Instance introuvable !");
        }
    }
}
