using UnityEngine;
using UnityEngine.InputSystem;
using InventoryFramework; // Access HotbarUI, Item

public class PlayerBaseController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 9f;
    public float gravity = -15f;
    public float jumpHeight = 1.2f;

    [Header("Look & Camera")]
    public Transform cameraTransform;
    public float mouseSensitivity = 1.5f;
    public float minPitch = -80f;
    public float maxPitch = 80f;

    [Header("Interaction")]
    public float interactRange = 6.5f;
    public bool isNearHubScreen = false;

    [Header("Mining & Starter Tools")]
    public Item starterTool;
    public Item starterWelder;
    public Item starterIce;
    public Item starterOxygenBottle;

    [Header("UI & Inventory")]
    public GameObject inventoryUI;
    public bool isInventoryOpen = false;
    public GameObject blueprintUI;
    public bool isBlueprintOpen = false;

    [Header("State")]
    public bool canMove = true;
    public bool isFirstPerson = true;

    private CharacterController characterController;
    private BuilderController builderController;
    private float verticalVelocity = 0f;
    private float cameraPitch = 0f;
    private Vector3 defaultCameraLocalPos = new Vector3(0f, 0.75f, 0f);
    private IInteractable currentInteractable;
    private MineableResource currentMineable;

    [Header ("Main Menu")]
    public GameObject mainMenuUI;

    public float CameraPitch
    {
        get => cameraPitch;
        set => cameraPitch = value;
    }

    public Vector3 DefaultCameraLocalPos => defaultCameraLocalPos;

    private InputSystem_Actions inputActions;

    private void Awake()
    {
        Debug.Log($"[PlayerBaseController] Awake() - Position initiale du joueur : {transform.position}");
        characterController = GetComponent<CharacterController>();
        builderController = GetComponent<BuilderController>();
        inputActions = new InputSystem_Actions();
        if (cameraTransform == null)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null) cameraTransform = cam.transform;
        }
        if (cameraTransform != null)
        {
            defaultCameraLocalPos = cameraTransform.localPosition;
        }
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
        // When enabled, force sync physics transforms so CharacterController recognizes the position
        Physics.SyncTransforms();
        Debug.Log($"[PlayerBaseController] OnEnable() - Position : {transform.position}");
    }

    private void OnDisable()
    {
        inputActions.Player.Disable();
    }

    private void Start()
    {
        // Initial sync of physics transforms
        Physics.SyncTransforms();
        verticalVelocity = 0f;

        // Sync world physics gravity (e.g. Moon ~ -1.62 m/s² vs Earth ~ -9.81 m/s²)
        if (gravity > -10f)
        {
            Physics.gravity = new Vector3(0f, -1.62f, 0f);
        }
        else
        {
            Physics.gravity = new Vector3(0f, -9.81f, 0f);
        }

        // Lock cursor for gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Ensure inventory is closed at start
        if (inventoryUI != null)
        {
            inventoryUI.SetActive(false);
        }
        if (blueprintUI != null)
        {
            blueprintUI.SetActive(false);
        }
        if(mainMenuUI != null)
        {
            mainMenuUI.SetActive(false);
        }

        EnsureStarterTools();
    }

    private void EnsureStarterTools()
    {
        if (starterTool == null)
        {
            #if UNITY_EDITOR
            starterTool = UnityEditor.AssetDatabase.LoadAssetAtPath<Item>("Assets/Items/SonicFracturer.asset");
            #endif
        }

        if (starterWelder == null)
        {
            #if UNITY_EDITOR
            starterWelder = UnityEditor.AssetDatabase.LoadAssetAtPath<Item>("Assets/Items/Welder.asset");
            #endif
        }

        if (starterIce == null)
        {
            #if UNITY_EDITOR
            starterIce = UnityEditor.AssetDatabase.LoadAssetAtPath<Item>("Assets/Items/IceResource.asset");
            #endif
        }

        if (starterOxygenBottle == null)
        {
            #if UNITY_EDITOR
            starterOxygenBottle = UnityEditor.AssetDatabase.LoadAssetAtPath<Item>("Assets/Items/OxygenBottle.asset");
            #endif
        }

        HotbarUI hotbarUI = FindAnyObjectByType<HotbarUI>();
        Hotbar hotbar = hotbarUI != null ? hotbarUI.hotbar : FindAnyObjectByType<Hotbar>();
        Inventory inv = FindAnyObjectByType<Inventory>();

        // 1. Ensure starter mining tool (Sonic Fracturer)
        if (starterTool != null)
        {
            bool alreadyHasTool = false;
            if (hotbar != null && hotbar.HasItem(starterTool, 1)) alreadyHasTool = true;
            if (inv != null && inv.HasItem(starterTool, 1)) alreadyHasTool = true;

            if (!alreadyHasTool && hotbar != null)
            {
                var slot0 = hotbar.GetSlot(0);
                if (slot0 != null && slot0.IsEmpty)
                {
                    slot0.item = starterTool;
                    slot0.count = 1;
                }
                else
                {
                    hotbar.AddItem(starterTool, 1);
                }
            }
        }

        // 2. Ensure starter welding equipment (Welder)
        if (starterWelder != null)
        {
            bool alreadyHasWelder = false;
            if (hotbar != null && hotbar.HasItem(starterWelder, 1)) alreadyHasWelder = true;
            if (inv != null && inv.HasItem(starterWelder, 1)) alreadyHasWelder = true;

            if (!alreadyHasWelder && hotbar != null)
            {
                var slot1 = hotbar.GetSlot(1);
                if (slot1 != null && slot1.IsEmpty)
                {
                    slot1.item = starterWelder;
                    slot1.count = 1;
                }
                else
                {
                    if (!hotbar.AddItem(starterWelder, 1) && inv != null)
                    {
                        inv.AddItem(starterWelder, 1);
                    }
                }
            }
        }

        // 3. Ensure starter Ice (at least 10 units)
        if (starterIce != null && inv != null)
        {
            int currentIce = inv.GetItemCount(starterIce);
            if (hotbar != null) currentIce += hotbar.GetItemCount(starterIce);
            if (currentIce < 10)
            {
                inv.AddItem(starterIce, 10 - currentIce);
            }
        }

        // 4. Ensure 4 oxygen bottles in the 4 oxygen slots
        if (starterOxygenBottle != null && inv != null && inv.oxygenSlots != null)
        {
            for (int i = 0; i < inv.oxygenSlots.Count; i++)
            {
                var s = inv.oxygenSlots[i];
                if (s != null && s.IsEmpty)
                {
                    s.item = starterOxygenBottle;
                    s.count = 1;
                    // Initial oxygen values: 100%, 75%, 50%, 0%
                    s.currentOxygen = (i == 0) ? 100f : (i == 1) ? 75f : (i == 2) ? 50f : 0f;
                }
            }
        }

        if (hotbarUI != null) hotbarUI.RefreshUI();
    }

    private void Update()
    {
        
        HandleInventoryInput();
        HandleBlueprintInput();

        // Don't process player movement when main menu, pause, or Hub UI/Inventory is open
        if (MainMenuController.Instance != null && MainMenuController.Instance.isMenuOpen)
        {
            verticalVelocity = 0f;
            return;
        }

        if (EarthBaseController.Instance != null && EarthBaseController.Instance.isHubUIOpen)
        {
            verticalVelocity = 0f;
            return;
        }

        if (CraftingSystem.CraftingUIManager.Instance != null && CraftingSystem.CraftingUIManager.Instance.IsOpen)
        {
            verticalVelocity = 0f;
            return;
        }

        if (isInventoryOpen || isBlueprintOpen)
        {
            verticalVelocity = 0f;
            return;
        }

        if (!canMove) return;

        HandleLook();
        HandleMovement();
        CheckInteraction();
        HandleBuildMode();
        HandleInteractionInput();
        HandleMainMenu();
    }

    private void HandleBuildMode()
    {
        if (builderController == null) return;

        HotbarUI hotbarUI = FindAnyObjectByType<HotbarUI>();
        if (hotbarUI != null)
        {
            Item selectedItem = hotbarUI.GetSelectedItem();
            if (selectedItem != null && selectedItem is BuildingSystem.BlueprintItem blueprintItem)
            {
                if (builderController.currentModuleToBuild != blueprintItem.moduleData)
                {
                    builderController.EnterBuildMode(blueprintItem.moduleData);
                }
            }
            else if (selectedItem != null && builderController.IsWelder(selectedItem))
            {
                // If holding Welder directly and build mode was active with a module, keep build mode active.
                if (builderController.currentModuleToBuild == null && builderController.isBuildModeActive)
                {
                    builderController.ExitBuildMode();
                }
            }
            else
            {
                if (builderController.isBuildModeActive)
                {
                    builderController.ExitBuildMode();
                }
            }
        }
        else
        {
             if (builderController.isBuildModeActive)
             {
                 builderController.ExitBuildMode();
             }
        }
    }

    private void HandleInventoryInput()
    {
        if (inputActions.Player.Inventory.WasPressedThisFrame() || (Keyboard.current != null && Keyboard.current.iKey.wasPressedThisFrame))
        {
            // Do not toggle inventory if we are in other UI modes
            if (MainMenuController.Instance != null && MainMenuController.Instance.isMenuOpen) return;
            if (EarthBaseController.Instance != null && EarthBaseController.Instance.isHubUIOpen) return;
            if (CraftingSystem.CraftingUIManager.Instance != null && CraftingSystem.CraftingUIManager.Instance.IsOpen) return;

            isInventoryOpen = !isInventoryOpen;

            if (inventoryUI != null)
            {
                inventoryUI.SetActive(isInventoryOpen);
            }

            if (isInventoryOpen || isBlueprintOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    private void HandleBlueprintInput()
    {
        if (inputActions.Player.Construction.WasPressedThisFrame())
        {
            // Do not toggle blueprint UI if we are in other UI modes
            if (MainMenuController.Instance != null && MainMenuController.Instance.isMenuOpen) return;
            if (EarthBaseController.Instance != null && EarthBaseController.Instance.isHubUIOpen) return;
            if (CraftingSystem.CraftingUIManager.Instance != null && CraftingSystem.CraftingUIManager.Instance.IsOpen) return;

            isBlueprintOpen = !isBlueprintOpen;

            if (blueprintUI != null)
            {
                blueprintUI.SetActive(isBlueprintOpen);
            }

            if (isInventoryOpen || isBlueprintOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    private void HandleLook()
    {
        if (cameraTransform == null) return;

        Vector2 mouseDelta = inputActions.Player.Look.ReadValue<Vector2>() * (mouseSensitivity * 0.1f);

        // Yaw (Player horizontal rotation)
        transform.Rotate(Vector3.up * mouseDelta.x);

        // Pitch (Camera vertical rotation)
        cameraPitch -= mouseDelta.y;
        cameraPitch = Mathf.Clamp(cameraPitch, minPitch, maxPitch);
        cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
    }

    private void HandleMovement()
    {
        if (characterController == null) return;

        Vector2 moveInput = inputActions.Player.Move.ReadValue<Vector2>();
        float moveX = moveInput.x;
        float moveZ = moveInput.y;

        Vector3 moveDir = (transform.forward * moveZ + transform.right * moveX).normalized;

        bool isSprinting = inputActions.Player.Sprint.IsPressed();
        float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;

        float dt = Time.deltaTime;
        if (dt > 0.1f) dt = 0.1f; // Clamp to avoid large movement spikes (e.g., initial frame drops)

        if (characterController.isGrounded && verticalVelocity < 0)
        {
            verticalVelocity = -2f; // Slight negative force to keep grounded reliably
        }

        if (characterController.isGrounded && inputActions.Player.Jump.WasPressedThisFrame())
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        verticalVelocity += gravity * dt;
        if (verticalVelocity < -50f) verticalVelocity = -50f; // Terminal velocity

        Vector3 finalMovement = (moveDir * currentSpeed) + (Vector3.up * verticalVelocity);
        characterController.Move(finalMovement * dt);
    }

    private void CheckInteraction()
    {
        // 1. Raycast for IInteractable and MineableResource
        currentInteractable = null;
        currentMineable = null;
        if (cameraTransform != null)
        {
            int layerMask = ~LayerMask.GetMask("Ignore Raycast");
            if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out RaycastHit hit, interactRange, layerMask, QueryTriggerInteraction.Collide))
            {
                currentInteractable = hit.collider.GetComponentInParent<IInteractable>();
                currentMineable = hit.collider.GetComponentInParent<MineableResource>();
            }
        }

        // 2. Proximity for EarthBaseController Hub
        if (EarthBaseController.Instance == null) return;

        float distDesk = EarthBaseController.Instance.hubScreenTransform != null
            ? Vector3.Distance(transform.position, EarthBaseController.Instance.hubScreenTransform.position)
            : float.MaxValue;

        float distScreen = EarthBaseController.Instance.hubCameraTarget != null
            ? Vector3.Distance(transform.position, EarthBaseController.Instance.hubCameraTarget.position)
            : float.MaxValue;

        isNearHubScreen = Mathf.Min(distDesk, distScreen) <= interactRange;
    }

    private void HandleInteractionInput()
    {
        if (inputActions.Player.Interact.WasPressedThisFrame() || (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame))
        {
            if (currentInteractable != null)
            {
                currentInteractable.Interact();
            }
        }

        bool attackPressed = inputActions.Player.Attack.WasPressedThisFrame() || 
            (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);

        if (attackPressed)
        {
            // Bypass mining logic if builder mode is active to prevent conflicts with building input
            if (builderController != null && builderController.isBuildModeActive) return;

            // Direct raycast check at click time if currentMineable was null
            MineableResource targetMineable = currentMineable;
            if (targetMineable == null && cameraTransform != null)
            {
                int layerMask = ~LayerMask.GetMask("Ignore Raycast");
                if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out RaycastHit hit, interactRange, layerMask, QueryTriggerInteraction.Collide))
                {
                    targetMineable = hit.collider.GetComponentInParent<MineableResource>();
                }
            }

            if (targetMineable != null)
            {
                HotbarUI hotbarUI = FindAnyObjectByType<HotbarUI>();
                Item selectedItem = hotbarUI != null ? hotbarUI.GetSelectedItem() : null;

                string required = targetMineable.requiredToolName?.Trim();
                bool hasRequiredTool = false;

                // 1. Check currently selected item
                if (selectedItem != null && (string.IsNullOrEmpty(required) || string.Equals(selectedItem.itemName?.Trim(), required, System.StringComparison.OrdinalIgnoreCase)))
                {
                    hasRequiredTool = true;
                }

                // 2. If not selected, check if player has the tool in another hotbar slot
                if (!hasRequiredTool && !string.IsNullOrEmpty(required))
                {
                    Hotbar playerHotbar = hotbarUI != null ? hotbarUI.hotbar : null;
                    if (playerHotbar != null)
                    {
                        for (int i = 0; i < playerHotbar.size; i++)
                        {
                            var slot = playerHotbar.GetSlot(i);
                            if (slot != null && !slot.IsEmpty && string.Equals(slot.item.itemName?.Trim(), required, System.StringComparison.OrdinalIgnoreCase))
                            {
                                hotbarUI.SelectSlot(i);
                                selectedItem = slot.item;
                                hasRequiredTool = true;
                                break;
                            }
                        }
                    }
                }

                // 3. If still not found, check inventory
                if (!hasRequiredTool && !string.IsNullOrEmpty(required))
                {
                    Inventory inv = FindAnyObjectByType<Inventory>();
                    if (inv != null && inv.HasItem(starterTool, 1))
                    {
                        hasRequiredTool = true;
                    }
                    else if (GameManager.Instance != null && GameManager.Instance.playerInventory != null)
                    {
                        foreach (var slot in GameManager.Instance.playerInventory.slots)
                        {
                            if (!slot.IsEmpty && string.Equals(slot.item.itemName?.Trim(), required, System.StringComparison.OrdinalIgnoreCase))
                            {
                                hasRequiredTool = true;
                                break;
                            }
                        }
                    }
                }

                // 4. Perform mining or provide clear feedback
                if (hasRequiredTool)
                {
                    Debug.Log($"[Mining] Récolte de '{targetMineable.resourceName}' avec '{required}'...");
                    targetMineable.Mine();
                }
                else
                {
                    string toolName = !string.IsNullOrEmpty(required) ? required : "Sonic Fracturer";
                    Debug.LogWarning($"[Mining] Outil requis manquant pour récolter : {toolName}");
                    if (MainMenuController.Instance != null)
                    {
                        MainMenuController.Instance.ShowNotification($"Outil requis : {toolName}");
                    }
                }
            }
        }
    }

    private void OnGUI()
    {
        if (isInventoryOpen || isBlueprintOpen || (MainMenuController.Instance != null && MainMenuController.Instance.isMenuOpen))
            return;

        float centerX = Screen.width / 2f;
        float centerY = Screen.height / 2f;

        // Draw crosshair center dot
        bool isBuilding = builderController != null && builderController.isBuildModeActive && builderController.currentModuleToBuild != null;
        Color dotColor = isBuilding ? new Color(0.3f, 1f, 0.5f, 0.9f) : (currentMineable != null ? new Color(0f, 0.9f, 1f, 0.9f) : new Color(1f, 1f, 1f, 0.4f));
        GUI.color = dotColor;
        GUI.Box(new Rect(centerX - 3, centerY - 3, 6, 6), GUIContent.none);

        // Interaction prompt
        if (isBuilding)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.alignment = TextAnchor.MiddleCenter;
            style.fontSize = 15;

            if (!builderController.HasEquippedWelder())
            {
                style.normal.textColor = new Color(1f, 0.35f, 0.35f, 1f);
                string msg = builderController.HasWelderInInventory()
                    ? "[!] Welder requis : Équipez le Welder dans votre barre rapide !"
                    : "[!] Welder requis : Équipement de soudure manquant pour construire !";
                GUI.Label(new Rect(centerX - 250, centerY + 18, 500, 30), msg, style);
            }
            else
            {
                style.normal.textColor = new Color(0.3f, 1f, 0.5f, 1f);
                string prompt = builderController.isGhostPlaced
                    ? "[Clic Gauche Maintenu] Souder et Construire  |  [Clic Droit] Déplacer"
                    : $"[Clic Gauche] Poser : {builderController.currentModuleToBuild.moduleName}  |  [Molette] Tourner  |  [Clic Droit] Annuler";
                GUI.Label(new Rect(centerX - 250, centerY + 18, 500, 30), prompt, style);
            }
        }
        else if (currentMineable != null)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.alignment = TextAnchor.MiddleCenter;
            style.fontSize = 15;
            style.normal.textColor = Color.white;

            string resName = !string.IsNullOrEmpty(currentMineable.resourceName) ? currentMineable.resourceName : "Ressource";
            string prompt = $"[Clic Gauche] Récolter : {resName}";
            GUI.Label(new Rect(centerX - 150, centerY + 18, 300, 30), prompt, style);
        }
        else if (currentInteractable != null)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.alignment = TextAnchor.MiddleCenter;
            style.fontSize = 15;
            style.normal.textColor = Color.white;

            string prompt = (currentInteractable is IPromptInteractable promptInteractable)
                ? $"[E] {promptInteractable.GetInteractionPrompt()}"
                : "[E] Interagir";
            GUI.Label(new Rect(centerX - 150, centerY + 18, 300, 30), prompt, style);
        }
    }

    private void HandleMainMenu()
    {
        // Only process the new Input System "Menu" action (e.g. gamepad start button).
        // Escape key logic is handled centrally in MainMenuController.cs
        if (inputActions.Player.Menu.WasPressedThisFrame())
        {
            if (MainMenuController.Instance != null)
            {
                MainMenuController.Instance.ToggleMenu();
            }
        }
    }
}
