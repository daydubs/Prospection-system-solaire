using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SolarSystemBuilder : MonoBehaviour
{
    public static void BuildCompleteSolarSystem()
    {
        // 1. Clean existing solar system objects if any
        GameObject existingSystem = GameObject.Find("SolarSystem_Root");
        if (existingSystem != null)
        {
            DestroyImmediate(existingSystem);
        }

        GameObject root = new GameObject("SolarSystem_Root");

        // Ensure GameManager is present
        if (GameManager.Instance == null)
        {
            GameObject gmObj = new GameObject("GameManager");
            gmObj.AddComponent<GameManager>();
        }

        SolarSystemManager manager = root.AddComponent<SolarSystemManager>();
        SolarSystemUI ui = root.AddComponent<SolarSystemUI>();

        // 2. Space Environment (Starfield & ambient)
        CreateStarfield(root.transform);

        // 3. Materials dictionary
        Dictionary<string, Material> mats = CreatePlanetMaterials();

        // 4. Create Sun
        GameObject sunObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sunObj.name = "Soleil (Sun)";
        sunObj.transform.SetParent(root.transform, false);
        sunObj.transform.position = Vector3.zero;
        sunObj.transform.localScale = Vector3.one * 40f;
        if (mats.ContainsKey("Sun"))
            sunObj.GetComponent<Renderer>().sharedMaterial = mats["Sun"];

        CelestialBody sunBody = sunObj.AddComponent<CelestialBody>();
        sunBody.bodyName = "Soleil";
        sunBody.bodyType = CelestialBodyType.Star;
        sunBody.bodyRadius = 20f;
        sunBody.orbitRadius = 0f;
        sunBody.orbitPeriodDays = 0f;
        sunBody.rotationPeriodDays = 27f; // Sun rotation takes ~27 days
        sunBody.description = "L'étoile au centre de notre système solaire. Elle contient 99,86 % de la masse totale du système solaire.";
        sunBody.physicalCharacteristics = "• Type : Naine jaune (G2V)\n• Température de surface : 5 500 °C\n• Diamètre : 1 392 700 km\n• Âge : ~4,6 milliards d'années";

        // Sun light
        GameObject sunLightObj = new GameObject("Sun_PointLight");
        sunLightObj.transform.SetParent(sunObj.transform, false);
        Light sunLight = sunLightObj.AddComponent<Light>();
        sunLight.type = LightType.Point;
        sunLight.color = new Color(1f, 0.96f, 0.88f);
        sunLight.intensity = 250f;
        sunLight.range = 50000f;
        sunLight.shadows = LightShadows.Soft;

        manager.centralStar = sunBody;

        // 5. Create Planets and Moons (1 AU = 600 units)
        // (Name, OrbitRadius, OrbitSpeed, Size, Color, RotationSpeed, AxialTilt, MatKey, Description, Facts)

        // --- MERCURY (0.39 AU = 230 units) ---
        CelestialBody mercury = CreatePlanet(root.transform, sunObj.transform, "Mercure", 230f, 88f, 1.1f, mats["Mercury"], 58.6f, 0.03f,
            new Color(0.7f, 0.65f, 0.6f, 0.5f),
            "La planète la plus proche du Soleil et la plus petite du système solaire.",
            "• Distance moyenne : 57,9 millions km (0,39 UA)\n• Période orbitale : 88 jours\n• Satellites : 0\n• Température : -180°C à +430°C");

        // --- VENUS (0.72 AU = 430 units) ---
        CelestialBody venus = CreatePlanet(root.transform, sunObj.transform, "Vénus", 430f, 225f, 2.4f, mats["Venus"], -243f, 177.3f,
            new Color(0.9f, 0.8f, 0.4f, 0.5f),
            "Deuxième planète du système solaire, caractérisée par une atmosphère épaisse et un effet de serre extrême.",
            "• Distance moyenne : 108,2 millions km (0,72 UA)\n• Période orbitale : 225 jours\n• Satellites : 0\n• Pression au sol : 92 bars\n• Température : 465°C constante");

        // --- EARTH & MOON (1.00 AU = 600 units) ---
        CelestialBody earth = CreatePlanet(root.transform, sunObj.transform, "Terre", 600f, 365.25f, 2.5f, mats["Earth"], 1f, 23.4f,
            new Color(0.2f, 0.6f, 1f, 0.6f),
            "Notre planète d'origine, seul monde connu abritant la vie et de vastes océans d'eau liquide.",
            "• Distance moyenne : 149,6 millions km (1,0 UA)\n• Période orbitale : 365,25 jours\n• Satellites : 1 (La Lune)\n• Atmosphère : 78% Azote, 21% Oxygène");

        CelestialBody moon = CreateMoon(root.transform, earth.transform, "Lune", 25f, 27.3f, 0.7f, mats["Moon"], 27.3f,
            new Color(0.8f, 0.8f, 0.85f, 0.4f),
            "Le seul satellite naturel permanent de la Terre et le cinquième plus grand satellite du système solaire.",
            "• Distance de la Terre : 384 400 km (~2-3 jours de vol à vitesse de croisière)\n• Période orbitale : 27,3 jours\n• Gravité : 1,62 m/s² (1/6ème terrestre)");
        earth.satellites.Add(moon);

        // --- ORBITAL SPACE STATION (Earth orbit, 6.5 units) ---
        CelestialBody station = CreateOrbitalStation(root.transform, earth.transform);
        if (station != null)
        {
            earth.satellites.Add(station);
        }

        // --- MARS & MOONS (1.52 AU = 910 units) ---
        CelestialBody mars = CreatePlanet(root.transform, sunObj.transform, "Mars", 910f, 687f, 1.5f, mats["Mars"], 1.026f, 25.2f,
            new Color(1f, 0.4f, 0.2f, 0.5f),
            "La planète rouge, abritant le plus haut volcan du système solaire (Olympus Mons) et d'anciens lits de rivières asséchées.",
            "• Distance moyenne : 227,9 millions km (1,52 UA)\n• Période orbitale : 687 jours\n• Satellites : 2 (Phobos, Déimos)\n• Atmosphère : 95% CO2");

        CelestialBody phobos = CreateMoon(root.transform, mars.transform, "Phobos", 6.0f, 0.31f, 0.35f, mats["Phobos"], 0.31f,
            new Color(0.6f, 0.5f, 0.4f, 0.4f),
            "La plus grande et la plus proche des deux lunes de Mars.",
            "• Orbite très basse à seulement 6 000 km d'altitude martienne.");
        CelestialBody deimos = CreateMoon(root.transform, mars.transform, "Déimos", 10.0f, 1.26f, 0.25f, mats["Deimos"], 1.26f,
            new Color(0.55f, 0.5f, 0.45f, 0.4f),
            "La plus petite et la plus éloignée des deux lunes martiennes.",
            "• Forme irrégulière d'astéroïde capturé.");
        mars.satellites.Add(phobos);
        mars.satellites.Add(deimos);

        // --- ASTEROID BELT (2.3 to 3.1 AU = 1380 to 1860 units) ---
        CreateAsteroidBelt(root.transform, 1380f, 1860f, 400, mats["Asteroid"]);

        // --- JUPITER & MOONS (5.20 AU = 3120 units) ---
        CelestialBody jupiter = CreatePlanet(root.transform, sunObj.transform, "Jupiter", 3120f, 4333f, 8.5f, mats["Jupiter"], 0.41f, 3.1f,
            new Color(0.95f, 0.75f, 0.5f, 0.6f),
            "La plus imposante géante gazeuse du système solaire, connue pour sa Grande Tache Rouge et son puissant champ magnétique.",
            "• Distance moyenne : 778,5 millions km (5,2 UA)\n• Période orbitale : 11,86 ans\n• Satellites confirmés : 95+\n• Masse : 318 fois la Terre");

        CelestialBody io = CreateMoon(root.transform, jupiter.transform, "Io", 22f, 1.77f, 0.65f, mats["Io"], 1.77f,
            new Color(0.9f, 0.8f, 0.2f, 0.4f),
            "Le corps le plus géologiquement actif du système solaire avec plus de 400 volcans en éruption permanente.",
            "• Forces de marée extrêmes créées par Jupiter et les lunes voisines.");
        CelestialBody europa = CreateMoon(root.transform, jupiter.transform, "Europe", 32f, 3.55f, 0.6f, mats["Europa"], 3.55f,
            new Color(0.7f, 0.85f, 1f, 0.4f),
            "Monde glacé recouvert d'un immense océan liquide sous sa croûte de glace, candidat majeur pour la recherche de vie extraterrestre.",
            "• Océan sous-glaciaire profond de ~100 km.");
        CelestialBody ganymede = CreateMoon(root.transform, jupiter.transform, "Ganymède", 45f, 7.15f, 0.85f, mats["Ganymede"], 7.15f,
            new Color(0.65f, 0.6f, 0.55f, 0.4f),
            "Le plus grand satellite naturel du système solaire, plus grand que la planète Mercure.",
            "• Possède son propre champ magnétique.");
        CelestialBody callisto = CreateMoon(root.transform, jupiter.transform, "Callisto", 62f, 16.7f, 0.8f, mats["Callisto"], 16.7f,
            new Color(0.5f, 0.48f, 0.45f, 0.4f),
            "Surface fortement cratérisée et l'une des plus anciennes du système solaire.",
            "• Cratères d'impact géants dont Valhalla.");
        jupiter.satellites.Add(io);
        jupiter.satellites.Add(europa);
        jupiter.satellites.Add(ganymede);
        jupiter.satellites.Add(callisto);

        // --- SATURN & RINGS & MOONS (9.58 AU = 5750 units) ---
        CelestialBody saturn = CreatePlanet(root.transform, sunObj.transform, "Saturne", 5750f, 10759f, 7.0f, mats["Saturn"], 0.45f, 26.7f,
            new Color(0.9f, 0.85f, 0.65f, 0.6f),
            "Célèbre pour son spectaculaire et vaste système d'anneaux composés de milliards de fragments de glace et de poussières.",
            "• Distance moyenne : 1,43 milliard km (9,58 UA)\n• Période orbitale : 29,45 ans\n• Satellites confirmés : 146+\n• Densité moyenne inférieure à l'eau !");

        CreateSaturnRings(saturn.transform, 1.35f, 2.5f, mats["SaturnRings"]);

        CelestialBody titan = CreateMoon(root.transform, saturn.transform, "Titan", 42f, 15.9f, 0.8f, mats["Titan"], 15.9f,
            new Color(0.95f, 0.7f, 0.3f, 0.4f),
            "Le deuxième plus grand satellite du système solaire, le seul possédant une atmosphère dense et des lacs de méthane liquide.",
            "• Atmosphère riche en diazote (98%)\n• Présence de pluie et cycle d'hydrocarbures.");
        CelestialBody enceladus = CreateMoon(root.transform, saturn.transform, "Encelade", 25f, 1.37f, 0.45f, mats["Enceladus"], 1.37f,
            new Color(0.85f, 0.95f, 1f, 0.4f),
            "Petit satellite de glace ultra-réfléchissant éjectant des geysers d'eau salée depuis son océan sous-glaciaire.",
            "• Cryovolcanisme actif au pôle sud.");
        saturn.satellites.Add(titan);
        saturn.satellites.Add(enceladus);

        // --- URANUS & MOONS (19.2 AU = 11500 units) ---
        CelestialBody uranus = CreatePlanet(root.transform, sunObj.transform, "Uranus", 11500f, 30688f, 4.5f, mats["Uranus"], -0.72f, 97.8f,
            new Color(0.4f, 0.85f, 0.9f, 0.5f),
            "Géante de glace dont l'axe de rotation est presque totalement couché sur son plan orbital.",
            "• Distance moyenne : 2,87 milliards km (19,2 UA)\n• Période orbitale : 84 ans\n• Température atmosphérique minimale : -224°C");

        CelestialBody titania = CreateMoon(root.transform, uranus.transform, "Titania", 28f, 8.7f, 0.55f, mats["Titania"], 8.7f,
            new Color(0.6f, 0.7f, 0.75f, 0.4f),
            "La plus grande lune d'Uranus, parsemée de canyons et de vallées de failles géantes.",
            "• Découverte par William Herschel en 1787.");
        CelestialBody oberon = CreateMoon(root.transform, uranus.transform, "Obéron", 42f, 13.5f, 0.5f, mats["Oberon"], 13.5f,
            new Color(0.55f, 0.65f, 0.7f, 0.4f),
            "La deuxième plus grande lune d'Uranus et la plus externe des grandes lunes.",
            "• Surface couverte de cratères aux fonds sombres.");
        uranus.satellites.Add(titania);
        uranus.satellites.Add(oberon);

        // --- NEPTUNE & TRITON (30.1 AU = 18000 units) ---
        CelestialBody neptune = CreatePlanet(root.transform, sunObj.transform, "Neptune", 18000f, 60182f, 4.3f, mats["Neptune"], 0.67f, 28.3f,
            new Color(0.2f, 0.4f, 1f, 0.5f),
            "La planète la plus lointaine du système solaire, caractérisée par des vents supersoniques pouvant dépasser 2 000 km/h.",
            "• Distance moyenne : 4,5 milliards km (30,1 UA)\n• Période orbitale : 164,8 ans\n• Teinte bleue due à la présence de méthane");

        CelestialBody triton = CreateMoon(root.transform, neptune.transform, "Triton", 30f, -5.87f, 0.65f, mats["Triton"], 5.87f,
            new Color(0.7f, 0.75f, 0.9f, 0.4f),
            "La seule grande lune du système solaire avec une orbite rétrograde, vestige probable d'un corps capturé de la ceinture de Kuiper.",
            "• Geysers d'azote actif crachant des poussières sombres.");
        neptune.satellites.Add(triton);

        // --- PLUTO & CHARON (39.5 AU = 23700 units) ---
        CelestialBody pluto = CreatePlanet(root.transform, sunObj.transform, "Pluton", 23700f, 90560f, 0.9f, mats["Pluto"], -6.38f, 122.5f,
            new Color(0.75f, 0.65f, 0.55f, 0.4f),
            "La plus célèbre des planètes naines, au cœur de la ceinture de Kuiper avec son grand glacier d'azote en forme de cœur (Tombaugh Regio).",
            "• Distance moyenne : 5,9 milliards km (39,5 UA)\n• Période orbitale : 248 ans\n• Forme un système binaire avec Charon");
        pluto.bodyType = CelestialBodyType.DwarfPlanet;

        CelestialBody charon = CreateMoon(root.transform, pluto.transform, "Charon", 12f, 6.38f, 0.5f, mats["Charon"], 6.38f,
            new Color(0.6f, 0.58f, 0.55f, 0.4f),
            "La plus grande lune de Pluton, si massive que le barycentre du couple est situé dans l'espace entre les deux astres.",
            "• En verrouillage gravitationnel mutuel total avec Pluton.");
        pluto.satellites.Add(charon);

        // 6. Create Player Spaceship
        GameObject shipObj = CreatePlayerSpaceship(root.transform, earth.transform.position + new Vector3(6f, 2f, 6f));
        SpaceshipFlightController flightCtrl = shipObj.GetComponent<SpaceshipFlightController>();
        manager.playerShip = flightCtrl;
        ui.playerShip = flightCtrl;

        // Set Main Camera
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            mainCam = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
        }
        mainCam.farClipPlane = 60000f;
        mainCam.nearClipPlane = 0.1f;
        flightCtrl.shipCamera = mainCam;
        ui.mainCam = mainCam;

        // Scan all created celestial bodies
        manager.ScanAndRegisterBodies();
        manager.SetDestination(earth);

        Debug.Log("[SolarSystemBuilder] Solar system generated successfully with all planets, moons, asteroid belt and spaceship!");
    }

    private static CelestialBody CreatePlanet(Transform root, Transform center, string name, float orbitRadius, float orbitPeriodDays, float size, Material mat, float rotationPeriodDays, float tilt, Color orbitCol, string desc, string facts)
    {
        GameObject planetObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        planetObj.name = name;
        planetObj.transform.SetParent(root, false);
        planetObj.transform.localScale = Vector3.one * (size * 2f);
        if (mat != null) planetObj.GetComponent<Renderer>().sharedMaterial = mat;

        CelestialBody body = planetObj.AddComponent<CelestialBody>();
        body.bodyName = name;
        body.bodyType = CelestialBodyType.Planet;
        body.description = desc;
        body.physicalCharacteristics = facts;
        body.Initialize(center, orbitRadius, orbitPeriodDays, rotationPeriodDays, size, orbitCol, tilt, Random.Range(0f, 360f));

        return body;
    }

    private static CelestialBody CreateMoon(Transform root, Transform center, string name, float orbitRadius, float orbitPeriodDays, float size, Material mat, float rotationPeriodDays, Color orbitCol, string desc, string facts)
    {
        GameObject moonObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        moonObj.name = name;
        moonObj.transform.SetParent(root, false);
        moonObj.transform.localScale = Vector3.one * (size * 2f);
        if (mat != null) moonObj.GetComponent<Renderer>().sharedMaterial = mat;

        CelestialBody body = moonObj.AddComponent<CelestialBody>();
        body.bodyName = name;
        body.bodyType = CelestialBodyType.Moon;
        body.description = desc;
        body.physicalCharacteristics = facts;
        body.Initialize(center, orbitRadius, orbitPeriodDays, rotationPeriodDays, size, orbitCol, 0f, Random.Range(0f, 360f));

        return body;
    }

    private static CelestialBody CreateOrbitalStation(Transform root, Transform earthCenter)
    {
#if UNITY_EDITOR
        GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Station_Orbital.prefab");
        if (prefab != null)
        {
            GameObject stationInstance = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, root);
            stationInstance.name = "Station Orbitale Alpha";
            CelestialBody cb = stationInstance.GetComponent<CelestialBody>();
            cb.orbitCenter = earthCenter;
            cb.orbitRadius = 6.5f;
            cb.orbitPeriodDays = 15f;
            cb.rotationPeriodDays = 5f;
            cb.bodyRadius = 0.6f;
            cb.orbitLineColor = new Color(0f, 0.85f, 1f, 0.6f);
            return cb;
        }
