using UnityEngine;
using InventoryFramework;

[System.Serializable]
public class ResourceDrop
{
    public Item item;
    [Tooltip("Pourcentage de la récolte totale (ex: 40 pour 40%)")]
    public float percentage;
}

public class MineableResource : MonoBehaviour, IInteractable
{
    [Header("Multiple Item Settings (Composition)")]
    [Tooltip("Liste des ressources composant la roche et leurs pourcentages.")]
    public System.Collections.Generic.List<ResourceDrop> multipleDrops;
    [Tooltip("Quantité totale minimale d'items récoltés")]
    public int minTotalYield = 5;
    [Tooltip("Quantité totale maximale d'items récoltés")]
    public int maxTotalYield = 15;

    [Header("Single Inventory Item Setting (Drag & Drop)")]
    [Tooltip("L'item qui sera ajouté à l'inventaire si la liste 'multipleDrops' est vide. S'il n'est pas assigné, l'ancien système de cargo sera utilisé.")]
    public Item resourceItem;
    [Tooltip("La quantité d'item à ajouter dans l'inventaire (utilisé uniquement si single item)")]
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
            bool anySuccess = false;
            ItemPickupHandler pickupHandler = FindAnyObjectByType<ItemPickupHandler>();

            if (multipleDrops != null && multipleDrops.Count > 0)
            {
                int totalYield = Random.Range(minTotalYield, maxTotalYield + 1);

                foreach (ResourceDrop drop in multipleDrops)
                {
                    if (drop.item == null || drop.percentage <= 0f) continue;

                    int specificAmount = Mathf.RoundToInt(totalYield * (drop.percentage / 100f));
                    if (specificAmount <= 0) continue;

                    bool success = false;
                    if (pickupHandler != null)
                    {
                        pickupHandler.PickupItem(drop.item, specificAmount);
                        success = true;
                    }
                    else
                    {
                        success = GameManager.Instance.AddPlayerItem(drop.item, specificAmount);
                    }

                    if (success)
                    {
                        anySuccess = true;
                        Debug.Log($"[MineableResource] Miné avec succès : {specificAmount}x {drop.item.itemName}");
                    }
                    else
                    {
                        Debug.LogWarning($"[MineableResource] Impossible de miner {drop.item.itemName}, inventaire peut-être plein.");
                    }
                }

                if (anySuccess)
                {
                    Destroy(gameObject);
                }
            }
            else if (resourceItem != null)
            {
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
