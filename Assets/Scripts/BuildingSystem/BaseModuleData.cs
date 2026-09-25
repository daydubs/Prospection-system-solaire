using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Data definition for a constructible base module.
/// </summary>
[CreateAssetMenu(fileName = "NewBaseModule", menuName = "Building/Base Module Data")]
public class BaseModuleData : ScriptableObject
{
    [Header("General info")]
    public string moduleName = "New Module";
    [TextArea]
    public string description = "A constructible base module.";

    [Header("Prefabs")]
    [Tooltip("The transparent, non-colliding 'ghost' version for placement.")]
    public GameObject ghostPrefab;
    [Tooltip("The final, fully functional module with colliders and interactions.")]
    public GameObject finalPrefab;

    [Header("Building Requirements")]
    [Tooltip("Time in seconds it takes to construct this module at normal speed.")]
    public float baseConstructionTime = 5f;

    public List<ResourceCost> resourceCosts;
}

[System.Serializable]
public class ResourceCost
{
    public string resourceId;
    public float amount;
}