#endif
        return null;
    }

    private static void CreateSaturnRings(Transform saturnTransform, float innerRadius, float outerRadius, Material ringMat)
    {
        GameObject ringsObj = new GameObject("Saturn_Rings");
        ringsObj.transform.SetParent(saturnTransform, false);
        ringsObj.transform.localRotation = Quaternion.Euler(26.7f, 0f, 0f);

        MeshFilter mf = ringsObj.AddComponent<MeshFilter>();
        MeshRenderer mr = ringsObj.AddComponent<MeshRenderer>();
        mr.sharedMaterial = ringMat;

        // Create ring mesh
        int segments = 80;
        Mesh mesh = new Mesh();
        mesh.name = "RingDisc";

        Vector3[] vertices = new Vector3[(segments + 1) * 2];
        Vector2[] uv = new Vector2[vertices.Length];
        int[] triangles = new int[segments * 6 * 2]; // double-sided

        for (int i = 0; i <= segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);

            vertices[i * 2] = new Vector3(cos * innerRadius, 0f, sin * innerRadius);
            vertices[i * 2 + 1] = new Vector3(cos * outerRadius, 0f, sin * outerRadius);

            float u = (float)i / segments;
            uv[i * 2] = new Vector2(u, 0f);
            uv[i * 2 + 1] = new Vector2(u, 1f);
        }

        int t = 0;
        for (int i = 0; i < segments; i++)
        {
            int i0 = i * 2;
            int i1 = i0 + 1;
            int i2 = i0 + 2;
            int i3 = i0 + 3;

            // Front face
            triangles[t++] = i0;
            triangles[t++] = i1;
            triangles[t++] = i2;

            triangles[t++] = i1;
            triangles[t++] = i3;
            triangles[t++] = i2;

            // Back face
            triangles[t++] = i2;
            triangles[t++] = i1;
            triangles[t++] = i0;

            triangles[t++] = i2;
            triangles[t++] = i3;
            triangles[t++] = i1;
        }

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        mf.sharedMesh = mesh;
    }

    private static void CreateAsteroidBelt(Transform root, float innerRadius, float outerRadius, int count, Material asteroidMat)
    {
        GameObject beltObj = new GameObject("Asteroid_Belt");
        beltObj.transform.SetParent(root, false);

        for (int i = 0; i < count; i++)
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float radius = Random.Range(innerRadius, outerRadius);
            float height = Random.Range(-25f, 25f);

            Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, height, Mathf.Sin(angle) * radius);
            GameObject ast = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ast.name = $"Asteroid_{i}";
            ast.transform.SetParent(beltObj.transform, false);
            ast.transform.position = pos;

            float scale = Random.Range(0.6f, 2.2f);
            ast.transform.localScale = new Vector3(
                scale * Random.Range(0.8f, 1.3f),
                scale * Random.Range(0.7f, 1.2f),
                scale * Random.Range(0.8f, 1.3f)
            );
            ast.transform.rotation = Random.rotation;

            if (asteroidMat != null) ast.GetComponent<Renderer>().sharedMaterial = asteroidMat;
        }
    }

    private static void CreateStarfield(Transform root)
    {
        GameObject starfieldObj = new GameObject("Deep_Starfield");
        starfieldObj.transform.SetParent(root, false);

        ParticleSystem ps = starfieldObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.maxParticles = 2500;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.enabled = false;

        ParticleSystem.Particle[] particles = new ParticleSystem.Particle[2500];
        for (int i = 0; i < 2500; i++)
        {
            Vector3 dir = Random.onUnitSphere;
            float dist = Random.Range(30000f, 55000f);
            particles[i].position = dir * dist;
            particles[i].startSize = Random.Range(80f, 240f);

            float hue = Random.Range(0.55f, 0.65f); // Cool white/blue and faint golden stars
            if (Random.value < 0.25f) hue = Random.Range(0.08f, 0.15f);
            Color starCol = Color.HSVToRGB(hue, Random.Range(0.1f, 0.4f), Random.Range(0.75f, 1f));
            particles[i].startColor = starCol;
        }

        ps.SetParticles(particles, 2500);
    }

    private static GameObject CreatePlayerSpaceship(Transform root, Vector3 initialPos)
    {
#if UNITY_EDITOR
        GameObject shipPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Spaceship_Vessel.prefab");
        if (shipPrefab != null)
        {
            GameObject shipInstance = UnityEditor.PrefabUtility.InstantiatePrefab(shipPrefab, root) as GameObject;
            shipInstance.name = "Spaceship_Explorer";
            shipInstance.transform.position = initialPos;
            shipInstance.transform.rotation = Quaternion.LookRotation(Vector3.forward);
            return shipInstance;
        }

        // Modular FBX meshes fallback if prefab is not available
        GameObject c2Prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/models/Cockpit2.fbx");
        GameObject cmdPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/models/Command.fbx");
        GameObject wnePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/models/WingsNEngine_low.fbx");

        if (c2Prefab != null && cmdPrefab != null && wnePrefab != null)
        {
            GameObject ship = new GameObject("Spaceship_Explorer");
            ship.transform.SetParent(root, false);
            ship.transform.position = initialPos;
            ship.transform.rotation = Quaternion.LookRotation(Vector3.forward);

            var ctrlComp = ship.AddComponent<SpaceshipFlightController>();
            ctrlComp.normalSpeed = 0.08f;
            ctrlComp.boostMultiplier = 3.5f;
            ctrlComp.cameraOffset = new Vector3(0f, 2.2f, -6.8f);
            ctrlComp.firstPersonCameraOffset = new Vector3(0.06f, -0.11f, 1.61f);
            ctrlComp.thirdPersonCameraOffset = new Vector3(0f, 2f, -10f);

            var boxCol = ship.AddComponent<BoxCollider>();
            boxCol.center = Vector3.zero;
            boxCol.size = new Vector3(5f, 2f, 5.7f);

            GameObject model = new GameObject("Model");
            model.transform.SetParent(ship.transform, false);
            model.transform.localPosition = new Vector3(0f, -0.99f, 0.80f);

            GameObject c2 = UnityEditor.PrefabUtility.InstantiatePrefab(c2Prefab, model.transform) as GameObject;
            GameObject cmd = UnityEditor.PrefabUtility.InstantiatePrefab(cmdPrefab, model.transform) as GameObject;
            GameObject wne = UnityEditor.PrefabUtility.InstantiatePrefab(wnePrefab, model.transform) as GameObject;

            c2.name = "Cockpit2";
            cmd.name = "Command";
            wne.name = "WingsNEngine";

            c2.transform.localPosition = new Vector3(0f, 1f, 0f);
            c2.transform.localRotation = Quaternion.Euler(270.02f, 0f, 0f);
            c2.transform.localScale = new Vector3(100f, 100f, 100f);

            cmd.transform.localPosition = new Vector3(0f, 1f, 0f);
            cmd.transform.localRotation = Quaternion.Euler(270.02f, 0f, 0f);
            cmd.transform.localScale = new Vector3(100f, 100f, 100f);

            wne.transform.localPosition = new Vector3(0f, 1f, -1.79f);
            wne.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            wne.transform.localScale = new Vector3(100f, 100f, 100f);

            GameObject lightObj = new GameObject("Ship_Headlight");
            lightObj.transform.SetParent(ship.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 0.2f, 2.9f);
            Light spot = lightObj.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.color = new Color(0.85f, 0.95f, 1f);
            spot.intensity = 25f;
            spot.range = 200f;
            spot.spotAngle = 60f;

            GameObject mount = new GameObject("CameraMount");
            mount.transform.SetParent(ship.transform, false);
            mount.transform.localPosition = new Vector3(0f, 2.2f, -6.8f);
            ctrlComp.cameraMountPoint = mount.transform;

            return ship;
        }
#endif

        // Primitive fallback
        GameObject fallbackShip = new GameObject("Spaceship_Explorer");
        fallbackShip.transform.SetParent(root, false);
        fallbackShip.transform.position = initialPos;
        fallbackShip.transform.rotation = Quaternion.LookRotation(Vector3.forward);
        fallbackShip.AddComponent<SpaceshipFlightController>();
        return fallbackShip;
    }

    private static Dictionary<string, Material> CreatePlanetMaterials()
    {
        Dictionary<string, Material> dict = new Dictionary<string, Material>();
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        Shader urpUnlit = Shader.Find("Universal Render Pipeline/Unlit");
        if (urpLit == null) urpLit = Shader.Find("Standard");
        if (urpUnlit == null) urpUnlit = Shader.Find("Unlit/Color");

        // Sun (Emissive glowing star)
        Material sunMat = new Material(urpUnlit != null ? urpUnlit : urpLit);
        Texture2D sunTex = GenerateBandedTexture(256, 128, new Color(1f, 0.9f, 0.2f), new Color(1f, 0.4f, 0.05f), 12, 0.4f);
        sunMat.mainTexture = sunTex;
        sunMat.color = new Color(1.2f, 0.9f, 0.4f);
        dict["Sun"] = sunMat;

        // Mercury
        Material mercMat = new Material(urpLit);
        mercMat.mainTexture = GenerateNoiseTexture(256, 128, new Color(0.6f, 0.58f, 0.55f), new Color(0.35f, 0.33f, 0.32f), 8f);
        mercMat.SetFloat("_Smoothness", 0.15f);
        dict["Mercury"] = mercMat;

        // Venus
        Material venusMat = new Material(urpLit);
        venusMat.mainTexture = GenerateBandedTexture(256, 128, new Color(0.92f, 0.82f, 0.55f), new Color(0.75f, 0.6f, 0.35f), 18, 0.3f);
        venusMat.SetFloat("_Smoothness", 0.4f);
        dict["Venus"] = venusMat;

        // Earth
        Material earthMat = new Material(urpLit);
        earthMat.mainTexture = GenerateEarthTexture(512, 256);
        earthMat.SetFloat("_Smoothness", 0.5f);
        dict["Earth"] = earthMat;

        // Moon
        Material moonMat = new Material(urpLit);
        moonMat.mainTexture = GenerateNoiseTexture(256, 128, new Color(0.75f, 0.75f, 0.78f), new Color(0.4f, 0.4f, 0.42f), 12f);
        moonMat.SetFloat("_Smoothness", 0.1f);
        dict["Moon"] = moonMat;

        // Mars
        Material marsMat = new Material(urpLit);
        marsMat.mainTexture = GenerateBandedTexture(256, 128, new Color(0.85f, 0.38f, 0.18f), new Color(0.55f, 0.22f, 0.1f), 10, 0.5f);
        marsMat.SetFloat("_Smoothness", 0.2f);
        dict["Mars"] = marsMat;

        // Phobos / Deimos / Asteroid
        Material astMat = new Material(urpLit);
        astMat.mainTexture = GenerateNoiseTexture(128, 64, new Color(0.45f, 0.4f, 0.38f), new Color(0.25f, 0.22f, 0.2f), 6f);
        dict["Phobos"] = astMat;
        dict["Deimos"] = astMat;
        dict["Asteroid"] = astMat;

        // Jupiter
        Material jupMat = new Material(urpLit);
        jupMat.mainTexture = GenerateJupiterTexture(512, 256);
        jupMat.SetFloat("_Smoothness", 0.35f);
        dict["Jupiter"] = jupMat;

        // Jupiter Moons
        Material ioMat = new Material(urpLit);
        ioMat.mainTexture = GenerateNoiseTexture(128, 64, new Color(0.95f, 0.85f, 0.25f), new Color(0.8f, 0.3f, 0.1f), 10f);
        dict["Io"] = ioMat;

        Material euroMat = new Material(urpLit);
        euroMat.mainTexture = GenerateNoiseTexture(128, 64, new Color(0.9f, 0.95f, 1f), new Color(0.6f, 0.5f, 0.4f), 15f);
        dict["Europa"] = euroMat;

        Material ganMat = new Material(urpLit);
        ganMat.mainTexture = GenerateNoiseTexture(128, 64, new Color(0.6f, 0.55f, 0.5f), new Color(0.35f, 0.3f, 0.28f), 8f);
        dict["Ganymede"] = ganMat;

        Material calMat = new Material(urpLit);
        calMat.mainTexture = GenerateNoiseTexture(128, 64, new Color(0.45f, 0.42f, 0.4f), new Color(0.25f, 0.23f, 0.22f), 10f);
        dict["Callisto"] = calMat;

        // Saturn
        Material satMat = new Material(urpLit);
        satMat.mainTexture = GenerateBandedTexture(512, 256, new Color(0.92f, 0.85f, 0.62f), new Color(0.72f, 0.62f, 0.45f), 24, 0.2f);
        satMat.SetFloat("_Smoothness", 0.35f);
        dict["Saturn"] = satMat;

        // Saturn Rings (translucent striped)
        Material ringMat = new Material(urpUnlit != null ? urpUnlit : urpLit);
        ringMat.mainTexture = GenerateRingTexture(256, 32);
        dict["SaturnRings"] = ringMat;

        // Titan
        Material titanMat = new Material(urpLit);
        titanMat.mainTexture = GenerateBandedTexture(128, 64, new Color(0.95f, 0.68f, 0.25f), new Color(0.8f, 0.5f, 0.15f), 8, 0.15f);
        dict["Titan"] = titanMat;

        // Enceladus
        Material encMat = new Material(urpLit);
        encMat.mainTexture = GenerateNoiseTexture(128, 64, new Color(0.95f, 0.98f, 1f), new Color(0.75f, 0.85f, 0.95f), 6f);
        dict["Enceladus"] = encMat;

        // Uranus
        Material uranMat = new Material(urpLit);
        uranMat.mainTexture = GenerateBandedTexture(256, 128, new Color(0.55f, 0.88f, 0.92f), new Color(0.35f, 0.72f, 0.8f), 14, 0.1f);
        uranMat.SetFloat("_Smoothness", 0.4f);
        dict["Uranus"] = uranMat;

        Material titaniaMat = new Material(urpLit);
        titaniaMat.mainTexture = GenerateNoiseTexture(128, 64, new Color(0.65f, 0.68f, 0.7f), new Color(0.4f, 0.42f, 0.45f), 8f);
        dict["Titania"] = titaniaMat;
        dict["Oberon"] = titaniaMat;

        // Neptune
        Material nepMat = new Material(urpLit);
        nepMat.mainTexture = GenerateBandedTexture(256, 128, new Color(0.2f, 0.42f, 0.95f), new Color(0.1f, 0.25f, 0.7f), 16, 0.25f);
        nepMat.SetFloat("_Smoothness", 0.45f);
        dict["Neptune"] = nepMat;

        Material tritonMat = new Material(urpLit);
        tritonMat.mainTexture = GenerateNoiseTexture(128, 64, new Color(0.75f, 0.8f, 0.9f), new Color(0.5f, 0.45f, 0.55f), 9f);
        dict["Triton"] = tritonMat;

        // Pluto & Charon
        Material plutoMat = new Material(urpLit);
        plutoMat.mainTexture = GenerateNoiseTexture(128, 64, new Color(0.78f, 0.68f, 0.55f), new Color(0.4f, 0.32f, 0.25f), 10f);
        dict["Pluto"] = plutoMat;
        dict["Charon"] = plutoMat;

        return dict;
    }

    private static Texture2D GenerateNoiseTexture(int width, int height, Color colorA, Color colorB, float scale)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        Color[] pixels = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float u = (float)x / width;
                float v = (float)y / height;
                float n = Mathf.PerlinNoise(u * scale, v * scale);
                pixels[y * width + x] = Color.Lerp(colorA, colorB, n);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private static Texture2D GenerateBandedTexture(int width, int height, Color colorA, Color colorB, int bands, float noiseAmp)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        Color[] pixels = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            float v = (float)y / height;
            for (int x = 0; x < width; x++)
            {
                float u = (float)x / width;
                float wave = Mathf.Sin(v * bands * Mathf.PI) * 0.5f + 0.5f;
                float noise = Mathf.PerlinNoise(u * 6f, v * 6f) * noiseAmp;
                float blend = Mathf.Clamp01(wave + noise - (noiseAmp * 0.5f));
                pixels[y * width + x] = Color.Lerp(colorA, colorB, blend);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private static Texture2D GenerateJupiterTexture(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        Color[] pixels = new Color[width * height];
        Color darkBand = new Color(0.6f, 0.35f, 0.2f);
        Color lightBand = new Color(0.92f, 0.85f, 0.72f);
        Color redSpotCol = new Color(0.85f, 0.28f, 0.15f);

        for (int y = 0; y < height; y++)
        {
            float v = (float)y / height;
            for (int x = 0; x < width; x++)
            {
                float u = (float)x / width;
                float wave = Mathf.Sin(v * 26f * Mathf.PI) * 0.5f + 0.5f;
                float turbulence = Mathf.PerlinNoise(u * 12f, v * 14f) * 0.4f;
                float t = Mathf.Clamp01(wave + turbulence - 0.2f);
                Color baseCol = Color.Lerp(lightBand, darkBand, t);

                // Great Red Spot at ~ (u=0.6, v=0.35)
                float spotDist = Mathf.Sqrt(Mathf.Pow((u - 0.6f) * 2.5f, 2f) + Mathf.Pow((v - 0.35f) * 6f, 2f));
                if (spotDist < 0.25f)
                {
                    float spotBlend = Mathf.Clamp01((0.25f - spotDist) / 0.25f);
                    baseCol = Color.Lerp(baseCol, redSpotCol, spotBlend * 0.85f);
                }

                pixels[y * width + x] = baseCol;
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private static Texture2D GenerateEarthTexture(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        Color[] pixels = new Color[width * height];
        Color ocean = new Color(0.08f, 0.32f, 0.75f);
        Color shallowOcean = new Color(0.12f, 0.48f, 0.85f);
        Color landGreen = new Color(0.2f, 0.55f, 0.2f);
        Color landBrown = new Color(0.55f, 0.45f, 0.25f);
        Color ice = new Color(0.92f, 0.95f, 1f);

        for (int y = 0; y < height; y++)
        {
            float v = (float)y / height;
            for (int x = 0; x < width; x++)
            {
                float u = (float)x / width;
                float n = Mathf.PerlinNoise(u * 4.5f, v * 4.5f) + Mathf.PerlinNoise(u * 9f, v * 9f) * 0.3f;

                Color c;
                // Polar caps
                if (v > 0.88f || v < 0.12f)
                {
                    c = ice;
                }
                else if (n < 0.55f)
                {
                    c = n < 0.42f ? ocean : shallowOcean;
                }
                else
                {
                    c = n > 0.72f ? landBrown : landGreen;
                }

                // Swirly clouds
                float cloudN = Mathf.PerlinNoise((u + 0.3f) * 8f, v * 6f);
                if (cloudN > 0.62f)
                {
                    c = Color.Lerp(c, Color.white, (cloudN - 0.62f) * 2.2f);
                }

                pixels[y * width + x] = c;
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private static Texture2D GenerateRingTexture(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.ARGB32, false);
        Color[] pixels = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            float v = (float)y / height; // radius from inner (0) to outer (1)
            for (int x = 0; x < width; x++)
            {
                // Concentric ring grooves and Cassini division gap
                float ringStripe = Mathf.Sin(v * 45f * Mathf.PI) * 0.5f + 0.5f;
                float alpha = Mathf.Sin(v * Mathf.PI) * 0.8f; // fade at edges

                // Cassini division gap at v ~= 0.65 to 0.7
                if (v >= 0.62f && v <= 0.68f)
                {
                    alpha *= 0.1f;
                }

                Color col = Color.Lerp(new Color(0.85f, 0.78f, 0.62f, alpha), new Color(0.6f, 0.52f, 0.38f, alpha * 0.8f), ringStripe);
                pixels[y * width + x] = col;
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }
}
