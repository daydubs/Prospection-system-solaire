using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Attached to the ghost prefab of a base module.
/// Handles visual feedback (valid/invalid placement) and the progressive construction logic.
/// </summary>
public class ConstructibleGhost : MonoBehaviour
{
    private BaseModuleData moduleData;
    private BuilderController builder;

    private float currentConstructionTime = 0f;
    public bool CanBePlaced { get; private set; } = false;
    public bool IsConstructing { get; private set; } = false;

    // Optional: Renderers to tint the ghost
    private Renderer[] renderers;
    private Color validColor = new Color(0f, 1f, 0f, 0.4f);
    private Color invalidColor = new Color(1f, 0f, 0f, 0.4f);
    private Color constructingColor = new Color(0f, 0.5f, 1f, 0.6f);

    public void SetPlacementValidity(bool isValid)
    {
        if (IsConstructing) return; // Don't change visuals if we are actively building

        CanBePlaced = isValid;
        Color targetColor = isValid ? validColor : invalidColor;

        foreach (var r in renderers)
        {
            if (r.material != null)
            {
                // Simple tint, assuming the material supports it
                if (r.material.HasProperty("_Color"))
                {
                    r.material.color = targetColor;
                }
                else if (r.material.HasProperty("_BaseColor")) // For URP/HDRP
                {
                    r.material.SetColor("_BaseColor", targetColor);
                }
            }
        }
    }

    // Track fractional required progress
    private Dictionary<InventoryFramework.Item, float> exactDrainAccumulator = new Dictionary<InventoryFramework.Item, float>();
    // Store remaining integer resource costs needed to finish this building
    private Dictionary<InventoryFramework.Item, int> remainingCosts = new Dictionary<InventoryFramework.Item, int>();
    private float progressThreshold = 0f;

    public void Initialize(BaseModuleData data, BuilderController controller)
    {
        moduleData = data;
        builder = controller;
        renderers = GetComponentsInChildren<Renderer>();
        SetPlacementValidity(false);

        if (moduleData.resourceCosts != null)
        {
            foreach (var cost in moduleData.resourceCosts)
            {
                if (cost.item != null)
                {
                    remainingCosts[cost.item] = cost.amount;
                    exactDrainAccumulator[cost.item] = 0f;
                }
            }
        }
    }

    public void ConstructProgress(float deltaTime)
    {
        if (!CanBePlaced) return;

        // Lock the position by setting IsConstructing to true
        IsConstructing = true;

        // Update visuals to show construction is happening
        foreach (var r in renderers)
        {
             if (r.material.HasProperty("_Color")) r.material.color = constructingColor;
             else if (r.material.HasProperty("_BaseColor")) r.material.SetColor("_BaseColor", constructingColor);
        }

        // Calculate progress percentage
        float progressDelta = deltaTime / moduleData.baseConstructionTime;

        // Drain resources progressively by integers
        if (builder.inventory != null && moduleData.resourceCosts != null)
        {
            bool hasAllResourcesForTick = true;

            foreach (var cost in moduleData.resourceCosts)
            {
                if (cost.item == null || remainingCosts[cost.item] <= 0) continue;

                float totalCost = cost.amount;
                float expectedFractionalDrain = totalCost * progressDelta;

                exactDrainAccumulator[cost.item] += expectedFractionalDrain;

                // If accumulated drain goes above 1, we need to consume an integer amount
                if (exactDrainAccumulator[cost.item] >= 1f)
                {
                    int unitsToConsume = Mathf.FloorToInt(exactDrainAccumulator[cost.item]);
                    // Don't consume more than remaining
                    unitsToConsume = Mathf.Min(unitsToConsume, remainingCosts[cost.item]);

                    if (unitsToConsume > 0)
                    {
                        if (builder.inventory.HasItem(cost.item, unitsToConsume))
                        {
                            builder.inventory.ConsumeItem(cost.item, unitsToConsume);
                            remainingCosts[cost.item] -= unitsToConsume;
                            exactDrainAccumulator[cost.item] -= unitsToConsume;
                        }
                        else
                        {
                            // Player ran out of this resource
                            hasAllResourcesForTick = false;
                            exactDrainAccumulator[cost.item] -= expectedFractionalDrain; // Revert accumulator
                            Debug.LogWarning($"[Building] Insufficient {cost.item.itemName} to continue construction!");
                            break;
                        }
                    }
                }

                // If it's the very last tick and there is still some remaining cost, we must consume it
                float expectedProgress = (currentConstructionTime + deltaTime) / moduleData.baseConstructionTime;
                if (expectedProgress >= 0.999f && hasAllResourcesForTick && remainingCosts[cost.item] > 0)
                {
                    int finalUnits = remainingCosts[cost.item];
                    if (builder.inventory.HasItem(cost.item, finalUnits))
                    {
                        builder.inventory.ConsumeItem(cost.item, finalUnits);
                        remainingCosts[cost.item] -= finalUnits;
                    }
                    else
                    {
                        hasAllResourcesForTick = false;
                        exactDrainAccumulator[cost.item] -= expectedFractionalDrain;
                        Debug.LogWarning($"[Building] Insufficient {cost.item.itemName} to finish construction!");
                        break;
                    }
                }
            }

            if (!hasAllResourcesForTick)
            {
                // Stop progress if we hit a resource wall
                IsConstructing = false;
                SetPlacementValidity(false); // Make it red to indicate an issue
                return;
            }
        }

        currentConstructionTime += deltaTime;

        // UI Feedback could go here (e.g. updating a progress bar)
        if (currentConstructionTime - progressThreshold > 0.5f)
        {
            progressThreshold = currentConstructionTime;
            Debug.Log($"[Building] Construction progress: {(currentConstructionTime / moduleData.baseConstructionTime) * 100:0}%");
        }

        if (currentConstructionTime >= moduleData.baseConstructionTime)
        {
            FinishConstruction();
        }
    }

    public void ResetProgress()
    {
        // When stopping building mid-way, we don't reset the currentConstructionTime entirely,
        // allowing the player to resume where they left off!
        // We only reset the visual state.
        IsConstructing = false;
        SetPlacementValidity(CanBePlaced);
    }

    private void FinishConstruction()
    {
        // 2. Tell the builder to spawn the real object
        builder.FinalizeConstruction(moduleData.finalPrefab, transform.position, transform.rotation);
    }
}
