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
    public float orbitSpeed = 5f; // degrees per second at 1x time scale
    public float orbitInclination = 0f;
    public float initialOrbitAngle = 0f;
    public Color orbitLineColor = new Color(0.3f, 0.6f, 1f, 0.4f);

    [Header("Rotation")]
    public float rotationSpeed = 15f; // degrees per second
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

    public void Initialize(Transform center, float radius, float oSpeed, float rSpeed, float size, Color orbitCol, float tilt = 0f, float startAngle = 0f)
    {
        orbitCenter = center;
        orbitRadius = radius;
        orbitSpeed = oSpeed;
        rotationSpeed = rSpeed;
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

        Quaternion inclinationRot = Quaternion.Euler(orbitInclination, 0f, 0f);
        Vector3[] points = new Vector3[orbitResolution];
        for (int i = 0; i < orbitResolution; i++)
        {
            float angle = (i / (float)orbitResolution) * 360f * Mathf.Deg2Rad;
            Vector3 localPos = new Vector3(Mathf.Cos(angle) * orbitRadius, 0f, Mathf.Sin(angle) * orbitRadius);
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

    public void UpdatePosition(float deltaTime)
    {
        // Orbit around center
        if (orbitCenter != null && orbitRadius > 0.01f)
        {
            currentOrbitAngle += orbitSpeed * deltaTime;
            if (currentOrbitAngle >= 360f) currentOrbitAngle -= 360f;
            if (currentOrbitAngle < 0f) currentOrbitAngle += 360f;

            float rad = currentOrbitAngle * Mathf.Deg2Rad;
            Vector3 orbitLocal = new Vector3(Mathf.Cos(rad) * orbitRadius, 0f, Mathf.Sin(rad) * orbitRadius);
            Quaternion inclinationRot = Quaternion.Euler(orbitInclination, 0f, 0f);
            Vector3 rotatedLocal = inclinationRot * orbitLocal;

            transform.position = orbitCenter.position + rotatedLocal;
        }

        // Self rotation
        if (visualModel != null)
        {
            visualModel.transform.Rotate(Vector3.up, rotationSpeed * deltaTime, Space.Self);
        }
        else
        {
            transform.Rotate(Vector3.up, rotationSpeed * deltaTime, Space.Self);
        }
    }

    public Vector3 GetApproachPosition(Vector3 incomingDirection, float offsetMultiplier = 2.5f)
    {
        Vector3 dir = (incomingDirection - transform.position).normalized;
        if (dir.sqrMagnitude < 0.001f) dir = Vector3.forward;
        // Keep a clear offset from the body's surface
        float distance = Mathf.Max(bodyRadius * offsetMultiplier, 4f);
        return transform.position + dir * distance + Vector3.up * (distance * 0.25f);
    }

    private void OnMouseDown()
    {
        if (SolarSystemManager.Instance != null)
        {
            SolarSystemManager.Instance.SetDestination(this);
        }
    }
}
