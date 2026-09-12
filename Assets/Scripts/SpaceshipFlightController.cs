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
    [Header("Flight Parameters")]
    public float normalSpeed = 30f;
    public float boostMultiplier = 4f;
    public float rotationSpeed = 60f;
    public float mouseSensitivity = 2f;
    public float acceleration = 10f;
    public float deceleration = 15f;

    [Header("Autopilot Parameters")]
    public float warpMaxSpeed = 250f;
    public float warpAcceleration = 40f;
    public float arriveDistanceOffset = 3f;
    public float arrivalDamping = 5f;

    [Header("Orbit / Inspect Parameters")]
    public float orbitDistanceMultiplier = 3f;
    public float orbitRotateSpeed = 40f;
    public float minOrbitDist = 2f;
    public float maxOrbitDist = 300f;

    [Header("Camera & View")]
    public Camera shipCamera;
    public Transform cameraMountPoint;
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
            mount.transform.localPosition = cameraOffset;
            cameraMountPoint = mount.transform;
        }
    }

    private void Update()
    {
        UpdateDestinationDistance();
        HandleFlightModeInputs();

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

        // F key: Toggle Free Flight / Inspect
        if (keyboard.fKey.wasPressedThisFrame)
        {
            if (currentMode == FlightMode.OrbitInspect)
            {
                SetFlightMode(FlightMode.FreeFlight);
            }
            else
            {
                FocusOnDestination();
            }
        }

        // T key: Engage Autopilot
        if (keyboard.tKey.wasPressedThisFrame)
        {
            EngageAutopilot();
        }

        // J key: Instant Warp
        if (keyboard.jKey.wasPressedThisFrame)
        {
            WarpToDestination();
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
                currentOrbitDist = Mathf.Max(dest.bodyRadius * orbitDistanceMultiplier, 5f);
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
        float targetSpeedMagnitude = (isBoosting ? normalSpeed * boostMultiplier : normalSpeed);

        Vector3 inputDir = (transform.forward * moveForward + transform.right * moveSide + transform.up * moveUp).normalized;
        if (inputDir.sqrMagnitude > 0.01f)
        {
            targetSpeed = targetSpeedMagnitude;
            velocity = Vector3.MoveTowards(velocity, inputDir * targetSpeed, acceleration * Time.deltaTime);
        }
        else
        {
            targetSpeed = 0f;
            velocity = Vector3.MoveTowards(velocity, Vector3.zero, deceleration * Time.deltaTime);
        }

        transform.position += velocity * Time.deltaTime;
        currentSpeed = velocity.magnitude;

        // Rotation (Right mouse button drag or Q/E roll)
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

        CelestialBody dest = SolarSystemManager.Instance.currentDestination;
        float targetDistFromCenter = Mathf.Max(dest.bodyRadius * arriveDistanceOffset, 6f);
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

        // Speed calculation based on distance
        float desiredSpeed = Mathf.Clamp(dist * 1.5f, 5f, warpMaxSpeed);
        currentSpeed = Mathf.MoveTowards(currentSpeed, desiredSpeed, warpAcceleration * Time.deltaTime);

        transform.position = Vector3.MoveTowards(transform.position, targetPos, currentSpeed * Time.deltaTime);
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

        if (mouse != null)
        {
            // Mouse scroll zoom
            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                currentOrbitDist -= scroll * 0.05f * (currentOrbitDist * 0.2f);
                currentOrbitDist = Mathf.Clamp(currentOrbitDist, dest.bodyRadius * 1.3f, maxOrbitDist);
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
        orbitAngleY += orbitRotateSpeed * 0.1f * Time.deltaTime;

        Quaternion rotation = Quaternion.Euler(orbitAngleX, orbitAngleY, 0f);
        Vector3 offset = rotation * new Vector3(0f, 0f, -currentOrbitDist);
        transform.position = dest.transform.position + offset;
        transform.LookAt(dest.transform.position);

        currentSpeed = 0f;
    }

    private void UpdateCameraPosition()
    {
        if (shipCamera == null) return;

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
