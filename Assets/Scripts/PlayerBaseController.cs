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
    private Vector3 defaultCameraLocalPos = new Vector3(0f, 0.75f, 0f);

    public float CameraPitch
    {
        get => cameraPitch;
        set => cameraPitch = value;
    }

    public Vector3 DefaultCameraLocalPos => defaultCameraLocalPos;

    private void Awake()
    {
        Debug.Log($"[PlayerBaseController] Awake() - Position initiale du joueur : {transform.position}");
        characterController = GetComponent<CharacterController>();
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
        // When enabled, force sync physics transforms so CharacterController recognizes the position
        Physics.SyncTransforms();
        Debug.Log($"[PlayerBaseController] OnEnable() - Position : {transform.position}");
    }

    private void Start()
    {
        // Initial sync of physics transforms
        Physics.SyncTransforms();
        verticalVelocity = 0f;
    }

    private void Update()
    {
        // Don't process player movement when main menu or pause is open, or when Hub UI is active
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

    private void CheckInteraction()
    {
        if (EarthBaseController.Instance == null) return;

        float distDesk = EarthBaseController.Instance.hubScreenTransform != null
            ? Vector3.Distance(transform.position, EarthBaseController.Instance.hubScreenTransform.position)
            : float.MaxValue;

        float distScreen = EarthBaseController.Instance.hubCameraTarget != null
            ? Vector3.Distance(transform.position, EarthBaseController.Instance.hubCameraTarget.position)
            : float.MaxValue;

        isNearHubScreen = Mathf.Min(distDesk, distScreen) <= interactRange;
    }
}
