using System.Collections.Generic;
using UnityEngine;

public enum CelestialBodyType
{
    Star,
    Planet,
    DwarfPlanet,
    Moon,
    Asteroid
}

public class CelestialBody : MonoBehaviour
{
    [Header("General Info")]
    public string bodyName = "Celestial Body";
    public CelestialBodyType bodyType = CelestialBodyType.Planet;
    [TextArea(2, 4)]
    public string description = "";
    [TextArea(2, 4)]
    public string physicalCharacteristics = "";

    [Header("Orbital Parameters")]
    public Transform orbitCenter;
    public float orbitRadius = 20f;
    public float orbitPeriodDays = 365.25f; // in-game days to complete one orbit
    public float orbitInclination = 0f;
    public float initialOrbitAngle = 0f;
    public Color orbitLineColor = new Color(0.3f, 0.6f, 1f, 0.4f);

    [Header("Rotation")]
    public float rotationPeriodDays = 1f; // in-game days to complete one rotation
    public Vector3 rotationAxis = Vector3.up;
    public float axialTilt = 0f;

    [Header("Visual & Scaling")]
    public float bodyRadius = 2f;
    public GameObject visualModel;
    public LineRenderer orbitLine;
    public int orbitResolution = 100;

    [Header("Satellites")]
    public List<CelestialBody> satellites = new List<CelestialBody>();

    [HideInInspector]
    public float currentOrbitAngle = 0f;

    private Transform tiltContainer;
    private SphereCollider bodyCollider;

    private void Awake()
    {
        currentOrbitAngle = initialOrbitAngle;
        SetupCollider();
    }

    private void Start()
    {
        CreateOrbitLine();
        UpdatePosition(0f);
    }

    private void SetupCollider()
    {
        bodyCollider = GetComponent<SphereCollider>();
        if (bodyCollider == null)
        {
            bodyCollider = gameObject.AddComponent<SphereCollider>();
        }
        bodyCollider.radius = Mathf.Max(bodyRadius * 1.2f, 1.5f);
        bodyCollider.isTrigger = false;
    }

    public void Initialize(Transform center, float radius, float oPeriodDays, float rPeriodDays, float size, Color orbitCol, float tilt = 0f, float startAngle = 0f)
    {
        orbitCenter = center;
        orbitRadius = radius;
        orbitPeriodDays = oPeriodDays;
        rotationPeriodDays = rPeriodDays;
        bodyRadius = size;
        orbitLineColor = orbitCol;
        axialTilt = tilt;
        initialOrbitAngle = startAngle;
        currentOrbitAngle = startAngle;

        transform.localScale = Vector3.one * (size * 2f);
        SetupCollider();
        CreateOrbitLine();
        UpdatePosition(0f);
    }

    public void CreateOrbitLine()
    {
        if (orbitRadius <= 0.1f || orbitCenter == null)
            return;

        if (orbitLine == null)
        {
            GameObject lineObj = new GameObject(bodyName + "_OrbitLine");
            lineObj.transform.SetParent(orbitCenter, false);
            orbitLine = lineObj.AddComponent<LineRenderer>();
            orbitLine.useWorldSpace = false;
            orbitLine.loop = true;
            orbitLine.positionCount = orbitResolution;

            // Thin glowing line
            orbitLine.startWidth = Mathf.Max(0.15f, orbitRadius * 0.003f);
            orbitLine.endWidth = orbitLine.startWidth;

            // Shader material
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Color");

            Material lineMat = new Material(shader);
            lineMat.color = orbitLineColor;
            orbitLine.material = lineMat;
            orbitLine.startColor = orbitLineColor;
            orbitLine.endColor = orbitLineColor;
        }

        UpdateOrbitPath();
    }

    public void UpdateOrbitPath()
    {
        if (orbitLine == null || orbitRadius <= 0.1f) return;

        float scaledOrbitRadius = orbitRadius;
        if (orbitCenter != null)
        {
            scaledOrbitRadius *= orbitCenter.lossyScale.x;
        }

        Quaternion inclinationRot = Quaternion.Euler(orbitInclination, 0f, 0f);
        Vector3[] points = new Vector3[orbitResolution];
        for (int i = 0; i < orbitResolution; i++)
        {
            float angle = (i / (float)orbitResolution) * 360f * Mathf.Deg2Rad;
            Vector3 localPos = new Vector3(Mathf.Cos(angle) * scaledOrbitRadius, 0f, Mathf.Sin(angle) * scaledOrbitRadius);
            points[i] = inclinationRot * localPos;
        }
        orbitLine.SetPositions(points);
    }

