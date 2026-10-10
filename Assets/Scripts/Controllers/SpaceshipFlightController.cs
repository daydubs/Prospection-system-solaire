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

    [Header("Autopilot Parameters (Calibrated: ~2 in-game days Earth->Moon)")]
    public float baseAutoMaxSpeed = 15f;
    public float baseAutoAcceleration = 3f;
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
    public CelestialBody currentReferenceBody { get; private set; }

    private Vector3 velocity = Vector3.zero;
    private float targetSpeed = 0f;
    private float orbitAngleX = 20f;
    private float orbitAngleY = 0f;
    private float currentOrbitDist = 20f;

    private void Awake()
    {
        // Ensure responsive flight controls even if old low values were serialized
        if (normalSpeed < 1f)
        {
            normalSpeed = 4.0f;
            boostMultiplier = 3.5f;
            acceleration = 6.0f;
            deceleration = 8.0f;
        }
    }

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
        UpdateReferenceBody();

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

    private void UpdateReferenceBody()
    {
        CelestialBody newRef = null;
        if (currentMode == FlightMode.OrbitInspect && SolarSystemManager.Instance != null && SolarSystemManager.Instance.currentDestination != null)
        {
            newRef = SolarSystemManager.Instance.currentDestination;
        }
        else if (currentMode == FlightMode.Autopilot && SolarSystemManager.Instance != null && SolarSystemManager.Instance.currentDestination != null)
        {
            // During Autopilot, lock onto destination once inside its SOI
            CelestialBody dest = SolarSystemManager.Instance.currentDestination;
            if (dest.IsPositionInSOI(transform.position))
            {
                newRef = dest;
            }
            else
            {
                newRef = SolarSystemManager.Instance.GetDominantCelestialBody(transform.position, currentReferenceBody);
            }
        }
        else if (SolarSystemManager.Instance != null)
        {
            newRef = SolarSystemManager.Instance.GetDominantCelestialBody(transform.position, currentReferenceBody);
        }

        if (newRef != currentReferenceBody)
        {
            Vector3 prevHostVel = currentReferenceBody != null ? currentReferenceBody.GetVelocity() : Vector3.zero;
            Vector3 newHostVel = newRef != null ? newRef.GetVelocity() : Vector3.zero;

            // Conserve world velocity across SOI transition
            Vector3 worldVel = velocity + prevHostVel;
            velocity = worldVel - newHostVel;

            float maxV = normalSpeed * boostMultiplier * techSpeedMultiplier * 1.5f;
            if (velocity.magnitude > maxV && maxV > 0.1f)
            {
                velocity = velocity.normalized * maxV;
            }

            currentReferenceBody = newRef;
            if (currentReferenceBody != null)
            {
                Debug.Log($"[Spaceship] Entré dans le référentiel relatif de : {currentReferenceBody.bodyName}");
            }
        }
    }

    private void UpdateDestinationDistance()
    {
        if (SolarSystemManager.Instance != null && SolarSystemManager.Instance.currentDestination != null)
        {
            CelestialBody dest = SolarSystemManager.Instance.currentDestination;
            distanceToDestination = Vector3.Distance(transform.position, dest.transform.position);
            float scaledRadius = dest.bodyRadius * (dest.transform != null ? dest.transform.lossyScale.x : 1f);
            float safeDist = Mathf.Max(scaledRadius * arriveDistanceOffset, 8f);
            isNearDestination = distanceToDestination <= safeDist * 1.8f;
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

        // O key: Toggle Orbit Stabilization
        if (keyboard.oKey.wasPressedThisFrame)
        {
            ToggleOrbitMode();
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

    public void ToggleOrbitMode()
    {
        if (currentMode == FlightMode.OrbitInspect)
        {
            SetFlightMode(FlightMode.FreeFlight);
            velocity = Vector3.zero;
            currentSpeed = 0f;
            Debug.Log("[Spaceship] Désengagement de l'orbite -> Vol libre relatif.");
        }
        else
        {
            CelestialBody target = (SolarSystemManager.Instance != null && SolarSystemManager.Instance.currentDestination != null)
                ? SolarSystemManager.Instance.currentDestination
                : currentReferenceBody;

            if (target != null && target.bodyType != CelestialBodyType.Star)
            {
                float dist = Vector3.Distance(transform.position, target.transform.position);
                float scaledRadius = target.bodyRadius * (target.transform != null ? target.transform.lossyScale.x : 1f);
                float maxCatchDist = Mathf.Max(scaledRadius * 6f, target.GetEffectiveSOIRadius());

                if (dist <= maxCatchDist)
                {
                    if (SolarSystemManager.Instance != null)
                    {
                        SolarSystemManager.Instance.SetDestination(target);
                    }
                    SetFlightMode(FlightMode.OrbitInspect);
                    Debug.Log($"[Spaceship] Stabilisé en orbite de {target.bodyName} !");
                }
                else
                {
                    Debug.Log($"[Spaceship] Trop éloigné de {target.bodyName} ({dist:F1} u > {maxCatchDist:F1} u) pour se satelliser. Rapprochez-vous d'abord.");
                }
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
                currentReferenceBody = dest;
                float scaledRadius = dest.bodyRadius * dest.transform.lossyScale.x;
                Vector3 relPos = transform.position - dest.transform.position;
                float dist = relPos.magnitude;
                currentOrbitDist = Mathf.Clamp(dist, Mathf.Max(scaledRadius * 1.3f, minOrbitDist), maxOrbitDist);

                if (relPos.sqrMagnitude > 0.01f)
                {
                    orbitAngleY = Mathf.Atan2(relPos.x, -relPos.z) * Mathf.Rad2Deg;
                    float horizontalDist = new Vector2(relPos.x, relPos.z).magnitude;
                    orbitAngleX = Mathf.Atan2(relPos.y, horizontalDist) * Mathf.Rad2Deg;
                    orbitAngleX = Mathf.Clamp(orbitAngleX, -85f, 85f);
                }

                velocity = Vector3.zero;
                currentSpeed = 0f;
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

        // Relative motion: relative ship velocity + velocity of the host celestial body
        Vector3 hostVelocity = currentReferenceBody != null ? currentReferenceBody.GetVelocity() : Vector3.zero;
        transform.position += (velocity + hostVelocity) * simDt;
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
        float targetOrbitDist = Mathf.Max(scaledRadius * arriveDistanceOffset, 6f);
        Vector3 targetPos = dest.transform.position;
        Vector3 targetVelocity = dest.GetVelocity();

        Vector3 toTarget = targetPos - transform.position;
        float distToCenter = toTarget.magnitude;
        float distToOrbit = Mathf.Max(0f, distToCenter - targetOrbitDist);

        // Arrival at destination orbit
        if (distToCenter <= targetOrbitDist * 1.15f || distToOrbit < 1.5f)
        {
            currentSpeed = 0f;
            velocity = Vector3.zero;
            SetFlightMode(FlightMode.OrbitInspect);
            Debug.Log($"[Autopilot] Arrivé en orbite stabilisée de {dest.bodyName} !");
            return;
        }

        float maxAllowedSpeed = baseAutoMaxSpeed * techSpeedMultiplier;
        float currentAcceleration = baseAutoAcceleration * techSpeedMultiplier;

        // Distance-based kinematic deceleration
        float brakingDist = (maxAllowedSpeed * maxAllowedSpeed) / (2f * Mathf.Max(currentAcceleration, 0.1f)) + 6f;
        float desiredRelSpeed;

        if (distToOrbit < brakingDist)
        {
            float t = distToOrbit / brakingDist;
            desiredRelSpeed = Mathf.Max(1.0f, maxAllowedSpeed * Mathf.Sqrt(Mathf.Clamp01(t)));
        }
        else
        {
            desiredRelSpeed = maxAllowedSpeed;
        }

        currentSpeed = Mathf.MoveTowards(currentSpeed, desiredRelSpeed, currentAcceleration * simDt);

        // Direct relative approach vector toward destination
        Vector3 approachDir = toTarget.normalized;
        if (approachDir.sqrMagnitude < 0.001f) approachDir = transform.forward;

        Quaternion targetRot = Quaternion.LookRotation(approachDir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 5f * Time.deltaTime);

        // Movement in destination reference frame: relative velocity + target velocity
        Vector3 movement = (approachDir * currentSpeed + targetVelocity) * simDt;
        transform.position += movement;
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
                // Use a higher lerp speed to ensure camera keeps up when time is accelerated
                float lerpSpeed = 15f * Mathf.Max(1f, SolarSystemManager.Instance != null ? SolarSystemManager.Instance.timeScale : 1f);

                shipCamera.transform.position = Vector3.Lerp(shipCamera.transform.position, cameraMountPoint.position, lerpSpeed * Time.deltaTime);
                shipCamera.transform.rotation = Quaternion.Slerp(shipCamera.transform.rotation, cameraMountPoint.rotation, lerpSpeed * Time.deltaTime);
            }
        }
    }
}
