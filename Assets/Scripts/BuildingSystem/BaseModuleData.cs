using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Data definition for a constructible base module.
/// </summary>
public class BaseModuleData : ScriptableObject
{
    [Header("Informations communes")]
    [SerializeField] private InventoryFramework.Item _item;
    public InventoryFramework.Item Item => _item;
    public string moduleName => _item != null ? _item.itemName : name;
    public string description => _item != null ? _item.description : string.Empty;
    public Sprite icon => _item != null ? _item.icon : null;

    [Header("Prefabs")]
    [Tooltip("The transparent, non-colliding 'ghost' version for placement.")]
    public GameObject ghostPrefab;
    [Tooltip("The final, fully functional module with colliders and interactions.")]
    public GameObject finalPrefab;

    [Header("Placement")]
    [Tooltip("Place only on outdoor terrain, without snapping to module sockets.")]
    public bool groundPlacementOnly = false;

    [Header("Déconstruction")]
    [SerializeField, Tooltip("Recette d'impression à restituer par module. Sans recette : seuls les coûts de pose payés sont rendus.")]
    private CraftingSystem.CraftingRecipe _deconstructionRecipe;
    public CraftingSystem.CraftingRecipe DeconstructionRecipe => _deconstructionRecipe;

    [Header("Building Requirements")]
    [Tooltip("Time in seconds it takes to construct this module at normal speed.")]
    public float baseConstructionTime = 5f;

    public List<ResourceCost> resourceCosts;
}

[System.Serializable]
public class ResourceCost
{
    public InventoryFramework.Item item;
    public int amount;
}
