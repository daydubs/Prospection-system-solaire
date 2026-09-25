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

    // Store remaining resource costs needed to finish this building
    private Dictionary<string, float> remainingCosts = new Dictionary<string, float>();
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
                remainingCosts[cost.resourceId] = cost.amount;
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
        float expectedProgress = (currentConstructionTime + deltaTime) / moduleData.baseConstructionTime;

        // Drain resources progressively based on expected progress
        if (builder.inventory != null && moduleData.resourceCosts != null)
        {
            bool hasAllResourcesForTick = true;

            foreach (var cost in moduleData.resourceCosts)
            {
                float totalCost = cost.amount;
                float expectedDrain = totalCost * progressDelta;

                // Don't drain more than what's remaining
                if (expectedDrain > remainingCosts[cost.resourceId])
                {
                    expectedDrain = remainingCosts[cost.resourceId];
                }

                // Try to consume the resource
                if (builder.inventory.HasResource(cost.resourceId, expectedDrain))
                {
                    builder.inventory.ConsumeResource(cost.resourceId, expectedDrain);
                    remainingCosts[cost.resourceId] -= expectedDrain;
                }
                else
                {
                    // Player ran out of this resource
                    hasAllResourcesForTick = false;
                    Debug.LogWarning($"[Building] Insufficient {cost.resourceId} to continue construction!");
                    break;
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