    public void SetOrbitLineVisibility(bool visible)
    {
        if (orbitLine != null)
        {
            orbitLine.enabled = visible;
        }
    }


    public Vector3 GetVelocity()
    {
        if (orbitCenter == null || orbitRadius <= 0.01f || Mathf.Abs(orbitPeriodDays) < 0.0001f)
            return Vector3.zero;

        float inGameSecondsMultiplier = GameManager.Instance != null ? GameManager.Instance.inGameSecondsPerRealSecond : 720f;
        // radians per real second
        float radPerSec = (2f * Mathf.PI / orbitPeriodDays) * (inGameSecondsMultiplier / 86400f);

        // Remove timeScale multiplication from here because simDt in FlightController already handles it.
        // If we scaled it here too, the ship would aim for a quad-scaled velocity.

        float scaledOrbitRadius = orbitRadius * orbitCenter.lossyScale.x;

        float rad = currentOrbitAngle * Mathf.Deg2Rad;
        Vector3 localVel = new Vector3(-Mathf.Sin(rad) * scaledOrbitRadius * radPerSec, 0f, Mathf.Cos(rad) * scaledOrbitRadius * radPerSec);

        Quaternion inclinationRot = Quaternion.Euler(orbitInclination, 0f, 0f);
        return inclinationRot * localVel;
    }

    public void UpdatePosition(float deltaTime)
    {
        float inGameSecondsMultiplier = GameManager.Instance != null ? GameManager.Instance.inGameSecondsPerRealSecond : 720f;
        float inGameDaysPassed = (deltaTime * inGameSecondsMultiplier) / 86400f;

        // Orbit around center
        if (orbitCenter != null && orbitRadius > 0.01f)
        {
            float orbitDeltaAngle = 0f;
            if (Mathf.Abs(orbitPeriodDays) > 0.0001f)
            {
                orbitDeltaAngle = (360f / orbitPeriodDays) * inGameDaysPassed;
            }

            currentOrbitAngle += orbitDeltaAngle;
            if (currentOrbitAngle >= 360f) currentOrbitAngle -= 360f;
            if (currentOrbitAngle < 0f) currentOrbitAngle += 360f;

            float scaledOrbitRadius = orbitRadius * orbitCenter.lossyScale.x;

            float rad = currentOrbitAngle * Mathf.Deg2Rad;
            Vector3 orbitLocal = new Vector3(Mathf.Cos(rad) * scaledOrbitRadius, 0f, Mathf.Sin(rad) * scaledOrbitRadius);
            Quaternion inclinationRot = Quaternion.Euler(orbitInclination, 0f, 0f);
            Vector3 rotatedLocal = inclinationRot * orbitLocal;

            transform.position = orbitCenter.position + rotatedLocal;
        }

        // Self rotation
        float rotationDeltaAngle = 0f;
        if (Mathf.Abs(rotationPeriodDays) > 0.0001f)
        {
            rotationDeltaAngle = (360f / rotationPeriodDays) * inGameDaysPassed;
        }

        if (visualModel != null)
        {
            visualModel.transform.Rotate(Vector3.up, rotationDeltaAngle, Space.Self);
        }
        else
        {
            transform.Rotate(Vector3.up, rotationDeltaAngle, Space.Self);
        }
    }

    public Vector3 GetApproachPosition(Vector3 incomingDirection, float offsetMultiplier = 2.5f)
    {
        Vector3 dir = (incomingDirection - transform.position).normalized;
        if (dir.sqrMagnitude < 0.001f) dir = Vector3.forward;
        // Keep a clear offset from the body's scaled surface
        float scaledRadius = bodyRadius * transform.lossyScale.x;
        float distance = Mathf.Max(scaledRadius * offsetMultiplier, 4f);
        return transform.position + dir * distance;
    }

    private void OnMouseDown()
    {
        if (SolarSystemManager.Instance != null)
        {
            SolarSystemManager.Instance.SetDestination(this);
        }
    }
}
