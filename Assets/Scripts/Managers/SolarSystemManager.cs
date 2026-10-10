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
    public static string initialDestinationName = null;
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
        CelestialBody targetBody = null;
        if (!string.IsNullOrEmpty(initialDestinationName))
        {
            targetBody = allBodies.Find(b => b.bodyName.Equals(initialDestinationName, StringComparison.OrdinalIgnoreCase) ||
                                            b.bodyName.IndexOf(initialDestinationName, StringComparison.OrdinalIgnoreCase) >= 0);
            initialDestinationName = null; // consume
        }

        CelestialBody earth = allBodies.Find(b => b.bodyName.Equals("Terre", StringComparison.OrdinalIgnoreCase) || b.bodyName.Equals("Earth", StringComparison.OrdinalIgnoreCase));

        if (targetBody != null)
        {
            SetDestination(targetBody);
        }
        else if (centralStar != null && currentDestination == null)
        {
            // Default select Earth if available, else central star
            CelestialBody dest = earth != null ? earth : centralStar;
            SetDestination(dest);
        }

        // Initialize positions of all celestial bodies first
        if (centralStar != null)
        {
            centralStar.UpdatePosition(0f);
        }
        for (int i = 0; i < allBodies.Count; i++)
        {
            if (allBodies[i] != null && allBodies[i] != centralStar)
            {
                allBodies[i].UpdatePosition(0f);
            }
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

        if (playerShip == null)
        {
            playerShip = FindAnyObjectByType<SpaceshipFlightController>();
        }

        // Force spaceship to start in orbit of the destination
        if (playerShip != null && currentDestination != null)
        {
            playerShip.WarpToDestination();
            playerShip.SetFlightMode(FlightMode.OrbitInspect);
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

    public CelestialBody GetDominantCelestialBody(Vector3 worldPos)
    {
        return GetDominantCelestialBody(worldPos, null);
    }

    public CelestialBody GetDominantCelestialBody(Vector3 worldPos, CelestialBody currentRef)
    {
        const float exitHysteresis = 1.2f;

        // 1. If currently inside a Moon / small body, stay unless outside exit SOI boundary
        if (currentRef != null && (currentRef.bodyType == CelestialBodyType.Moon || currentRef.bodyType == CelestialBodyType.Asteroid))
        {
            float distToCurrent = Vector3.Distance(currentRef.transform.position, worldPos);
            if (distToCurrent <= currentRef.GetEffectiveSOIRadius() * exitHysteresis)
            {
                return currentRef;
            }
        }

        // 2. Check Moons and Asteroids / small bodies first (nested SOIs)
        float closestMoonDist = float.MaxValue;
        CelestialBody closestMoon = null;

        for (int i = 0; i < allBodies.Count; i++)
        {
            var b = allBodies[i];
            if (b != null && (b.bodyType == CelestialBodyType.Moon || b.bodyType == CelestialBodyType.Asteroid))
            {
                float dist = Vector3.Distance(b.transform.position, worldPos);
                if (dist <= b.GetEffectiveSOIRadius() && dist < closestMoonDist)
                {
                    closestMoonDist = dist;
                    closestMoon = b;
                }
            }
        }
        if (closestMoon != null) return closestMoon;

        // 3. If currently inside a Planet, stay unless outside exit SOI boundary
        if (currentRef != null && (currentRef.bodyType == CelestialBodyType.Planet || currentRef.bodyType == CelestialBodyType.DwarfPlanet))
        {
            float distToCurrent = Vector3.Distance(currentRef.transform.position, worldPos);
            if (distToCurrent <= currentRef.GetEffectiveSOIRadius() * exitHysteresis)
            {
                return currentRef;
            }
        }

        // 4. Check Planets
        float closestPlanetDist = float.MaxValue;
        CelestialBody closestPlanet = null;

        for (int i = 0; i < allBodies.Count; i++)
        {
            var b = allBodies[i];
            if (b != null && (b.bodyType == CelestialBodyType.Planet || b.bodyType == CelestialBodyType.DwarfPlanet))
            {
                float dist = Vector3.Distance(b.transform.position, worldPos);
                if (dist <= b.GetEffectiveSOIRadius() && dist < closestPlanetDist)
                {
                    closestPlanetDist = dist;
                    closestPlanet = b;
                }
            }
        }
        if (closestPlanet != null) return closestPlanet;

        // 5. Fallback to Central Star (heliocentric reference frame)
        return centralStar;
    }
}
