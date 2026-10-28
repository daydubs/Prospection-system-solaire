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

    [Header("Engine Audio")]
    public AudioSource engineAudioSource;
    public AudioClip engineLoopClip;

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

    // Track the target's exact position to calculate curved displacement
    private Vector3 lastTargetPos;
    private CelestialBody lastTargetBody;
    private int lastAutopilotFrame = -1;
    private Vector3 positionRemainder = Vector3.zero;
    private Vector3 freeFlightPositionRemainder = Vector3.zero;

    // Track current reference body's exact displacement for perfect curved relative movement
    private Vector3 lastRefBodyPos;
    private CelestialBody lastRefBodyFrameTracker;
    private int lastFreeFlightFrame = -1;

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

        InitializeEngineAudio();
    }

    private void InitializeEngineAudio()
    {
        if (engineAudioSource == null)
        {
            engineAudioSource = gameObject.AddComponent<AudioSource>();
        }

        engineAudioSource.loop = true;
        engineAudioSource.playOnAwake = false;
        engineAudioSource.spatialBlend = 0.2f;

#if UNITY_EDITOR
        if (engineLoopClip == null)
        {
            engineLoopClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Libraries/Soundbits_freeSFX_2025/Sounds/jw4_engines-motors-024.wav");
        }
#endif

        if (engineLoopClip != null)
        {
            engineAudioSource.clip = engineLoopClip;
            engineAudioSource.volume = 0.15f;
            engineAudioSource.pitch = 0.8f;
            engineAudioSource.Play();
        }
    }

    private void UpdateEngineAudio()
    {
        if (engineAudioSource == null || !engineAudioSource.isPlaying) return;

        float maxExpectedSpeed = Mathf.Max(normalSpeed * boostMultiplier * techSpeedMultiplier, 1f);
        float speedRatio = Mathf.Clamp01(currentSpeed / maxExpectedSpeed);

        float targetPitch = Mathf.Lerp(0.8f, 1.4f, speedRatio);
        float effectiveSFX = AudioManager.Instance != null ? AudioManager.Instance.GetEffectiveSFXVolume() : 0.8f;
        float targetVol = Mathf.Lerp(0.18f, 0.85f, speedRatio) * effectiveSFX;

        engineAudioSource.pitch = Mathf.Lerp(engineAudioSource.pitch, targetPitch, 5f * Time.deltaTime);
        engineAudioSource.volume = Mathf.Lerp(engineAudioSource.volume, targetVol, 5f * Time.deltaTime);
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

        UpdateEngineAudio();
        UpdateCameraPosition();
    }

    private void UpdateReferenceBody()
    {
        CelestialBody newRef = null;
        if (currentMode == FlightMode.OrbitInspect && currentReferenceBody != null)
        {
            newRef = currentReferenceBody;
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
                    // Assign Reference Body Before Entering Orbit Inspect
                    currentReferenceBody = target;
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
            if (currentReferenceBody != null)
            {
                CelestialBody dest = currentReferenceBody;
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
        if (currentMode == FlightMode.OrbitInspect)
        {
            Debug.Log("[Spaceship] Impossible d'engager l'autopilote : veuillez quitter l'orbite d'abord.");
            return;
        }

        if (SolarSystemManager.Instance != null && SolarSystemManager.Instance.currentDestination != null)
        {
            currentMode = FlightMode.Autopilot;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayShipWarp();
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
            currentReferenceBody = dest;
            SetFlightMode(FlightMode.OrbitInspect);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayShipWarp();
            Debug.Log($"[Spaceship] Warped to orbit of {dest.bodyName}");
        }
    }

    public void FocusOnDestination()
    {
        if (SolarSystemManager.Instance != null && SolarSystemManager.Instance.currentDestination != null)
        {
            currentReferenceBody = SolarSystemManager.Instance.currentDestination;
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

        // Determine exact displacement of the reference body this frame
        Vector3 hostDeltaPosition = Vector3.zero;
        if (currentReferenceBody != null)
        {
            if (lastRefBodyFrameTracker == currentReferenceBody && Time.frameCount == lastFreeFlightFrame + 1)
            {
                hostDeltaPosition = currentReferenceBody.transform.position - lastRefBodyPos;
            }
            lastRefBodyPos = currentReferenceBody.transform.position;
        }
        lastRefBodyFrameTracker = currentReferenceBody;
        lastFreeFlightFrame = Time.frameCount;

        // Relative motion: relative ship velocity + exact host positional displacement (curve, not tangent)
        Vector3 relativeMovement = velocity * simDt;

        // Fix Floating-Point Precision Loss at extreme scales for Free Flight
        freeFlightPositionRemainder += relativeMovement;

        Vector3 appliedRelativeMovement = Vector3.zero;
        if (freeFlightPositionRemainder.sqrMagnitude > 0.01f)
        {
            appliedRelativeMovement = freeFlightPositionRemainder;
            freeFlightPositionRemainder = Vector3.zero;
        }

        transform.position += appliedRelativeMovement + hostDeltaPosition;
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
            positionRemainder = Vector3.zero; // Clear remainder on arrival
            currentReferenceBody = dest;
            SetFlightMode(FlightMode.OrbitInspect);
            Debug.Log($"[Autopilot] Arrivé en orbite stabilisée de {dest.bodyName} !");
            return;
        }

        // Determine the actual displacement of the target this frame
        Vector3 targetDeltaPosition = Vector3.zero;
        if (lastTargetBody == dest && Time.frameCount == lastAutopilotFrame + 1)
        {
            targetDeltaPosition = targetPos - lastTargetPos;
        }
        lastTargetPos = targetPos;
        lastTargetBody = dest;
        lastAutopilotFrame = Time.frameCount;

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

        float rotSpeed = 5f * Mathf.Max(1f, SolarSystemManager.Instance != null ? SolarSystemManager.Instance.timeScale : 1f);
        Quaternion targetRot = Quaternion.LookRotation(approachDir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotSpeed * Time.deltaTime);

        // Movement perfectly anchored in the moving/rotating destination reference frame
        Vector3 relativeMovement = approachDir * currentSpeed * simDt;

        // Fix Floating-Point Precision Loss at extreme scales
        // Store small movements in an accumulator and only apply them when they exceed a threshold Unity can reliably process
        positionRemainder += relativeMovement;

        Vector3 appliedRelativeMovement = Vector3.zero;
        if (positionRemainder.sqrMagnitude > 0.01f) // Apply when movement exceeds ~0.1 units
        {
            appliedRelativeMovement = positionRemainder;
            positionRemainder = Vector3.zero;
        }

        Vector3 totalMovement = appliedRelativeMovement + targetDeltaPosition;
        transform.position += totalMovement;
    }

    private void UpdateOrbitInspect()
    {
        if (currentReferenceBody == null)
        {
            currentMode = FlightMode.FreeFlight;
            return;
        }

        CelestialBody dest = currentReferenceBody;
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
                shipCamera.transform.position = cameraMountPoint.position;
                shipCamera.transform.rotation = cameraMountPoint.rotation;
            }
            else
            {
                shipCamera.transform.position = transform.position;
                shipCamera.transform.rotation = transform.rotation;
            }
        }
    }
}
