using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// Handles raycasting for placing base modules, logic for snapping to sockets, and initiating construction.
/// Attach to the Player, requires a reference to the main camera.
/// </summary>
public class BuilderController : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public InventoryFramework.Inventory inventory;

    [Header("Welder Requirement")]
    public InventoryFramework.Item welderItem;

    [Header("Build Settings")]
    public float buildRange = 20f;
    public float snapDistance = 2f; // Distance from raycast point to socket to trigger a snap

    [Header("Current Build State")]
    public BaseModuleData currentModuleToBuild;
    public bool isBuildModeActive = false;
    public bool isGhostPlaced = false;

    [Header("Rotation Settings")]
    public float rotationSensitivity = 10f; // Degrees per scroll tick
    public float rotationLerpSpeed = 15f;

    private float targetYRotation = 0f;
    private float currentYRotation = 0f;

    private GameObject currentGhost;
    private Renderer groundPlacementRenderer;
    private ConstructibleGhost ghostScript;
    private ModuleSocket currentSnappedSocket;
    private InputSystem_Actions inputActions;

    private void Awake()
    {
        inputActions = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        inputActions?.Player.Enable();
    }

    private void OnDisable()
    {
        inputActions?.Player.Disable();
    }

    private void Start()
    {
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();

        if (playerCamera == null)
            playerCamera = Camera.main;

        if (inventory == null && GameManager.Instance != null)
            inventory = GameManager.Instance.playerInventory;

        if (welderItem == null)
        {
            #if UNITY_EDITOR
            welderItem = UnityEditor.AssetDatabase.LoadAssetAtPath<InventoryFramework.Item>("Assets/Items/Welder.asset");
            #endif
        }
    }

    private void Update()
    {
        if (!isBuildModeActive || currentModuleToBuild == null)
        {
            if (currentGhost != null)
            {
                DestroyGhost();
            }
            return;
        }

        if (currentGhost == null)
        {
            CreateGhost();
        }

        UpdateGhostPlacement();
        HandleBuildingInput();
    }

    public bool IsWelder(InventoryFramework.Item item)
    {
        if (item == null) return false;
        if (welderItem != null && item == welderItem) return true;
        if (string.Equals(item.itemName?.Trim(), "Welder", System.StringComparison.OrdinalIgnoreCase)) return true;
        if (item.id == 5) return true;
        return false;
    }

    public bool HasEquippedWelder()
    {
        var hotbarUI = FindAnyObjectByType<InventoryFramework.HotbarUI>();
        var hotbar = hotbarUI != null ? hotbarUI.hotbar : FindAnyObjectByType<InventoryFramework.Hotbar>();

        if (hotbar != null && hotbar.slots != null)
        {
            foreach (var slot in hotbar.slots)
            {
                if (slot != null && !slot.IsEmpty && IsWelder(slot.item))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public bool HasWelderInInventory()
    {
        if (inventory != null && inventory.slots != null)
        {
            foreach (var slot in inventory.slots)
            {
                if (slot != null && !slot.IsEmpty && IsWelder(slot.item))
                    return true;
            }
        }
        else if (GameManager.Instance != null && GameManager.Instance.playerInventory != null && GameManager.Instance.playerInventory.slots != null)
        {
            foreach (var slot in GameManager.Instance.playerInventory.slots)
            {
                if (slot != null && !slot.IsEmpty && IsWelder(slot.item))
                    return true;
            }
        }
        return false;
    }

    private float lastNotificationTime = 0f;
    public void NotifyWelderMissing()
    {
        if (Time.time - lastNotificationTime < 2f) return;
        lastNotificationTime = Time.time;

        string msg = HasWelderInInventory()
            ? "Équipement requis : Vous devez équiper le Welder dans votre barre rapide pour construire !"
            : "Équipement requis : Welder (Soudeur) manquant ! Nécessaire pour toute construction.";

        Debug.LogWarning($"[Building] {msg}");
        if (MainMenuController.Instance != null)
        {
            MainMenuController.Instance.ShowNotification(msg);
        }
    }

    public void EnterBuildMode(BaseModuleData moduleData)
    {
        if (moduleData == null) return;

        if (currentModuleToBuild != moduleData || currentGhost == null)
        {
            DestroyGhost();
            currentModuleToBuild = moduleData;
            isBuildModeActive = true;
            isGhostPlaced = false;
            targetYRotation = 0f;
            currentYRotation = 0f;
            CreateGhost();
        }
        else
        {
            isBuildModeActive = true;
        }

        if (!HasEquippedWelder())
        {
            NotifyWelderMissing();
        }
    }

    public void ExitBuildMode()
    {
        isBuildModeActive = false;
        currentModuleToBuild = null;
        isGhostPlaced = false;
        DestroyGhost();
    }

    private void CreateGhost()
    {
        if (currentModuleToBuild == null || currentModuleToBuild.ghostPrefab == null) return;

        currentGhost = Instantiate(currentModuleToBuild.ghostPrefab);
        groundPlacementRenderer = currentGhost.GetComponent<Renderer>();

        // Put ghost on Ignore Raycast layer so placement raycast never hits it
        int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
        SetLayerRecursively(currentGhost, ignoreRaycastLayer);

        // Ensure non-socket colliders (like leftover MeshColliders) do not block anything
        foreach (var col in currentGhost.GetComponentsInChildren<Collider>())
        {
            if (col.GetComponent<ModuleSocket>() == null)
            {
                col.enabled = false;
                if (Application.isPlaying) Destroy(col);
                else DestroyImmediate(col);
            }
            else
            {
                col.isTrigger = true;
            }
        }

        ghostScript = currentGhost.GetComponent<ConstructibleGhost>();

        if (ghostScript == null)
        {
            ghostScript = currentGhost.AddComponent<ConstructibleGhost>();
        }

        ghostScript.Initialize(currentModuleToBuild, this);
    }

    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }

    private void DestroyGhost()
    {
        if (currentGhost != null)
        {
            if (Application.isPlaying) Destroy(currentGhost);
            else DestroyImmediate(currentGhost);
            currentGhost = null;
            ghostScript = null;
            currentSnappedSocket = null;
            isGhostPlaced = false;
        }
    }

    private void UpdateGhostPlacement()
    {
        if (currentGhost == null || ghostScript == null || ghostScript.IsConstructing) return;

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
                playerCamera = GetComponentInChildren<Camera>();
        }
        if (playerCamera == null) return;

        bool hasWelder = HasEquippedWelder();

        if (!isGhostPlaced)
        {
            // Handle Y rotation input (horizontal yaw rotation around vertical axis)
            float scroll = 0f;
            if (inputActions != null && inputActions.Player.Rotation.enabled)
            {
                scroll = inputActions.Player.Rotation.ReadValue<float>();
            }
            if (scroll == 0f && Mouse.current != null)
            {
                scroll = Mouse.current.scroll.ReadValue().y;
            }

            // Scroll usually gives values like 120 or -120 per tick, normalize it
            if (scroll != 0)
            {
                targetYRotation += Mathf.Sign(scroll) * rotationSensitivity;
            }

            // Smoothly interpolate current rotation
            currentYRotation = Mathf.Lerp(currentYRotation, targetYRotation, Time.deltaTime * rotationLerpSpeed);

            // Cast a ray from the center of the screen, ignoring triggers and the ghost
            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            int layerMask = ~LayerMask.GetMask("Ignore Raycast");

            if (Physics.Raycast(ray, out RaycastHit hit, buildRange, layerMask, QueryTriggerInteraction.Ignore))
            {
                // Ground equipment must not snap to room/door sockets.
                ModuleSocket closestSocket = currentModuleToBuild.groundPlacementOnly
                    ? null
                    : FindClosestUnoccupiedSocket(hit.point);

                if (closestSocket != null)
                {
                    currentSnappedSocket = closestSocket;

                    // Find a matching socket on the ghost to align with
                    ModuleSocket[] ghostSockets = currentGhost.GetComponentsInChildren<ModuleSocket>();
                    ModuleSocket ghostConnectingSocket = null;

                    if (ghostSockets.Length > 0)
                    {
                        // Calculate player's intended facing direction from current rotation
                        Vector3 playerFacing = transform.forward;
                        playerFacing.y = 0;
                        if (playerFacing.sqrMagnitude < 0.01f) playerFacing = Vector3.forward;
                        Quaternion initialPrefabRot = currentModuleToBuild.ghostPrefab != null ? currentModuleToBuild.ghostPrefab.transform.rotation : Quaternion.identity;
                        Quaternion intendedGhostRot = Quaternion.LookRotation(playerFacing.normalized, Vector3.up) * Quaternion.Euler(0, currentYRotation, 0) * initialPrefabRot;

                        // Pick ghost socket whose outward direction under intendedGhostRot best matches -closestSocket.OutwardDirection
                        float bestDot = -float.MaxValue;
                        foreach (var gs in ghostSockets)
                        {
                            Vector3 gsOutward = intendedGhostRot * (gs.transform.localRotation * Vector3.forward);
                            float dot = Vector3.Dot(gsOutward, -closestSocket.OutwardDirection);
                            if (dot > bestDot)
                            {
                                bestDot = dot;
                                ghostConnectingSocket = gs;
                            }
                        }
                        if (ghostConnectingSocket == null) ghostConnectingSocket = ghostSockets[0];
                    }

                    if (ghostConnectingSocket != null)
                    {
                        // Keep vertical alignment upright for wall/door sockets
                        Vector3 socketUp = Vector3.up;
                        if (Mathf.Abs(Vector3.Dot(closestSocket.OutwardDirection, Vector3.up)) > 0.8f)
                        {
                            socketUp = closestSocket.transform.up;
                            if (Vector3.Cross(closestSocket.OutwardDirection, socketUp).sqrMagnitude < 0.01f)
                            {
                                socketUp = Vector3.forward;
                            }
                        }

                        // Rotate the ghost so its socket's outward direction is exactly opposite to the base's socket
                        Quaternion targetRotation = Quaternion.LookRotation(-closestSocket.OutwardDirection, socketUp);

                        // Rotation offset between the ghost's root and its connecting socket
                        Quaternion rotationOffset = Quaternion.Inverse(ghostConnectingSocket.transform.localRotation);
                        currentGhost.transform.rotation = targetRotation * rotationOffset;

                        // Position the ghost so its socket exactly overlaps the base's socket
                        Vector3 positionOffset = currentGhost.transform.position - ghostConnectingSocket.transform.position;
                        currentGhost.transform.position = closestSocket.transform.position + positionOffset;
                    }
                    else
                    {
                        // Fallback if ghost has no sockets defined
                        currentGhost.transform.position = closestSocket.transform.position;
                        currentGhost.transform.rotation = Quaternion.LookRotation(closestSocket.OutwardDirection, Vector3.up);
                    }

                    ghostScript.SetPlacementValidity(hasWelder);
                }
                else
                {
                    // Free placement on terrain/surface
                    currentSnappedSocket = null;
                    currentGhost.transform.position = hit.point;

                    // Keep the module upright, orient it based on player facing + Y rotation, while preserving the prefab's native mesh orientation
                    Quaternion initialPrefabRot = currentModuleToBuild.ghostPrefab != null ? currentModuleToBuild.ghostPrefab.transform.rotation : Quaternion.identity;
                    Vector3 playerForward = transform.forward;
                    playerForward.y = 0; // Keep horizontal
                    if (playerForward.sqrMagnitude > 0.01f)
                    {
                        currentGhost.transform.rotation = Quaternion.LookRotation(playerForward.normalized, Vector3.up) * Quaternion.Euler(0, currentYRotation, 0) * initialPrefabRot;
                    }
                    else
                    {
                        currentGhost.transform.rotation = Quaternion.Euler(0, currentYRotation, 0) * initialPrefabRot;
                    }

                    // Ground equipment rests on the terrain instead of using the imported mesh pivot.
                    bool validSurface = hit.normal.y > 0.7f;
                    if (currentModuleToBuild.groundPlacementOnly)
                    {
                        validSurface &= hit.collider is TerrainCollider;
                        if (groundPlacementRenderer != null)
                        {
                            float groundOffset = hit.point.y - groundPlacementRenderer.bounds.min.y;
                            currentGhost.transform.position += Vector3.up * groundOffset;
                        }
                    }

                    ghostScript.SetPlacementValidity(hasWelder && validSurface);
                }
            }
            else
            {
                // Looking at the sky, hide ghost or place at max range
                currentGhost.transform.position = ray.origin + ray.direction * buildRange;
                currentSnappedSocket = null;
                ghostScript.SetPlacementValidity(false);
            }
        }
    }

    private ModuleSocket FindClosestUnoccupiedSocket(Vector3 point)
    {
        Collider[] colliders = Physics.OverlapSphere(point, snapDistance);
        ModuleSocket closest = null;
        float minDistance = float.MaxValue;

        foreach (var col in colliders)
        {
            ModuleSocket socket = col.GetComponent<ModuleSocket>();
            if (socket != null && !socket.isOccupied)
            {
                if (currentGhost != null && socket.transform.IsChildOf(currentGhost.transform)) continue;

                float dist = Vector3.Distance(point, socket.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closest = socket;
                }
            }
        }

        return closest;
    }

    private void HandleBuildingInput()
    {
        if (currentGhost == null) return;

        bool hasWelder = HasEquippedWelder();
        var mouse = Mouse.current;

        if (mouse != null)
        {
            if (mouse.leftButton.wasPressedThisFrame)
            {
                if (!hasWelder)
                {
                    NotifyWelderMissing();
                    return;
                }

                if (!isGhostPlaced && ghostScript.CanBePlaced)
                {
                    isGhostPlaced = true;
                }
            }
            else if (mouse.leftButton.isPressed)
            {
                if (!hasWelder)
                {
                    NotifyWelderMissing();
                    if (isGhostPlaced)
                    {
                        ghostScript.ResetProgress();
                    }
                    return;
                }

                if (isGhostPlaced)
                {
                    ghostScript.ConstructProgress(Time.deltaTime);
                }
            }
            else if (mouse.leftButton.wasReleasedThisFrame)
            {
                if (isGhostPlaced)
                {
                    ghostScript.ResetProgress();
                }
            }

            if (mouse.rightButton.wasPressedThisFrame)
            {
                if (isGhostPlaced)
                {
                    isGhostPlaced = false;
                    ghostScript.ResetProgress();
                }
                else
                {
                    ExitBuildMode();
                }
            }
        }
    }

    public void FinalizeConstruction(GameObject finalPrefab, Vector3 pos, Quaternion rot, BaseModuleData moduleData = null)
    {
        BaseModuleData moduleJustBuilt = moduleData != null ? moduleData : currentModuleToBuild;

        // Remember the socket we snapped to before destroying the ghost
        ModuleSocket snappedSocket = currentSnappedSocket;

        // Destroy ghost so it doesn't linger or interfere
        DestroyGhost();

        // Spawn the final module
        GameObject finalModule = Instantiate(finalPrefab, pos, rot);

        // Mark the base socket we snapped to as connected
        if (snappedSocket != null)
        {
            snappedSocket.SetConnected(true);
        }

        // Connect opposing sockets between newly placed module and existing real modules in the base
        ModuleSocket[] newSockets = finalModule.GetComponentsInChildren<ModuleSocket>();
        ModuleSocket[] allSockets = Object.FindObjectsByType<ModuleSocket>(FindObjectsInactive.Exclude);

        if (snappedSocket != null && newSockets.Length > 0)
        {
            ModuleSocket bestMatchSocket = null;
            float bestDist = float.MaxValue;
            foreach (var s in newSockets)
            {
                float d = Vector3.Distance(s.transform.position, snappedSocket.transform.position);
                if (d < bestDist)
                {
                    bestDist = d;
                    bestMatchSocket = s;
                }
            }
            if (bestMatchSocket != null && bestDist < 1.5f)
            {
                bestMatchSocket.SetConnected(true);
            }
        }

        foreach (var newSocket in newSockets)
        {
            foreach (var otherSocket in allSockets)
            {
                if (otherSocket == newSocket) continue;
                if (otherSocket.transform.IsChildOf(finalModule.transform)) continue;

                // Never connect to a ghost (which might still be in memory before frame ends)
                if (otherSocket.GetComponentInParent<ConstructibleGhost>() != null) continue;

                // Only connect if the sockets are close enough AND facing opposite each other (facing into each other)
                if (Vector3.Distance(newSocket.transform.position, otherSocket.transform.position) < 1.0f)
                {
                    float alignmentDot = Vector3.Dot(newSocket.OutwardDirection, otherSocket.OutwardDirection);
                    if (alignmentDot < -0.5f) // Must be pointing towards each other
                    {
                        newSocket.SetConnected(true);
                        otherSocket.SetConnected(true);
                    }
                }
            }
        }

        isBuildModeActive = false;
        currentModuleToBuild = null;

        // Remove the module from the belt (hotbar) or inventory once placed to prevent inventory clogging
        if (moduleJustBuilt != null)
        {
            RemovePlacedModuleItem(moduleJustBuilt);
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayConstructionSound();
        }
    }

    /// <summary>
    /// Removes 1 module/blueprint item corresponding to the placed module from the hotbar (belt),
    /// or from main inventory if not present on the belt, preventing inventory clutter.
    /// </summary>
    public bool RemovePlacedModuleItem(BaseModuleData moduleData)
    {
        if (moduleData == null) return false;

        var hotbarUI = FindAnyObjectByType<InventoryFramework.HotbarUI>();
        var hotbar = hotbarUI != null ? hotbarUI.hotbar : FindAnyObjectByType<InventoryFramework.Hotbar>();
        bool removed = false;

        // 1. Check the belt (Hotbar) first
        if (hotbar != null && hotbar.slots != null)
        {
            // Check currently selected slot first
            int selectedIdx = hotbarUI != null ? hotbarUI.SelectedIndex : -1;
            if (selectedIdx >= 0 && selectedIdx < hotbar.slots.Count)
            {
                var slot = hotbar.slots[selectedIdx];
                if (slot != null && !slot.IsEmpty && IsMatchingBlueprint(slot.item, moduleData))
                {
                    slot.count--;
                    if (slot.count <= 0)
                    {
                        slot.item = null;
                        slot.count = 0;
                    }
                    removed = true;
                }
            }

            // If not found in selected slot, scan all other belt slots
            if (!removed)
            {
                for (int i = 0; i < hotbar.slots.Count; i++)
                {
                    var slot = hotbar.slots[i];
                    if (slot != null && !slot.IsEmpty && IsMatchingBlueprint(slot.item, moduleData))
                    {
                        slot.count--;
                        if (slot.count <= 0)
                        {
                            slot.item = null;
                            slot.count = 0;
                        }
                        removed = true;
                        break;
                    }
                }
            }
        }

        // 2. If not found on the belt, check player inventory
        if (!removed)
        {
            var inv = inventory != null ? inventory : (GameManager.Instance != null ? GameManager.Instance.playerInventory : FindAnyObjectByType<InventoryFramework.Inventory>());
            if (inv != null && inv.slots != null)
            {
                for (int i = 0; i < inv.slots.Count; i++)
                {
                    var slot = inv.slots[i];
                    if (slot != null && !slot.IsEmpty && IsMatchingBlueprint(slot.item, moduleData))
                    {
                        slot.count--;
                        if (slot.count <= 0)
                        {
                            slot.item = null;
                            slot.count = 0;
                        }
                        removed = true;
                        break;
                    }
                }
            }
        }

        // 3. Refresh UIs immediately
        if (hotbarUI != null)
        {
            hotbarUI.RefreshUI();
        }

        var invUI = FindAnyObjectByType<InventoryFramework.InventoryUI>();
        if (invUI != null)
        {
            invUI.RefreshUI();
        }

        return removed;
    }

    private bool IsMatchingBlueprint(InventoryFramework.Item item, BaseModuleData module)
    {
        if (item == null || module == null) return false;

        if (item is BuildingSystem.BlueprintItem bp)
        {
            if (bp.moduleData == module) return true;
            if (bp.moduleData != null && !string.IsNullOrEmpty(bp.moduleData.moduleName) && bp.moduleData.moduleName == module.moduleName) return true;
        }

        if (!string.IsNullOrEmpty(item.itemName) && !string.IsNullOrEmpty(module.moduleName))
        {
            if (item.itemName.IndexOf(module.moduleName, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        return false;
    }
}
