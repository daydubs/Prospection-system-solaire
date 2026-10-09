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

    [Header("Legacy Settings (Fallback)")]
    public Item resourceItem;
    public int dropAmount = 1;
    public string resourceId;
    public string resourceName;
    public float quantityTons;
    public double basePrice;

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
            ItemPickupHandler pickupHandler = FindAnyObjectByType<ItemPickupHandler>();

            bool anyAttempted = false;

            if (multipleDrops != null && multipleDrops.Count > 0)
            {
                int totalYield = Random.Range(minTotalYield, maxTotalYield + 1);

                foreach (ResourceDrop drop in multipleDrops)
                {
                    if (drop.item == null || drop.percentage <= 0f)
                    {
                        Debug.LogWarning($"[MineableResource] Invalid drop configuration: item={drop.item}, percentage={drop.percentage}");
                        continue;
                    }

                    int specificAmount = Mathf.RoundToInt(totalYield * (drop.percentage / 100f));
                    if (specificAmount <= 0)
                    {
                        Debug.LogWarning($"[MineableResource] Calculated drop amount is <= 0 for {drop.item.itemName}");
                        continue;
                    }

                    anyAttempted = true;
                    bool success = false;

                    if (pickupHandler != null)
                    {
                        success = pickupHandler.PickupItem(drop.item, specificAmount);
                    }
                    else
                    {
                        success = GameManager.Instance.AddPlayerItem(drop.item, specificAmount);
                    }

                    if (success)
                    {
                        Debug.Log($"[MineableResource] Miné avec succès : {specificAmount}x {drop.item.itemName}");
                        if (MainMenuController.Instance != null)
                        {
                            MainMenuController.Instance.ShowNotification($"+{specificAmount}x {drop.item.itemName}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[MineableResource] Inventaire plein pour {drop.item.itemName}. Item instancié au sol.");
                        SpawnPhysicalItem(drop.item, specificAmount);
                    }
                }
            }
            else if (resourceItem != null)
            {
                // Fallback to legacy fields
                anyAttempted = true;
                bool success = false;
                if (pickupHandler != null)
                {
                    success = pickupHandler.PickupItem(resourceItem, dropAmount);
                }
                else
                {
                    success = GameManager.Instance.AddPlayerItem(resourceItem, dropAmount);
                }

                if (success)
                {
                    Debug.Log($"[MineableResource] Miné avec succès (Legacy) : {dropAmount}x {resourceItem.itemName}");
                    if (MainMenuController.Instance != null)
                    {
                        MainMenuController.Instance.ShowNotification($"+{dropAmount}x {resourceItem.itemName}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[MineableResource] Inventaire plein (Legacy) pour {resourceItem.itemName}. Item instancié au sol.");
                    SpawnPhysicalItem(resourceItem, dropAmount);
                }
            }
            else
            {
                Debug.LogWarning("[MineableResource] multipleDrops est vide et resourceItem est nul. Impossible de miner cette ressource.");
            }

            if (anyAttempted)
            {
                Destroy(gameObject);
            }
        }
        else
        {
            Debug.LogWarning("[MineableResource] GameManager.Instance introuvable !");
        }
    }

    private void SpawnPhysicalItem(Item item, int amount)
    {
        if (item.model != null)
        {
            // Spawn with some random offset so they don't stack perfectly on top of each other
            Vector3 randomOffset = new Vector3(Random.Range(-0.5f, 0.5f), 0.5f, Random.Range(-0.5f, 0.5f));
            Vector3 spawnPosition = transform.position + randomOffset;

            GameObject droppedObject = Instantiate(item.model, spawnPosition, Quaternion.identity);

            // Try to assign the item and amount to the PickupItem script if it exists
            PickupItem pickupComponent = droppedObject.GetComponent<PickupItem>();
            if (pickupComponent == null)
            {
                // Add the component dynamically if it was not already on the model
                pickupComponent = droppedObject.AddComponent<PickupItem>();
            }

            pickupComponent.item = item;
            pickupComponent.amount = amount;

            // Try to ensure it has a collider so the player can actually interact with it
            if (droppedObject.GetComponent<Collider>() == null)
            {
                SphereCollider sc = droppedObject.AddComponent<SphereCollider>();
                sc.radius = 0.5f;
                sc.isTrigger = true; // Use trigger so OnTriggerEnter in PickupItem can be fired
            }
        }
        else
        {
            Debug.LogWarning($"[MineableResource] Pas de modèle 3D (model) défini pour l'item {item.itemName}. Impossible de l'instancier au sol.");
        }
    }
}
