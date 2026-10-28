using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using InventoryFramework;

/// <summary>
/// Allows the player to place equipment (such as the Scrubber Unit) onto host EquipmentSockets
/// (such as wall mount slots in the Module de Vie) when holding the equipment in their hotbar.
/// Displays visual ghost previews on all nearby available slots, and highlights the targeted slot.
/// </summary>
public class EquipmentPlacementController : MonoBehaviour
{
    [Header("Placement Settings")]
    [Tooltip("Maximum distance in meters from which the player can target and place equipment onto a socket.")]
    public float placeRange = 6.0f;

    [Tooltip("Distance around the player in which nearby unoccupied sockets show their ghost preview markers.")]
    public float nearbyPreviewRadius = 15.0f;

    [Tooltip("Radius around raycast hit point or look ray to snap onto a socket.")]
    public float snapRadius = 2.5f;

    [Header("Current State")]
    public EquipmentSocket currentTargetSocket;
    public Item currentEquipmentItem;

    private Camera playerCamera;
    private HotbarUI hotbarUI;
    private BuilderController builderController;
    private GameObject dynamicGhostInstance;
    private InputSystem_Actions inputActions;

    // Track sockets that are currently showing passive ghost preview
    private readonly List<EquipmentSocket> activeNearbySockets = new List<EquipmentSocket>();

