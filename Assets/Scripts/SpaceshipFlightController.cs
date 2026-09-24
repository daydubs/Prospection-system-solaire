using UnityEngine;
using UnityEngine.InputSystem;

public enum FlightMode
{
    FreeFlight,
    Autopilot,
    OrbitInspect
}

public class SpaceshipFlightController : MonoBehaviour
{
    [Header("Flight Parameters (Calibrated: 2-3 in-game days Earth->Moon at cruise speed)")]
    public float normalSpeed = 0.08f;
    public float boostMultiplier = 3.5f;
    public float rotationSpeed = 50f;
    public float mouseSensitivity = 2f;
    public float acceleration = 0.05f;
    public float deceleration = 0.08f;

    [Header("Technology & Upgrades")]
    [Tooltip("Multiplier increased by unlocking technologies. Starts at 1.")]
    public float techSpeedMultiplier = 1f;
    public bool hasWarpTechnology = false;

    [Header("Autopilot Parameters")]
    public float baseAutoMaxSpeed = 10f; // Was warpMaxSpeed (160f)
    public float baseAutoAcceleration = 2f; // Was warpAcceleration (15f)
    public float arriveDistanceOffset = 2.5f;
    public float arrivalDamping = 5f;

    [Header("Orbit / Inspect Parameters")]
    public float orbitDistanceMultiplier = 3f;
    public float orbitRotateSpeed = 30f;
    public float minOrbitDist = 2f;
    public float maxOrbitDist = 1500f;

    [Header("Camera & View")]
    public Camera shipCamera;
    public Transform cameraMountPoint;
    public Vector3 firstPersonCameraOffset = new Vector3(0.06f, -0.11f, 1.61f); // Adjust in Inspector for cockpit seat
    public Vector3 thirdPersonCameraOffset = new Vector3(0f, 2f, -10f); // Pulled back view
    public bool isFirstPersonView = false;

    [HideInInspector] // Keeping it for compatibility if needed elsewhere, but transitioning to FP/TP offsets
    public Vector3 cameraOffset = new Vector3(0f, 2f, -6f);

    [Header("Status")]
    public FlightMode currentMode = FlightMode.FreeFlight;
    public float currentSpeed = 0f;
    public float distanceToDestination = 0f;
    public bool isNearDestination = false;

    private Vector3 velocity = Vector3.zero;
    private float targetSpeed = 0f;
    private float orbitAngleX = 20f;
    private float orbitAngleY = 0f;
    private float currentOrbitDist = 20f;

    private float GetSimulationDeltaTime()
    {
        if (SolarSystemManager.Instance != null)
        {
            if (SolarSystemManager.Instance.isPaused) return 0f;
            return Time.deltaTime * SolarSystemManager.Instance.timeScale;
        }
        return Time.deltaTime;
    }

    private void Start()
    {
        if (shipCamera == null)
        {
            shipCamera = Camera.main;
        }

        if (shipCamera != null && cameraMountPoint == null)
        {
            GameObject mount = new GameObject("CameraMount");
            mount.transform.SetParent(transform, false);
            mount.transform.localPosition = isFirstPersonView ? firstPersonCameraOffset : thirdPersonCameraOffset;
            cameraMountPoint = mount.transform;
        }
    }

    private void Update()
    {
        HandleFlightModeInputs();
    }

    private void LateUpdate()
    {
        UpdateDestinationDistance();

        switch (currentMode)
        {
            case FlightMode.FreeFlight:
                UpdateFreeFlight();
                break;
            case FlightMode.Autopilot:
                UpdateAutopilot();
                break;
            case FlightMode.OrbitInspect:
                UpdateOrbitInspect();
                break;
        }

        UpdateCameraPosition();
    }

    private void UpdateDestinationDistance()
    {
        if (SolarSystemManager.Instance != null && SolarSystemManager.Instance.currentDestination != null)
        {
            CelestialBody dest = SolarSystemManager.Instance.currentDestination;
            distanceToDestination = Vector3.Distance(transform.position, dest.transform.position);
            float safeDist = Mathf.Max(dest.bodyRadius * arriveDistanceOffset, 5f);
            isNearDestination = distanceToDestination <= safeDist * 1.5f;
        }
        else
        {
            distanceToDestination = 0f;
            isNearDestination = false;
        }
    }

    private void HandleFlightModeInputs()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // F key: Toggle Camera View (First/Third Person)
        if (keyboard.fKey.wasPressedThisFrame)
        {
            isFirstPersonView = !isFirstPersonView;
        }

        // T key: Engage Autopilot
        if (keyboard.tKey.wasPressedThisFrame)
        {
            EngageAutopilot();
        }

