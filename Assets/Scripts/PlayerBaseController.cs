using UnityEngine;
using UnityEngine.InputSystem;

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

    [Header("State")]
    public bool canMove = true;
    public bool isFirstPerson = true;

    private CharacterController characterController;
    private float verticalVelocity = 0f;
    private float cameraPitch = 0f;

    private void Awake()
    {
        Debug.Log($"[PlayerBaseController] Awake() - Position initiale du joueur : {transform.position}");
        characterController = GetComponent<CharacterController>();
        if (cameraTransform == null)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null) cameraTransform = cam.transform;
        }
    }

    private void OnEnable()
    {
        // When enabled, force sync physics transforms so CharacterController recognizes the position
        Physics.SyncTransforms();
        Debug.Log($"[PlayerBaseController] OnEnable() - Position : {transform.position}");
    }

    private void Start()
    {
        Debug.Log($"[PlayerBaseController] Start() (AVANT bump) - Position : {transform.position}");
        // Fix CharacterController falling through floor on spawn:
        // When the game starts, UI is active and the player doesn't move, but
        // physics are not resolved yet.
        // The floor is perfectly flat and sometimes the player is initialized exactly at the boundary.
        // A slight manual bump ensures the CC initiates its collision resolution correctly downwards.
        // (Note: Unparenting has been intentionally omitted to avoid coordinate system regressions).
        transform.position += Vector3.up * 1f; // Slight bump up
        Physics.SyncTransforms();
        Debug.Log($"[PlayerBaseController] Start() (APRES bump) - Position : {transform.position}");
    }

    private void Update()
    {
        // Log physics state unconditionally to track position even while in menus
        LogPhysicsState();

        // Don't process player movement when main menu is open or when Hub UI is active
        if (MainMenuController.Instance != null && MainMenuController.Instance.isMenuOpen)
        {
            return;
        }

        if (EarthBaseController.Instance != null && EarthBaseController.Instance.isHubUIOpen)
        {
            return;
        }

        if (!canMove) return;

        HandleLook();
        HandleMovement();
        CheckInteraction();
    }

    private void HandleLook()
    {
        if (cameraTransform == null) return;

        var mouse = Mouse.current;
        if (mouse == null) return;

        Vector2 mouseDelta = mouse.delta.ReadValue() * (mouseSensitivity * 0.1f);

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

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        float moveX = 0f;
        float moveZ = 0f;

        // Support both AZERTY (ZQSD) and QWERTY (WASD)
        if (keyboard.wKey.isPressed || keyboard.zKey.isPressed || keyboard.upArrowKey.isPressed) moveZ += 1f;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) moveZ -= 1f;
        if (keyboard.aKey.isPressed || keyboard.qKey.isPressed || keyboard.leftArrowKey.isPressed) moveX -= 1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) moveX += 1f;

        Vector3 moveDir = (transform.forward * moveZ + transform.right * moveX).normalized;

        bool isSprinting = keyboard.leftShiftKey.isPressed;
        float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;

        float dt = Time.deltaTime;
        if (dt > 0.1f) dt = 0.1f; // Clamp to avoid large movement spikes (e.g., initial frame drops)

        if (characterController.isGrounded && verticalVelocity < 0)
        {
            verticalVelocity = -2f; // Slight negative force to keep grounded reliably
        }

        if (characterController.isGrounded && keyboard.spaceKey.wasPressedThisFrame)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        verticalVelocity += gravity * dt;
        if (verticalVelocity < -50f) verticalVelocity = -50f; // Terminal velocity

        Vector3 finalMovement = (moveDir * currentSpeed) + (Vector3.up * verticalVelocity);
        characterController.Move(finalMovement * dt);
    }

    private int logFrameSkip = 0;
    private void LogPhysicsState()
    {
        if (characterController == null) return;

        // Skip some frames so we don't spam the console too hard, 
        // but still capture the fall. Logging every 5th frame.
        logFrameSkip++;
        if (logFrameSkip % 5 != 0) return;

        string state = $"[PlayerPhysicsLog] PosY: {transform.position.y:F3}, " +
                       $"VelocityY: {verticalVelocity:F3}, " +
                       $"CC.isGrounded: {characterController.isGrounded}";

        RaycastHit hit;
        float rayDist = 5f;
        // Raycast from slightly above the center to ensure we hit the floor even if slightly embedded
        Vector3 rayStart = transform.position + Vector3.up * 0.1f;
        if (Physics.Raycast(rayStart, Vector3.down, out hit, rayDist))
        {
            state += $", HitDist: {hit.distance:F3}, HitObj: {hit.collider.gameObject.name}";

            // Warning if we seem to be falling through
            if (!characterController.isGrounded && hit.distance < (characterController.height / 2f) + 0.2f && verticalVelocity < -2f)
            {
                Debug.LogWarning(">>> [WARNING] PLAYER FALLING THROUGH DETECTED! " + state);
            }
        }
        else
        {
            state += $", HitDist: >{rayDist} (No ground found below)";
        }

        Debug.Log(state);
    }

    private void CheckInteraction()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // Check distance to Giant Hub Screen
        if (EarthBaseController.Instance != null && EarthBaseController.Instance.hubScreenTransform != null)
        {
            float dist = Vector3.Distance(transform.position, EarthBaseController.Instance.hubScreenTransform.position);
            isNearHubScreen = dist <= interactRange;

            if (isNearHubScreen && (keyboard.eKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame))
            {
                EarthBaseController.Instance.ToggleHubUI();
            }
        }
    }
}