    public bool IsTargetingSocket => currentTargetSocket != null && currentEquipmentItem != null;

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
        ClearTargetSocket();
        HideAllNearbyPreviews();
        inputActions?.Player.Disable();
    }

    private void OnDestroy()
    {
        ClearTargetSocket();
        HideAllNearbyPreviews();
    }

    private void Start()
    {
        EnsureReferences();
    }

    private void EnsureReferences()
    {
        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera == null) playerCamera = Camera.main;
        }

        if (hotbarUI == null)
        {
            hotbarUI = Object.FindAnyObjectByType<HotbarUI>();
        }

        if (builderController == null)
        {
            builderController = GetComponent<BuilderController>() ?? Object.FindAnyObjectByType<BuilderController>();
        }
    }

    private void Update()
    {
        EnsureReferences();

        // 1. Do not process if builder mode (module room building) is active
        if (builderController != null && builderController.isBuildModeActive)
        {
            ClearTargetSocket();
            HideAllNearbyPreviews();
            return;
        }

        // 2. Check if menus or inventory are open
        if (MainMenuController.Instance != null && MainMenuController.Instance.isMenuOpen)
        {
            ClearTargetSocket();
            HideAllNearbyPreviews();
            return;
        }

        if (CraftingSystem.CraftingUIManager.Instance != null && CraftingSystem.CraftingUIManager.Instance.IsOpen)
        {
            ClearTargetSocket();
            HideAllNearbyPreviews();
            return;
        }

        PlayerBaseController pbc = GetComponent<PlayerBaseController>();
        if (pbc != null && (pbc.isInventoryOpen || pbc.isBlueprintOpen))
        {
            ClearTargetSocket();
            HideAllNearbyPreviews();
            return;
        }

        // 3. Check selected item in hotbar
        Item selectedItem = hotbarUI != null ? hotbarUI.GetSelectedItem() : null;
        if (!IsEquipmentItem(selectedItem, out GameObject equipmentPrefab, out string socketType))
        {
            ClearTargetSocket();
            HideAllNearbyPreviews();
            currentEquipmentItem = null;
            return;
        }

        currentEquipmentItem = selectedItem;

        // 4. Update visual ghost markers for all nearby available sockets
        UpdateNearbyPreviews(socketType);

        // 5. Detect best targeted socket using raycast and look cone with hysteresis
        if (playerCamera == null) return;

        EquipmentSocket foundSocket = DetectTargetSocket(socketType);

        // 6. Update Target Socket and Ghost Preview
        if (foundSocket != currentTargetSocket)
        {
            if (currentTargetSocket != null && !activeNearbySockets.Contains(currentTargetSocket))
            {
                currentTargetSocket.ShowGhostPreview(false);
            }

            currentTargetSocket = foundSocket;

            if (currentTargetSocket != null)
            {
                ShowGhostOnSocket(currentTargetSocket, equipmentPrefab);
            }
        }
        else if (currentTargetSocket != null)
        {
            // Ensure preview stays active while focused
            currentTargetSocket.ShowGhostPreview(true);
        }

        // 7. Handle Placement Input
        if (currentTargetSocket != null)
        {
            bool placeClicked = (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                || (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                || (inputActions != null && (inputActions.Player.Attack.WasPressedThisFrame() || inputActions.Player.Interact.WasPressedThisFrame()));

            if (placeClicked)
            {
                PlaceEquipment(currentTargetSocket, selectedItem, equipmentPrefab);
            }
        }
    }

    private EquipmentSocket DetectTargetSocket(string desiredType)
    {
        Vector3 camPos = playerCamera.transform.position;
        Vector3 camFwd = playerCamera.transform.forward;
        Ray ray = new Ray(camPos, camFwd);
        int layerMask = ~LayerMask.GetMask("Ignore Raycast");

        // First: If we already have a target, apply hysteresis (keep it unless player moves away or looks far)
        if (currentTargetSocket != null && !currentTargetSocket.isOccupied && currentTargetSocket.isHostSocket)
        {
            float dist = Vector3.Distance(camPos, currentTargetSocket.transform.position);
            if (dist <= placeRange + 1.0f)
            {
                Vector3 toSocket = (currentTargetSocket.transform.position - camPos).normalized;
                float dot = Vector3.Dot(camFwd, toSocket);
                // Keep lock if looking within ~40 degrees
                if (dot > 0.75f)
                {
                    return currentTargetSocket;
                }
            }
        }

        // Second: Direct raycast hit on EquipmentSocket or wall
        if (Physics.Raycast(ray, out RaycastHit hit, placeRange, layerMask, QueryTriggerInteraction.Collide))
        {
            EquipmentSocket direct = hit.collider.GetComponentInParent<EquipmentSocket>();
            if (direct != null && direct.isHostSocket && !direct.isOccupied && direct.CanConnectType(desiredType))
            {
                return direct;
            }

            EquipmentSocket nearHit = EquipmentSocket.FindClosestUnoccupiedHostSocket(hit.point, snapRadius, desiredType);
            if (nearHit != null && Vector3.Distance(camPos, nearHit.transform.position) <= placeRange)
            {
                return nearHit;
            }
        }

        // Third: Best socket in view cone in front of camera
        EquipmentSocket[] allSockets = Object.FindObjectsByType<EquipmentSocket>(FindObjectsInactive.Exclude);
        EquipmentSocket bestSocket = null;
        float bestScore = -1f;

        foreach (var s in allSockets)
        {
            if (!s.isHostSocket || s.isOccupied || !s.CanConnectType(desiredType)) continue;

            float dist = Vector3.Distance(camPos, s.transform.position);
            if (dist > placeRange) continue;

            Vector3 toSocket = (s.transform.position - camPos).normalized;
            float lookDot = Vector3.Dot(camFwd, toSocket);
            if (lookDot < 0.65f) continue; // Must be roughly in front of player

            // Player must be facing the front of the socket
            Vector3 toCam = (camPos - s.transform.position).normalized;
            if (Vector3.Dot(s.OutwardDirection, toCam) < -0.3f) continue;

            // Score prefers center of screen and closer distance
            float score = lookDot * 2.0f - (dist / placeRange);
            if (score > bestScore)
            {
                bestScore = score;
                bestSocket = s;
            }
        }

        return bestSocket;
    }

    private void UpdateNearbyPreviews(string socketType)
    {
        Vector3 playerPos = transform.position;
        EquipmentSocket[] allSockets = Object.FindObjectsByType<EquipmentSocket>(FindObjectsInactive.Exclude);

        var currentNearby = new HashSet<EquipmentSocket>();

        foreach (var s in allSockets)
        {
            if (!s.isHostSocket || s.isOccupied) continue;
            if (!s.CanConnectType(socketType)) continue;

            float dist = Vector3.Distance(playerPos, s.transform.position);
            if (dist <= nearbyPreviewRadius)
            {
                currentNearby.Add(s);
                s.ShowGhostPreview(true);
            }
        }

        // Hide previews for sockets that went out of range
        for (int i = activeNearbySockets.Count - 1; i >= 0; i--)
        {
            var oldSock = activeNearbySockets[i];
            if (oldSock != null && !currentNearby.Contains(oldSock) && oldSock != currentTargetSocket)
            {
                oldSock.ShowGhostPreview(false);
            }
        }

        activeNearbySockets.Clear();
        activeNearbySockets.AddRange(currentNearby);
    }

    private void HideAllNearbyPreviews()
    {
        foreach (var s in activeNearbySockets)
        {
            if (s != null && !s.isOccupied)
            {
                s.ShowGhostPreview(false);
            }
        }
        activeNearbySockets.Clear();
    }

    public bool IsEquipmentItem(Item item, out GameObject equipmentPrefab, out string socketType)
    {
        equipmentPrefab = null;
        socketType = null;
        if (item == null) return false;

        // Check if item model has EquipmentSocket
        if (item.model != null)
        {
            var eqSocket = item.model.GetComponentInChildren<EquipmentSocket>();
            if (eqSocket != null && !eqSocket.isHostSocket)
            {
                equipmentPrefab = item.model;
                socketType = eqSocket.socketType;
                return true;
            }
        }

        // Fallback for Scrubber items
        if (item.id == 60 || item.id == 43 ||
            (!string.IsNullOrEmpty(item.itemName) && item.itemName.IndexOf("Scrubber", System.StringComparison.OrdinalIgnoreCase) >= 0))
        {
            equipmentPrefab = item.model;
            if (equipmentPrefab == null)
            {
#if UNITY_EDITOR
                equipmentPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Equipment/ScrubberUnitPrefab.prefab");
#endif
            }
            socketType = "Scrubber";
            return true;
        }

        return false;
    }

    private void ShowGhostOnSocket(EquipmentSocket socket, GameObject equipmentPrefab)
    {
        if (socket == null) return;

        if (socket.GhostPreview != null)
        {
            socket.ShowGhostPreview(true);
        }
        else
        {
            GameObject ghostPrefabToUse = null;
#if UNITY_EDITOR
            ghostPrefabToUse = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Equipment/ScrubberUnitGhostPrefab.prefab");
#endif
            if (ghostPrefabToUse == null) ghostPrefabToUse = equipmentPrefab;

            if (ghostPrefabToUse != null)
            {
                dynamicGhostInstance = Instantiate(ghostPrefabToUse);
                socket.SnapEquipment(dynamicGhostInstance);

                foreach (var col in dynamicGhostInstance.GetComponentsInChildren<Collider>())
                {
                    col.enabled = false;
                }
            }
        }
    }

    private void ClearTargetSocket()
    {
        if (currentTargetSocket != null)
        {
            if (!activeNearbySockets.Contains(currentTargetSocket))
            {
                currentTargetSocket.ShowGhostPreview(false);
            }
            currentTargetSocket = null;
        }

        if (dynamicGhostInstance != null)
        {
            if (Application.isPlaying) Destroy(dynamicGhostInstance);
            else DestroyImmediate(dynamicGhostInstance);
            dynamicGhostInstance = null;
        }
    }

    private void PlaceEquipment(EquipmentSocket socket, Item item, GameObject prefab)
    {
        if (socket == null || prefab == null || item == null) return;

        socket.ShowGhostPreview(false);
        if (dynamicGhostInstance != null)
        {
            Destroy(dynamicGhostInstance);
            dynamicGhostInstance = null;
        }

        // Spawn final equipment
        GameObject placedEquipment = Instantiate(prefab);
        socket.SnapEquipment(placedEquipment);
        socket.SetOccupied(true, placedEquipment);

        var scrubber = placedEquipment.GetComponent<ScrubberUnit>();
        if (scrubber == null)
        {
            scrubber = placedEquipment.AddComponent<ScrubberUnit>();
        }
        scrubber.Initialize(socket, item);

        // Consume 1 item from hotbar
        ConsumeSelectedItem(item);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayConstructionSound();
        }

        if (MainMenuController.Instance != null)
        {
            MainMenuController.Instance.ShowNotification($"{item.itemName} installé avec succès !");
        }

        currentTargetSocket = null;
    }

    private void ConsumeSelectedItem(Item item)
    {
        var hotbar = hotbarUI != null ? hotbarUI.hotbar : Object.FindAnyObjectByType<Hotbar>();
        int selectedIdx = hotbarUI != null ? hotbarUI.SelectedIndex : -1;
        bool consumed = false;

        if (hotbar != null && hotbar.slots != null && selectedIdx >= 0 && selectedIdx < hotbar.slots.Count)
        {
            var slot = hotbar.slots[selectedIdx];
            if (slot != null && !slot.IsEmpty && (slot.item == item || slot.item.id == item.id || slot.item.itemName == item.itemName))
            {
                slot.count--;
                if (slot.count <= 0)
                {
                    slot.item = null;
                    slot.count = 0;
                }
                consumed = true;
            }
        }

        if (!consumed && hotbar != null)
        {
            consumed = hotbar.ConsumeItem(item, 1);
        }

        if (!consumed && GameManager.Instance != null)
        {
            GameManager.Instance.ConsumePlayerItem(item, 1);
        }

        if (hotbarUI != null) hotbarUI.RefreshUI();
        var invUI = Object.FindAnyObjectByType<InventoryUI>();
        if (invUI != null) invUI.RefreshUI();
    }

    private void OnGUI()
    {
        if (currentTargetSocket != null && currentEquipmentItem != null)
        {
            float centerX = Screen.width * 0.5f;
            float centerY = Screen.height * 0.5f;

            GUIStyle style = new GUIStyle(GUI.skin.box);
            style.alignment = TextAnchor.MiddleCenter;
            style.fontSize = 16;
            style.normal.textColor = Color.cyan;
            style.fontStyle = FontStyle.Bold;

            string prompt = $"[Clic Gauche] ou [E] : Installer {currentEquipmentItem.itemName}";
            GUI.Box(new Rect(centerX - 180, centerY + 30, 360, 36), prompt, style);
        }
    }
}