        // J key: Instant Warp (Requires Tech)
        if (keyboard.jKey.wasPressedThisFrame)
        {
            if (hasWarpTechnology)
            {
                WarpToDestination();
            }
            else
            {
                Debug.Log("[Spaceship] Warp technology not yet researched.");
            }
        }

        // Space / WASD interrupt autopilot
        if (currentMode == FlightMode.Autopilot)
        {
            if (keyboard.wKey.isPressed || keyboard.sKey.isPressed || keyboard.aKey.isPressed || keyboard.dKey.isPressed)
            {
                // Soft interrupt if player manually takes control
                SetFlightMode(FlightMode.FreeFlight);
            }
        }
    }

    public void SetFlightMode(FlightMode mode)
    {
        currentMode = mode;
        if (mode == FlightMode.OrbitInspect)
        {
            if (SolarSystemManager.Instance != null && SolarSystemManager.Instance.currentDestination != null)
            {
                CelestialBody dest = SolarSystemManager.Instance.currentDestination;
                float scaledRadius = dest.bodyRadius * dest.transform.lossyScale.x;
                currentOrbitDist = Mathf.Max(scaledRadius * orbitDistanceMultiplier, 5f);
            }
        }
    }

    public void EngageAutopilot()
    {
        if (SolarSystemManager.Instance != null && SolarSystemManager.Instance.currentDestination != null)
        {
            currentMode = FlightMode.Autopilot;
            Debug.Log($"[Autopilot] Engaging navigation to {SolarSystemManager.Instance.currentDestination.bodyName}");
        }
    }

    public void WarpToDestination()
    {
        if (SolarSystemManager.Instance != null && SolarSystemManager.Instance.currentDestination != null)
        {
            CelestialBody dest = SolarSystemManager.Instance.currentDestination;
            Vector3 approachPos = dest.GetApproachPosition(transform.position, arriveDistanceOffset);
            transform.position = approachPos;
            transform.LookAt(dest.transform);
            velocity = Vector3.zero;
            currentSpeed = 0f;
            SetFlightMode(FlightMode.OrbitInspect);
            Debug.Log($"[Spaceship] Warped to orbit of {dest.bodyName}");
        }
    }

    public void FocusOnDestination()
    {
        if (SolarSystemManager.Instance != null && SolarSystemManager.Instance.currentDestination != null)
        {
            SetFlightMode(FlightMode.OrbitInspect);
        }
    }

    private void UpdateFreeFlight()
    {
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;
        if (keyboard == null) return;

        // Translation inputs
        float moveForward = 0f;
        float moveSide = 0f;
        float moveUp = 0f;

        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) moveForward += 1f;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) moveForward -= 1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) moveSide += 1f;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) moveSide -= 1f;
        if (keyboard.spaceKey.isPressed) moveUp += 1f;
        if (keyboard.cKey.isPressed || keyboard.leftCtrlKey.isPressed) moveUp -= 1f;

        bool isBoosting = keyboard.leftShiftKey.isPressed;
        float currentTechMaxSpeed = normalSpeed * techSpeedMultiplier;
        float targetSpeedMagnitude = (isBoosting ? currentTechMaxSpeed * boostMultiplier : currentTechMaxSpeed);
        float currentTechAcceleration = acceleration * techSpeedMultiplier;
        float simDt = GetSimulationDeltaTime();

        Vector3 inputDir = (transform.forward * moveForward + transform.right * moveSide + transform.up * moveUp).normalized;
        if (inputDir.sqrMagnitude > 0.01f)
        {
            targetSpeed = targetSpeedMagnitude;
            velocity = Vector3.MoveTowards(velocity, inputDir * targetSpeed, currentTechAcceleration * (simDt > 0f ? simDt : Time.deltaTime));
        }
        else
        {
            targetSpeed = 0f;
            velocity = Vector3.MoveTowards(velocity, Vector3.zero, deceleration * (simDt > 0f ? simDt : Time.deltaTime));
        }

        transform.position += velocity * simDt;
        currentSpeed = velocity.magnitude;

        // Rotation (Right mouse button drag or Q/E roll) - responsive with real time
        float roll = 0f;
        if (keyboard.qKey.isPressed) roll += 1f;
        if (keyboard.eKey.isPressed) roll -= 1f;
        transform.Rotate(Vector3.forward, roll * rotationSpeed * Time.deltaTime, Space.Self);

        if (mouse != null && mouse.rightButton.isPressed)
        {
            Vector2 mouseDelta = mouse.delta.ReadValue();
            float yaw = mouseDelta.x * mouseSensitivity * 0.1f;
            float pitch = -mouseDelta.y * mouseSensitivity * 0.1f;

            transform.Rotate(Vector3.up, yaw, Space.World);
            transform.Rotate(Vector3.right, pitch, Space.Self);
        }
    }

    private void UpdateAutopilot()
    {
        if (SolarSystemManager.Instance == null || SolarSystemManager.Instance.currentDestination == null)
        {
            currentMode = FlightMode.FreeFlight;
            return;
        }

        float simDt = GetSimulationDeltaTime();
        if (simDt <= 0f) return;

        CelestialBody dest = SolarSystemManager.Instance.currentDestination;
        float scaledRadius = dest.bodyRadius * dest.transform.lossyScale.x;
        float targetDistFromCenter = Mathf.Max(scaledRadius * arriveDistanceOffset, 6f);
        Vector3 targetPos = dest.GetApproachPosition(transform.position, arriveDistanceOffset);

        Vector3 toTarget = targetPos - transform.position;
        float dist = toTarget.magnitude;

        if (dist < 2f)
        {
            // Arrived at destination orbit
            currentSpeed = 0f;
            velocity = Vector3.zero;
            SetFlightMode(FlightMode.OrbitInspect);
            Debug.Log($"[Autopilot] Arrived in orbit of {dest.bodyName}!");
            return;
        }

        // Rotate smoothly towards target
        Quaternion targetRot = Quaternion.LookRotation(toTarget.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 5f * Time.deltaTime);

        // Speed calculation based on distance and tech
        float maxAllowedSpeed = baseAutoMaxSpeed * techSpeedMultiplier;
        float currentAcceleration = baseAutoAcceleration * techSpeedMultiplier;

        float desiredSpeed = Mathf.Clamp(dist * 0.8f, 0.5f, maxAllowedSpeed);
        currentSpeed = Mathf.MoveTowards(currentSpeed, desiredSpeed, currentAcceleration * simDt);

        transform.position = Vector3.MoveTowards(transform.position, targetPos, currentSpeed * simDt);
    }

    private void UpdateOrbitInspect()
    {
        if (SolarSystemManager.Instance == null || SolarSystemManager.Instance.currentDestination == null)
        {
            currentMode = FlightMode.FreeFlight;
            return;
        }

        CelestialBody dest = SolarSystemManager.Instance.currentDestination;
        var mouse = Mouse.current;
        float scaledRadius = dest.bodyRadius * dest.transform.lossyScale.x;

        if (mouse != null)
        {
            // Mouse scroll zoom
            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                currentOrbitDist -= scroll * 0.05f * (currentOrbitDist * 0.2f);
                currentOrbitDist = Mathf.Clamp(currentOrbitDist, scaledRadius * 1.3f, maxOrbitDist);
            }

            // Right click drag rotate
            if (mouse.rightButton.isPressed)
            {
                Vector2 delta = mouse.delta.ReadValue();
                orbitAngleY += delta.x * mouseSensitivity * 0.2f;
                orbitAngleX -= delta.y * mouseSensitivity * 0.2f;
                orbitAngleX = Mathf.Clamp(orbitAngleX, -85f, 85f);
            }
        }

        // Passive slow orbital rotation
        float simDt = GetSimulationDeltaTime();
        orbitAngleY += orbitRotateSpeed * 0.1f * (simDt > 0f ? simDt : Time.deltaTime);

        Quaternion rotation = Quaternion.Euler(orbitAngleX, orbitAngleY, 0f);
        Vector3 offset = rotation * new Vector3(0f, 0f, -currentOrbitDist);
        transform.position = dest.transform.position + offset;
        transform.LookAt(dest.transform.position);

        currentSpeed = 0f;
    }

    private void UpdateCameraPosition()
    {
        if (shipCamera == null) return;

        // Smoothly interpolate the camera mount's local position and rotation based on the selected view mode
        if (cameraMountPoint != null)
        {
            Vector3 targetOffset = isFirstPersonView ? firstPersonCameraOffset : thirdPersonCameraOffset;
            cameraMountPoint.localPosition = Vector3.Lerp(cameraMountPoint.localPosition, targetOffset, 5f * Time.deltaTime);

            Quaternion targetRot = isFirstPersonView ? Quaternion.identity : Quaternion.Euler(8f, 0f, 0f);
            cameraMountPoint.localRotation = Quaternion.Slerp(cameraMountPoint.localRotation, targetRot, 5f * Time.deltaTime);
        }

        if (currentMode == FlightMode.OrbitInspect)
        {
            shipCamera.transform.position = transform.position;
            shipCamera.transform.rotation = transform.rotation;
        }
        else
        {
            if (cameraMountPoint != null)
            {
                shipCamera.transform.position = Vector3.Lerp(shipCamera.transform.position, cameraMountPoint.position, 15f * Time.deltaTime);
                shipCamera.transform.rotation = Quaternion.Slerp(shipCamera.transform.rotation, cameraMountPoint.rotation, 15f * Time.deltaTime);
            }
        }
    }
}
