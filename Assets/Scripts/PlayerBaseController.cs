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
    public float interactRange = 4.5f;
    public bool isNearHubScreen = false;

    [Header("UI & Inventory")]
    public GameObject inventoryUI;
    public bool isInventoryOpen = false;

    [Header("State")]
    public bool canMove = true;
    public bool isFirstPerson = true;

    private CharacterController characterController;
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

        // Ensure inventory is closed at start
        if (inventoryUI != null)
        {
            inventoryUI.SetActive(false);
        }
        if(mainMenuUI != null)
        {
            mainMenuUI.SetActive(false);
        }
    }

    private void Update()
    {
        
        HandleInventoryInput();

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

        if (isInventoryOpen)
        {
            verticalVelocity = 0f;
            return;
        }

        if (!canMove) return;

        HandleLook();
        HandleMovement();
        CheckInteraction();
        HandleInteractionInput();
        HandleMainMenu();
    }

    private void HandleInventoryInput()
    {
        if (inputActions.Player.Inventory.WasPressedThisFrame() || (Keyboard.current != null && Keyboard.current.iKey.wasPressedThisFrame))
        {
            // Do not toggle inventory if we are in other UI modes
            if (MainMenuController.Instance != null && MainMenuController.Instance.isMenuOpen) return;
            if (EarthBaseController.Instance != null && EarthBaseController.Instance.isHubUIOpen) return;

            isInventoryOpen = !isInventoryOpen;

            if (inventoryUI != null)
            {
                inventoryUI.SetActive(isInventoryOpen);
            }

            if (isInventoryOpen)
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
        // 1. Raycast for IInteractable
        currentInteractable = null;
        currentMineable = null;
        if (cameraTransform != null)
        {
            if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out RaycastHit hit, interactRange))
            {
                currentInteractable = hit.collider.GetComponent<IInteractable>();
                currentMineable = hit.collider.GetComponent<MineableResource>();
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

        if (inputActions.Player.Attack.WasPressedThisFrame())
        {
            if (currentMineable != null)
            {
                HotbarUI hotbarUI = FindAnyObjectByType<HotbarUI>();
                if (hotbarUI != null)
                {
                    Item selectedItem = hotbarUI.GetSelectedItem();
                    if (selectedItem != null && selectedItem.itemName == currentMineable.requiredToolName)
                    {
                        currentMineable.Mine();
                    }
                }
            }
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
