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

    [Header("Build Settings")]
    public float buildRange = 10f;
    public float snapDistance = 2f; // Distance from raycast point to socket to trigger a snap

    [Header("Current Build State")]
    public BaseModuleData currentModuleToBuild;
    public bool isBuildModeActive = false;
    public bool isGhostPlaced = false;

    [Header("Ghost Rotation")]
    public float ghostRotationX = 0f;
    public float targetGhostRotationX = 0f;
    public float rotationSpeed = 15f;

    private GameObject currentGhost;
    private ConstructibleGhost ghostScript;
    private ModuleSocket currentSnappedSocket;
    private InputSystem_Actions inputActions;

    private void Awake()
    {
        inputActions = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
    }

    private void OnDisable()
    {
        inputActions.Player.Disable();
    }

    private void Start()
    {
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();

        if (inventory == null && GameManager.Instance != null)
            inventory = GameManager.Instance.playerInventory;
    }

    private void Update()
    {
        if (!isBuildModeActive || currentModuleToBuild == null)
        {
            if (currentGhost != null) DestroyGhost();
            return;
        }

        if (currentGhost == null)
        {
            CreateGhost();
        }

        UpdateGhostPlacement();
        HandleBuildingInput();
    }

    public void EnterBuildMode(BaseModuleData moduleData)
    {
        currentModuleToBuild = moduleData;
        isBuildModeActive = true;
        isGhostPlaced = false;
        ghostRotationX = 0f; // Reset rotation
        targetGhostRotationX = 0f;
    }

    public void ExitBuildMode()
    {
        isBuildModeActive = false;
        currentModuleToBuild = null;
        DestroyGhost();
    }

    private void CreateGhost()
    {
        if (currentModuleToBuild.ghostPrefab == null) return;

        currentGhost = Instantiate(currentModuleToBuild.ghostPrefab);
        ghostScript = currentGhost.GetComponent<ConstructibleGhost>();

        if (ghostScript == null)
        {
            ghostScript = currentGhost.AddComponent<ConstructibleGhost>();
        }

        ghostScript.Initialize(currentModuleToBuild, this);
    }

    private void DestroyGhost()
    {
        if (currentGhost != null)
        {
            Destroy(currentGhost);
            currentGhost = null;
            ghostScript = null;
            currentSnappedSocket = null;
            isGhostPlaced = false;
        }
    }

    private void UpdateGhostPlacement()
    {
        if (currentGhost == null || ghostScript.IsConstructing) return;

        if (!isGhostPlaced)
        {
            // Cast a ray from the center of the screen
            Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));

            if (Physics.Raycast(ray, out RaycastHit hit, buildRange))
            {
                // First, check if we hit near an existing unoccupied socket
                ModuleSocket closestSocket = FindClosestUnoccupiedSocket(hit.point);

                if (closestSocket != null)
                {
                currentSnappedSocket = closestSocket;

                // Find a matching socket on the ghost to align with
                ModuleSocket[] ghostSockets = currentGhost.GetComponentsInChildren<ModuleSocket>();
                ModuleSocket ghostConnectingSocket = null;

                // For simplicity, just grab the first one.
                // A more advanced system would let the player cycle through available sockets on the ghost.
                if (ghostSockets.Length > 0)
                {
                    ghostConnectingSocket = ghostSockets[0];
                }

                if (ghostConnectingSocket != null)
                {
                    // Rotate the ghost so its socket's outward direction is exactly opposite to the base's socket
                    Quaternion targetRotation = Quaternion.LookRotation(-closestSocket.OutwardDirection, closestSocket.transform.up);

                    // We need to figure out the rotation offset between the ghost's root and its connecting socket
                    Quaternion rotationOffset = Quaternion.Inverse(ghostConnectingSocket.transform.localRotation);
                    currentGhost.transform.rotation = targetRotation * rotationOffset;

                    // Position the ghost so its socket exactly overlaps the base's socket
                    Vector3 positionOffset = currentGhost.transform.position - ghostConnectingSocket.transform.position;
                    currentGhost.transform.position = closestSocket.transform.position + positionOffset;
                }
                else
                {
                    // Fallback if ghost has no sockets defined (should not happen normally)
                    currentGhost.transform.position = closestSocket.transform.position;
                    currentGhost.transform.rotation = Quaternion.LookRotation(closestSocket.OutwardDirection, closestSocket.transform.up);
                }

                // Apply manual X-axis rotation on top of snapped rotation
                currentGhost.transform.Rotate(Vector3.right, ghostRotationX, Space.Self);

                ghostScript.SetPlacementValidity(true);
            }
            else
            {
                // Free placement on terrain/surface
                currentSnappedSocket = null;
                currentGhost.transform.position = hit.point;
                // Keep the module upright, optionally orient it based on player facing
                Vector3 playerForward = transform.forward;
                playerForward.y = 0; // Keep horizontal
                if (playerForward.sqrMagnitude > 0.01f)
                {
                    currentGhost.transform.rotation = Quaternion.LookRotation(playerForward, Vector3.up);
                }
                else
                {
                    currentGhost.transform.rotation = Quaternion.identity;
                }

                // Apply manual X-axis rotation on top of free placement rotation
                currentGhost.transform.Rotate(Vector3.right, ghostRotationX, Space.Self);

                    // Check if placement is valid (e.g. slope not too steep, not intersecting)
                    // For now, we assume simple validity on terrain
                    ghostScript.SetPlacementValidity(hit.normal.y > 0.8f);
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

        // Handle rotation input using InputSystem's Action Map (Rotation)
        // Read the continuous 1D axis value
        var rotationInput = inputActions.Player.Rotation.ReadValue<float>();
        if (Mathf.Abs(rotationInput) > 0.01f)
        {
            // Normalize the scroll value to make the rotation speed manageable.
            // We use the actual value scaled down, so smooth scroll devices aren't too fast.
            // Typical notch is 120, so dividing by 120 normalizes a notch to 1.
            targetGhostRotationX += (rotationInput / 120f) * rotationSpeed;
        }

        // Smoothly interpolate the actual rotation towards the target
        ghostRotationX = Mathf.Lerp(ghostRotationX, targetGhostRotationX, Time.deltaTime * 10f);

        if (!ghostScript.CanBePlaced) return;

        var mouse = Mouse.current;
        if (mouse != null)
        {
            if (mouse.leftButton.wasPressedThisFrame)
            {
                if (!isGhostPlaced && ghostScript.CanBePlaced)
                {
                    isGhostPlaced = true;
                }
            }
            else if (mouse.leftButton.isPressed)
            {
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

    public void FinalizeConstruction(GameObject finalPrefab, Vector3 pos, Quaternion rot)
    {
        // Spawn the final module
        GameObject finalModule = Instantiate(finalPrefab, pos, rot);

        // Mark the socket as occupied if we used one, and also mark the newly spawned opposing socket
        if (currentSnappedSocket != null)
        {
            currentSnappedSocket.isOccupied = true;

            // Find the socket on the newly created module that connects back to the base
            ModuleSocket[] newSockets = finalModule.GetComponentsInChildren<ModuleSocket>();
            foreach (var socket in newSockets)
            {
                // If a socket on the new module is very close to the base socket, it's the connection point
                if (Vector3.Distance(socket.transform.position, currentSnappedSocket.transform.position) < 0.1f)
                {
                    socket.isOccupied = true;
                    break;
                }
            }
        }

        // The ghost destroys itself, reset state
        DestroyGhost();
        isBuildModeActive = false;
        currentModuleToBuild = null;
    }
}
