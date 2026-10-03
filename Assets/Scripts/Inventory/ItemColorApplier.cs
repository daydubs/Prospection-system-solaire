using UnityEngine;
using InventoryFramework;

/// <summary>
/// Ce script peut être attaché au prefab du "vial" (ou de tout objet 3D physique généré lors du minage).
/// Il récupère la couleur définie dans l'Item associé et l'applique au Material (Base Map / _BaseColor) via son Renderer.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class ItemColorApplier : MonoBehaviour
{
    [Tooltip("L'item de la ressource qui contient la couleur à appliquer.")]
    public Item associatedItem;

    [Tooltip("Si vrai, appliquera la couleur automatiquement dans le Start().")]
    public bool applyOnStart = true;

    private Renderer _renderer;

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
    }

    private void Start()
    {
        if (applyOnStart && associatedItem != null)
        {
            ApplyItemColor();
        }
    }

    /// <summary>
    /// Applique la couleur de l'Item associé au Material du Renderer de cet objet.
    /// Peut être appelé manuellement lors de l'instanciation si l'Item est assigné à l'exécution.
    /// </summary>
    public void ApplyItemColor()
    {
        if (_renderer == null)
            _renderer = GetComponent<Renderer>();

        if (associatedItem != null && _renderer != null && _renderer.material != null)
        {
            // SetColor change la propriété principale de couleur (généralement _BaseColor pour l'URP ou _Color pour le standard).
            _renderer.material.color = associatedItem.itemColor;
        }
        else if (associatedItem == null)
        {
            Debug.LogWarning($"[ItemColorApplier] Aucun Item associé n'a été défini sur {gameObject.name}.", this);
        }
    }

    /// <summary>
    /// Permet d'assigner dynamiquement un nouvel item et de rafraîchir la couleur.
    /// </summary>
    /// <param name="newItem">Le nouvel item contenant la couleur.</param>
    public void SetItemAndApply(Item newItem)
    {
        associatedItem = newItem;
        ApplyItemColor();
    }
}
