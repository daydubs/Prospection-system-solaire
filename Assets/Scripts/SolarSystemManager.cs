using System;
using System.Collections.Generic;
using UnityEngine;

public class SolarSystemManager : MonoBehaviour
{
    public static SolarSystemManager Instance { get; private set; }

    [Header("Simulation Settings")]
    [Range(0f, 100f)]
    public float timeScale = 1.0f;
    public bool isPaused = false;
    public bool showOrbitLines = true;

    [Header("Hierarchy / Celestial Bodies")]
    public CelestialBody centralStar;
    public List<CelestialBody> planets = new List<CelestialBody>();
    public List<CelestialBody> allBodies = new List<CelestialBody>();

    [Header("Current Targets")]
    public CelestialBody currentDestination;
    public CelestialBody currentFocusBody;

    [Header("Player & Flight Reference")]
    public SpaceshipFlightController playerShip;

    public event Action<CelestialBody> OnDestinationSelected;
    public event Action<CelestialBody> OnFocusBodyChanged;
    public event Action<float> OnTimeScaleChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        ScanAndRegisterBodies();
    }

    private void Start()
    {
        if (centralStar != null && currentDestination == null)
        {
            // Default select Earth if available, else central star
            CelestialBody earth = allBodies.Find(b => b.bodyName.Equals("Terre", StringComparison.OrdinalIgnoreCase) || b.bodyName.Equals("Earth", StringComparison.OrdinalIgnoreCase));
            CelestialBody dest = earth != null ? earth : centralStar;
            SetDestination(dest);

            // Force spaceship to start in orbit of the initial destination
            if (playerShip != null)
            {
                playerShip.WarpToDestination();
            }

            // Fix Station Orbitale Alpha orbit if present
            if (earth != null)
            {
                CelestialBody station = allBodies.Find(b => b.bodyName.Equals("Station Orbitale Alpha", StringComparison.OrdinalIgnoreCase));
                if (station != null)
                {
                    station.orbitCenter = earth.transform;
                    if (!earth.satellites.Contains(station))
                    {
                        earth.satellites.Add(station);
                    }
                    station.UpdatePosition(0f);
                }
            }
        }
    }

    public void ScanAndRegisterBodies()
    {
        allBodies.Clear();
        CelestialBody[] found = FindObjectsByType<CelestialBody>(FindObjectsInactive.Exclude);
        allBodies.AddRange(found);

        planets.Clear();
        foreach (var body in allBodies)
        {
            if (body.bodyType == CelestialBodyType.Planet)
            {
                planets.Add(body);
            }
            if (body.bodyType == CelestialBodyType.Star && centralStar == null)
            {
                centralStar = body;
            }
        }
    }

    private void Update()
    {
        float effectiveDeltaTime = isPaused ? 0f : Time.deltaTime * timeScale;

        // Update central star rotation
        if (centralStar != null)
        {
            centralStar.UpdatePosition(effectiveDeltaTime);
        }

        // Update all planets and satellites
        for (int i = 0; i < allBodies.Count; i++)
        {
            if (allBodies[i] != null && allBodies[i] != centralStar)
            {
                allBodies[i].UpdatePosition(effectiveDeltaTime);
            }
        }
    }

    public void SetDestination(CelestialBody body)
    {
        if (body == null) return;
        currentDestination = body;
        OnDestinationSelected?.Invoke(body);
        Debug.Log($"[SolarSystem] Destination set to: {body.bodyName} ({body.bodyType})");
    }

    public void SetDestinationByName(string name)
    {
        CelestialBody target = allBodies.Find(b => b.bodyName.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (target != null)
        {
            SetDestination(target);
        }
    }

    public void SetFocusBody(CelestialBody body)
    {
        currentFocusBody = body;
        OnFocusBodyChanged?.Invoke(body);
    }

    public void SetTimeScale(float newScale)
    {
        timeScale = Mathf.Clamp(newScale, 0f, 100f);
        OnTimeScaleChanged?.Invoke(timeScale);
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
    }

    public void ToggleOrbitLines(bool show)
    {
        showOrbitLines = show;
        foreach (var body in allBodies)
        {
            if (body != null)
            {
                body.SetOrbitLineVisibility(showOrbitLines);
            }
        }
    }

    public List<CelestialBody> GetMoonsForPlanet(CelestialBody planet)
    {
        List<CelestialBody> moons = new List<CelestialBody>();
        if (planet == null) return moons;
        
        foreach (var body in allBodies)
        {
            if (body.bodyType == CelestialBodyType.Moon && body.orbitCenter == planet.transform)
            {
                moons.Add(body);
            }
        }
        return moons;
    }
}
