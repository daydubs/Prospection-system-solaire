using UnityEngine;

public class MineableResource : MonoBehaviour, IInteractable
{
    [Header("Resource Settings")]
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
            bool success = GameManager.Instance.AddCargo(resourceId, resourceName, quantityTons, basePrice);
            if (success)
            {
                Debug.Log($"[MineableResource] Miné avec succès : {quantityTons}t de {resourceName}");
                Destroy(gameObject);
            }
            else
            {
                Debug.LogWarning($"[MineableResource] Impossible de miner {resourceName}, soute pleine.");
            }
        }
        else
        {
            Debug.LogWarning("[MineableResource] GameManager.Instance introuvable !");
        }
    }
}
