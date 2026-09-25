using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Basic inventory system specifically for the player character when walking around
/// on planets or in bases. distinct from the Spaceship cargo.
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    // A dictionary mapping resource IDs to their quantity in the player's personal inventory
    private Dictionary<string, float> inventory = new Dictionary<string, float>();

    /// <summary>
    /// Adds a resource to the player's inventory.
    /// </summary>
    public void AddResource(string resourceId, float amount)
    {
        if (amount <= 0) return;

        if (inventory.ContainsKey(resourceId))
        {
            inventory[resourceId] += amount;
        }
        else
        {
            inventory[resourceId] = amount;
        }

        Debug.Log($"[PlayerInventory] Added {amount} of {resourceId}. Total: {inventory[resourceId]}");
    }

    /// <summary>
    /// Checks if the player has at least the specified amount of a resource.
    /// </summary>
    public bool HasResource(string resourceId, float amount)
    {
        if (inventory.TryGetValue(resourceId, out float currentAmount))
        {
            return currentAmount >= amount;
        }
        return false;
    }

    /// <summary>
    /// Consumes a specific amount of a resource. Returns true if successful, false if not enough.
    /// </summary>
    public bool ConsumeResource(string resourceId, float amount)
    {
        if (HasResource(resourceId, amount))
        {
            inventory[resourceId] -= amount;
            Debug.Log($"[PlayerInventory] Consumed {amount} of {resourceId}. Remaining: {inventory[resourceId]}");
            return true;
        }
        return false;
    }

    /// <summary>
    /// Returns the current amount of a specific resource.
    /// </summary>
    public float GetResourceAmount(string resourceId)
    {
        if (inventory.TryGetValue(resourceId, out float amount))
        {
            return amount;
        }
        return 0f;
    }
}
