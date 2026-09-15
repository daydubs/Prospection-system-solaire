using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum GameLocationState
{
    EarthBase,
    SpaceFlight
}

public class EarthBaseController : MonoBehaviour
{
    private static EarthBaseController instance;
    public static EarthBaseController Instance
    {
        get
        {
            if (instance == null) instance = FindAnyObjectByType<EarthBaseController>();
            return instance;
        }
        private set => instance = value;
    }

    [Header("Base State")]
    public GameLocationState currentLocation = GameLocationState.EarthBase;
    public bool isHubUIOpen = false;
    public int hubCurrentTab = 0; // 0: Station, 1: Vaisseau, 2: Lancement, 3: Corporation

    [Header("References")]
    public Transform hubScreenTransform;
    public PlayerBaseController playerController;
    public Camera baseCamera;
    public Camera spaceCamera;
    public GameObject baseEnvironmentRoot;
    public GameObject spaceshipHangarModel;
    public GameObject spaceStationHoloModel;

    [Header("Space Station Construction")]
    public bool isCoreBuilt = false;
    public bool isSolarPanelsBuilt = false;
    public bool isScienceLabBuilt = false;
    public bool isDockingBayBuilt = false;
    public bool isStationLaunched = false;
    public GameObject orbitalStationPrefab;
    public CelestialBody earthCelestialBody;

    [Header("Spaceship Modular Parts")]
    public bool hasCockpit = true;
    public bool hasHullChassis = true;
    public bool hasIonEngines = true;
    public bool hasMiningLaser = false;
    public bool hasShieldGenerators = false;
    public bool hasExtraCargoBay = false;
    public bool isSpaceshipAssembled = false;

    [Header("Modular Mesh References in Hangar")]
    public GameObject shipCockpitPart;
    public GameObject shipHullPart;
    public GameObject shipWingsEnginesPart;
    public GameObject shipMiningLaserPart;
    public GameObject shipShieldEmitterPart;
    public GameObject shipExtraCargoPart;

    [Header("UI Styling")]
    public Color primaryColor = new Color(0f, 0.85f, 1f, 1f);
    public Color accentColor = new Color(1f, 0.75f, 0.1f, 1f);
    public Color successColor = new Color(0.2f, 0.9f, 0.4f, 1f);
    public Color warningColor = new Color(1f, 0.35f, 0.2f, 1f);
    public Color hubBgColor = new Color(0.04f, 0.08f, 0.16f, 0.96f);

    private GUIStyle headerStyle;
    private GUIStyle titleStyle;
    private GUIStyle bodyStyle;
    private GUIStyle buttonStyle;
    private GUIStyle activeBtnStyle;
    private GUIStyle cardBoxStyle;
    private GUIStyle badgeStyle;
    private Texture2D panelTex;
    private Texture2D activeBtnTex;
    private Texture2D normalBtnTex;
    private Texture2D cardTex;

    private Vector2 stationScroll;
    private Vector2 shipScroll;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    private void Start()
    {
        UpdateHangarShipVisuals();
        
        // Find Earth reference
        if (earthCelestialBody == null && SolarSystemManager.Instance != null)
        {
            earthCelestialBody = SolarSystemManager.Instance.allBodies.Find(b => b.bodyName.Equals("Terre", StringComparison.OrdinalIgnoreCase));
        }

        // Initially in Earth Base mode
        SetLocationState(GameLocationState.EarthBase);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            // If near screen or already open, toggle
            if (playerController != null && playerController.isNearHubScreen && !isHubUIOpen)
            {
                ToggleHubUI();
            }
        }

        // Close on Escape if open
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isHubUIOpen)
            {
                SetHubUIVisibility(false);
            }
        }
    }

    public void ToggleHubUI()
    {
        SetHubUIVisibility(!isHubUIOpen);
    }

    public void SetHubUIVisibility(bool open)
    {
        isHubUIOpen = open;
        if (open)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            if (currentLocation == GameLocationState.EarthBase && (MainMenuController.Instance == null || !MainMenuController.Instance.isMenuOpen))
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }

    public void SetLocationState(GameLocationState state)
    {
        currentLocation = state;

        if (spaceCamera == null)
        {
            GameObject mainCamObj = GameObject.Find("Main Camera");
            if (mainCamObj != null) spaceCamera = mainCamObj.GetComponent<Camera>();
        }

        if (state == GameLocationState.EarthBase)
        {
            if (baseEnvironmentRoot != null) baseEnvironmentRoot.SetActive(true);

            if (playerController != null)
            {
                playerController.gameObject.SetActive(true);

                // Re-sync player physics transform to ensure CharacterController doesn't glitch through floor
                // if it was disabled and re-enabled at a high negative Y offset
                Physics.SyncTransforms();

                playerController.canMove = true;
            }

            if (baseCamera != null)
            {
                baseCamera.gameObject.SetActive(true);
                baseCamera.enabled = true;
                baseCamera.tag = "MainCamera";

                var baseListener = baseCamera.GetComponent<AudioListener>();
                if (baseListener != null) baseListener.enabled = true;
            }

            if (spaceCamera != null && spaceCamera != baseCamera)
            {
                spaceCamera.tag = "Untagged";
                spaceCamera.enabled = false;

                var spaceListener = spaceCamera.GetComponent<AudioListener>();
                if (spaceListener != null) spaceListener.enabled = false;
            }

            // Pause / hide ship in flight
            if (SolarSystemManager.Instance != null && SolarSystemManager.Instance.playerShip != null)
            {
                SolarSystemManager.Instance.playerShip.gameObject.SetActive(false);
            }

            if (MainMenuController.Instance == null || !MainMenuController.Instance.isMenuOpen)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
        else // Space Flight
        {
            SetHubUIVisibility(false);

            if (playerController != null)
            {
                playerController.canMove = false;
                playerController.gameObject.SetActive(false);
            }

            if (baseCamera != null)
            {
                baseCamera.tag = "Untagged";
                baseCamera.enabled = false;

                var baseListener = baseCamera.GetComponent<AudioListener>();
                if (baseListener != null) baseListener.enabled = false;
            }

            if (baseEnvironmentRoot != null) baseEnvironmentRoot.SetActive(false);

            if (spaceCamera != null)
            {
                spaceCamera.tag = "MainCamera";
                spaceCamera.enabled = true;
                spaceCamera.gameObject.SetActive(true);

                var spaceListener = spaceCamera.GetComponent<AudioListener>();
                if (spaceListener != null) spaceListener.enabled = true;
            }

            if (SolarSystemManager.Instance != null && SolarSystemManager.Instance.playerShip != null)
            {
                var ship = SolarSystemManager.Instance.playerShip;
                ship.gameObject.SetActive(true);
                ship.enabled = true;

                // Position ship near Earth in orbit
                if (earthCelestialBody != null)
                {
                    ship.transform.position = earthCelestialBody.transform.position + new Vector3(8f, 3f, 8f);
                    ship.transform.LookAt(earthCelestialBody.transform.position);
                }
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    #region Modular Ship Construction

    public void BuildShipPart(string partKey)
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        switch (partKey)
        {
            case "mining_laser":
                if (!hasMiningLaser && gm.SpendCredits(5000, "Construction Laser Minier"))
                {
                    hasMiningLaser = true;
                    gm.shipStats.miningLaserPower += 15f;
                    UpdateHangarShipVisuals();
                }
                break;

            case "shield":
                if (!hasShieldGenerators && gm.SpendCredits(8000, "Installation Boucliers Déflecteurs"))
                {
                    hasShieldGenerators = true;
                    gm.shipStats.maxShield += 50f;
                    gm.shipStats.currentShield += 50f;
                    UpdateHangarShipVisuals();
                }
                break;

            case "cargo":
                if (!hasExtraCargoBay && gm.SpendCredits(6000, "Ajout Module Soute Lourde"))
                {
                    hasExtraCargoBay = true;
                    gm.shipStats.maxCargoCapacity += 50f;
                    UpdateHangarShipVisuals();
                }
                break;
        }

        CheckShipReadiness();
    }

    private void CheckShipReadiness()
    {
        isSpaceshipAssembled = hasCockpit && hasHullChassis && hasIonEngines;
    }

    public void UpdateHangarShipVisuals()
    {
        if (shipCockpitPart != null) shipCockpitPart.SetActive(hasCockpit);
        if (shipHullPart != null) shipHullPart.SetActive(hasHullChassis);
        if (shipWingsEnginesPart != null) shipWingsEnginesPart.SetActive(hasIonEngines);
        if (shipMiningLaserPart != null) shipMiningLaserPart.SetActive(hasMiningLaser);
        if (shipShieldEmitterPart != null) shipShieldEmitterPart.SetActive(hasShieldGenerators);
        if (shipExtraCargoPart != null) shipExtraCargoPart.SetActive(hasExtraCargoBay);

        CheckShipReadiness();
    }

    #endregion

    #region Space Station Construction & Launch

    public void BuildStationModule(string moduleKey)
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        switch (moduleKey)
        {
            case "core":
                if (!isCoreBuilt && gm.SpendCredits(10000, "Module Central Station Spatiale"))
                {
                    isCoreBuilt = true;
                    gm.playerStats.AddXP(250);
                }
                break;

            case "solar":
                if (!isSolarPanelsBuilt && gm.SpendCredits(6000, "Panneaux Solaires Station Spatiale"))
                {
                    isSolarPanelsBuilt = true;
                    gm.playerStats.AddXP(150);
                }
                break;

            case "lab":
                if (!isScienceLabBuilt && gm.SpendCredits(8000, "Laboratoire de Recherche Orbitale"))
                {
                    isScienceLabBuilt = true;
                    gm.playerStats.AddXP(200);
                }
                break;

            case "dock":
                if (!isDockingBayBuilt && gm.SpendCredits(7000, "Baie d'Amarrage & Logistique"))
                {
                    isDockingBayBuilt = true;
                    gm.playerStats.AddXP(200);
                }
                break;
        }
    }

    public bool IsStationReadyToLaunch => isCoreBuilt && isSolarPanelsBuilt;

    public void LaunchStationToOrbit()
    {
        if (!IsStationReadyToLaunch || isStationLaunched) return;

        isStationLaunched = true;
        GameManager gm = GameManager.Instance;
        if (gm != null)
        {
            gm.playerStats.AddXP(500);
            gm.playerStats.repEarthCoalition += 15f;
            gm.RegisterDiscoveredBody("Station Orbitale Alpha");
        }

        SpawnOrbitalStationInSolarSystem();
        Debug.Log("[EarthBase] 🛰️ La Station Spatiale Terrestre 'Station Orbitale Alpha' a été déployée en orbite avec succès !");
    }

    private void SpawnOrbitalStationInSolarSystem()
    {
        if (earthCelestialBody == null && SolarSystemManager.Instance != null)
        {
            earthCelestialBody = SolarSystemManager.Instance.allBodies.Find(b => b.bodyName.Equals("Terre", StringComparison.OrdinalIgnoreCase));
        }

        if (earthCelestialBody == null) return;

        // Check if station already exists
        Transform existingStation = earthCelestialBody.transform.parent != null 
            ? earthCelestialBody.transform.parent.Find("Station Orbitale Alpha") 
            : GameObject.Find("Station Orbitale Alpha")?.transform;

        if (existingStation != null)
        {
            existingStation.gameObject.SetActive(true);
            return;
        }

        // Create 3D Space Station GameObject in Earth Orbit
        Transform parentTransform = earthCelestialBody.transform.parent != null ? earthCelestialBody.transform.parent : earthCelestialBody.transform;
        GameObject stationObj = new GameObject("Station Orbitale Alpha");
        stationObj.transform.SetParent(parentTransform, false);

        // Station Central Core
        GameObject coreObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        coreObj.name = "Station_Core";
        coreObj.transform.SetParent(stationObj.transform, false);
        coreObj.transform.localScale = new Vector3(0.8f, 0.6f, 0.8f);

        // Station Ring / Habitat Wheel
        GameObject ringObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ringObj.name = "Station_Ring";
        ringObj.transform.SetParent(stationObj.transform, false);
        ringObj.transform.localScale = new Vector3(2.5f, 0.1f, 2.5f);

        // Solar Array Wings
        GameObject solarLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
        solarLeft.name = "Solar_Left";
        solarLeft.transform.SetParent(stationObj.transform, false);
        solarLeft.transform.localPosition = new Vector3(-2f, 0f, 0f);
        solarLeft.transform.localScale = new Vector3(1.8f, 0.05f, 0.6f);

        GameObject solarRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
        solarRight.name = "Solar_Right";
        solarRight.transform.SetParent(stationObj.transform, false);
        solarRight.transform.localPosition = new Vector3(2f, 0f, 0f);
        solarRight.transform.localScale = new Vector3(1.8f, 0.05f, 0.6f);

        // Add CelestialBody component to make it targetable and orbiting
        CelestialBody stationBody = stationObj.AddComponent<CelestialBody>();
        stationBody.bodyName = "Station Orbitale Alpha";
        stationBody.bodyType = CelestialBodyType.Moon; // Orbits Earth
        stationBody.orbitRadius = 4.2f;
        stationBody.orbitPeriodDays = 0.063f; // ~90 minutes orbit
        stationBody.bodyRadius = 0.5f;
        stationBody.rotationPeriodDays = 0.063f;
        stationBody.orbitCenter = earthCelestialBody.transform;
        stationBody.description = "La station spatiale orbitale terrestre construite et déployée par votre corporation. Hub de ravitaillement et de recherche scientifique de pointe.";
        stationBody.physicalCharacteristics = "• Altitude orbitale : 420 km\n• Énergie : Panneaux solaires photovoltaïques\n• Modules actifs : Laboratoire & Baie d'amarrage";

        earthCelestialBody.satellites.Add(stationBody);
        if (SolarSystemManager.Instance != null)
        {
            SolarSystemManager.Instance.allBodies.Add(stationBody);
        }
    }

    public void LaunchMissionToSpace()
    {
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadSolarSystem();
        }
        else
        {
            SetLocationState(GameLocationState.SpaceFlight);
        }
    }

    #endregion

    #region OnGUI Base Hub

    private void InitStyles()
    {
        if (panelTex == null)
        {
            panelTex = MakeTex(2, 2, hubBgColor);
            activeBtnTex = MakeTex(2, 2, new Color(0f, 0.55f, 0.95f, 0.9f));
            normalBtnTex = MakeTex(2, 2, new Color(0.08f, 0.16f, 0.28f, 0.85f));
            cardTex = MakeTex(2, 2, new Color(0.05f, 0.1f, 0.18f, 0.9f));
        }

        if (headerStyle == null)
        {
            headerStyle = new GUIStyle(GUI.skin.label);
            headerStyle.fontSize = 16;
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.normal.textColor = primaryColor;

            titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontSize = 20;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.normal.textColor = Color.white;

            bodyStyle = new GUIStyle(GUI.skin.label);
            bodyStyle.fontSize = 13;
            bodyStyle.normal.textColor = new Color(0.9f, 0.95f, 1f, 0.95f);
            bodyStyle.wordWrap = true;

            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 13;
            buttonStyle.fontStyle = FontStyle.Bold;
            buttonStyle.normal.background = normalBtnTex;
            buttonStyle.normal.textColor = Color.white;
            buttonStyle.hover.textColor = accentColor;

            activeBtnStyle = new GUIStyle(buttonStyle);
            activeBtnStyle.normal.background = activeBtnTex;
            activeBtnStyle.normal.textColor = Color.yellow;

            cardBoxStyle = new GUIStyle(GUI.skin.box);
            cardBoxStyle.normal.background = cardTex;
            cardBoxStyle.padding = new RectOffset(10, 10, 8, 8);

            badgeStyle = new GUIStyle(bodyStyle);
            badgeStyle.fontSize = 14;
            badgeStyle.fontStyle = FontStyle.Bold;
        }
    }

    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; ++i) pix[i] = col;
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }

    private void OnGUI()
    {
        if (currentLocation != GameLocationState.EarthBase) return;

        // Interaction prompt when near screen and UI is closed
        if (!isHubUIOpen && playerController != null && playerController.isNearHubScreen)
        {
            InitStyles();
            DrawScreenInteractionPrompt();
            return;
        }

        if (!isHubUIOpen) return;

        InitStyles();
        DrawGiantScreenHubModal();
    }

    private void DrawScreenInteractionPrompt()
    {
        float promptW = 460f;
        float promptH = 48f;
        Rect rect = new Rect((Screen.width - promptW) * 0.5f, Screen.height - promptH - 40f, promptW, promptH);

        GUI.Box(rect, GUIContent.none, cardBoxStyle);
        GUILayout.BeginArea(rect);
        GUILayout.BeginHorizontal();
        GUILayout.Space(12);
        GUILayout.Label("<b>[E]</b> <color=#00e5ff>ACCÉDER À L'ÉCRAN GÉANT DU HUB CENTRAL</color>", headerStyle);
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    private void DrawGiantScreenHubModal()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        float winW = 860f;
        float winH = 580f;
        Rect winRect = new Rect((Screen.width - winW) * 0.5f, (Screen.height - winH) * 0.5f, winW, winH);

        GUI.Box(winRect, GUIContent.none, cardBoxStyle);
        GUILayout.BeginArea(winRect);
        GUILayout.Space(12);

        // Header Title
        GUILayout.BeginHorizontal();
        GUILayout.Space(16);
        string corpName = gm.playerStats != null ? gm.playerStats.corporationName : "Astraea Mining Corp";
        GUILayout.Label($"BASE TERRESTRE • HUB D'OPÉRATIONS ({corpName.ToUpper()})", titleStyle);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("[X] FERMER [E]", buttonStyle, GUILayout.Width(130), GUILayout.Height(28)))
        {
            SetHubUIVisibility(false);
        }
        GUILayout.Space(16);
        GUILayout.EndHorizontal();

        // Top Status Bar (Credits, Commander, Difficulty)
        GUILayout.Space(6);
        GUILayout.BeginHorizontal();
        GUILayout.Space(16);
        GUILayout.Label($"Commandant: <color=#00e5ff>{gm.playerStats.pilotName}</color>", bodyStyle, GUILayout.Width(220));
        GUILayout.Label($"Crédits: <color=#ffd700>{gm.Credits:N0} CR</color>", badgeStyle, GUILayout.Width(180));
        GUILayout.Label($"Statut: <color=#44ff88>{gm.playerStats.difficultyName}</color>", bodyStyle);
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        // Tabs
        GUILayout.BeginHorizontal();
        GUILayout.Space(16);
        if (GUILayout.Button("1. STATION SPATIALE", hubCurrentTab == 0 ? activeBtnStyle : buttonStyle, GUILayout.Height(32))) hubCurrentTab = 0;
        if (GUILayout.Button("2. CHANTIER VAISSEAU", hubCurrentTab == 1 ? activeBtnStyle : buttonStyle, GUILayout.Height(32))) hubCurrentTab = 1;
        if (GUILayout.Button("3. DÉCOLLAGE & VOL", hubCurrentTab == 2 ? activeBtnStyle : buttonStyle, GUILayout.Height(32))) hubCurrentTab = 2;
        if (GUILayout.Button("4. CORPORATION", hubCurrentTab == 3 ? activeBtnStyle : buttonStyle, GUILayout.Height(32))) hubCurrentTab = 3;
        GUILayout.Space(16);
        GUILayout.EndHorizontal();

        GUILayout.Space(12);

        // Tab Body
        GUILayout.BeginHorizontal();
        GUILayout.Space(16);
        GUILayout.BeginVertical();

        switch (hubCurrentTab)
        {
            case 0:
                DrawStationConstructionTab(gm);
                break;
            case 1:
                DrawSpaceshipConstructionTab(gm);
                break;
            case 2:
                DrawLaunchTab(gm);
                break;
            case 3:
                DrawCorporationTab(gm);
                break;
        }

        GUILayout.EndVertical();
        GUILayout.Space(16);
        GUILayout.EndHorizontal();

        GUILayout.EndArea();
    }

    private void DrawStationConstructionTab(GameManager gm)
    {
        GUILayout.Label("<b>Fabrication et Mise en Orbite de la Station Spatiale Terrestre :</b>", headerStyle);
        GUILayout.Label("Assemblez les modules requis sur la rampe de lancement pour déployer la station en orbite basse terrestre.", bodyStyle);
        GUILayout.Space(8);

        stationScroll = GUILayout.BeginScrollView(stationScroll, false, true, GUILayout.Height(320));

        // Module 1: Noyau Central
        DrawStationModuleCard("Module Central de Commandement & Énergie", "Noyau opérationnel et générateurs principaux.", 10000, isCoreBuilt, () => BuildStationModule("core"), gm);

        // Module 2: Panneaux Solaires
        DrawStationModuleCard("Panneaux Solaires Haute Puissance", "Génération d'énergie continue pour tous les sous-systèmes orbitaux.", 6000, isSolarPanelsBuilt, () => BuildStationModule("solar"), gm);

        // Module 3: Laboratoire
        DrawStationModuleCard("Laboratoire Scientifique Avancé", "Permet l'analyse géologique des échantillons et accélère la recherche.", 8000, isScienceLabBuilt, () => BuildStationModule("lab"), gm);

        // Module 4: Baie d'Amarrage
        DrawStationModuleCard("Baie d'Amarrage & Plateforme Logistique", "Permet le transfert automatique de fret et le ravitaillement des vaisseaux.", 7000, isDockingBayBuilt, () => BuildStationModule("dock"), gm);

        GUILayout.EndScrollView();

        GUILayout.Space(10);
        GUILayout.BeginHorizontal();

        if (isStationLaunched)
        {
            GUILayout.Label("<color=#44ff88>✓ <b>STATION ORBITALE ALPHA DÉPLOYÉE EN ORBITE TERRESTRE</b></color>", badgeStyle);
        }
        else if (IsStationReadyToLaunch)
        {
            if (GUILayout.Button("🚀 LANCER LA STATION EN ORBITE TERRESTRE", activeBtnStyle, GUILayout.Height(36)))
            {
                LaunchStationToOrbit();
            }
        }
        else
        {
            GUILayout.Label("<i>(Construisez au minimum le Module Central et les Panneaux Solaires pour débloquer le lancement)</i>", bodyStyle);
        }

        GUILayout.EndHorizontal();
    }

    private void DrawStationModuleCard(string name, string desc, double cost, bool isBuilt, Action onBuild, GameManager gm)
    {
        GUILayout.BeginVertical(cardBoxStyle);
        GUILayout.BeginHorizontal();

        string statusText = isBuilt ? "<color=#44ff88>[✓ CONSTRUIT]</color>" : $"<color=#ffd700>[{cost:N0} CR]</color>";
        GUILayout.Label($"<b>{name}</b> {statusText}", bodyStyle);

        if (!isBuilt)
        {
            bool canAfford = gm.CanAfford(cost);
            GUI.enabled = canAfford;
            if (GUILayout.Button(canAfford ? "Fabriquer Module" : "Fonds insuffisants", buttonStyle, GUILayout.Width(160), GUILayout.Height(24)))
            {
                onBuild?.Invoke();
            }
            GUI.enabled = true;
        }

        GUILayout.EndHorizontal();
        GUILayout.Label($"<color=#aaaaaa>{desc}</color>", bodyStyle);
        GUILayout.EndVertical();
        GUILayout.Space(4);
    }

    private void DrawSpaceshipConstructionTab(GameManager gm)
    {
        GUILayout.Label("<b>Chantier Naval & Assemblage Modulaire du Vaisseau :</b>", headerStyle);
        GUILayout.Label("Personnalisez et équipez votre navette spatiale de prospection directement dans le hangar terrestre.", bodyStyle);
        GUILayout.Space(8);

        shipScroll = GUILayout.BeginScrollView(shipScroll, false, true, GUILayout.Height(320));

        // Part 1: Cockpit & Châssis (Base)
        GUILayout.BeginVertical(cardBoxStyle);
        GUILayout.Label("<b>Cockpit Panoramique & Châssis Principal</b> <color=#44ff88>[✓ INSTALLÉ DE SÉRIE]</color>", bodyStyle);
        GUILayout.Label("<color=#aaaaaa>Structure de base profilée pour vol atmosphérique et rentrée orbitale.</color>", bodyStyle);
        GUILayout.EndVertical();
        GUILayout.Space(4);

        // Part 2: Propulseurs
        GUILayout.BeginVertical(cardBoxStyle);
        GUILayout.Label("<b>Propulseurs Ioniques Standard</b> <color=#44ff88>[✓ INSTALLÉ DE SÉRIE]</color>", bodyStyle);
        GUILayout.Label("<color=#aaaaaa>Moteurs de croisière et tuyères de contrôle d'attitude.</color>", bodyStyle);
        GUILayout.EndVertical();
        GUILayout.Space(4);

        // Part 3: Laser Minier
        DrawShipPartCard("Laser Minier Impulsionnel de Bord", "Permet de fragmenter et miner les astéroïdes rocheux et comètes.", 5000, hasMiningLaser, () => BuildShipPart("mining_laser"), gm);

        // Part 4: Bouclier
        DrawShipPartCard("Générateur de Bouclier Déflecteur", "Ajoute +50 MW de protection contre les micro-météorites et impacts.", 8000, hasShieldGenerators, () => BuildShipPart("shield"), gm);

        // Part 5: Soute Étendue
        DrawShipPartCard("Extension de Soute Modulaire (+50t)", "Double la capacité de stockage des minerais extraits.", 6000, hasExtraCargoBay, () => BuildShipPart("cargo"), gm);

        GUILayout.EndScrollView();
    }

    private void DrawShipPartCard(string name, string desc, double cost, bool isInstalled, Action onInstall, GameManager gm)
    {
        GUILayout.BeginVertical(cardBoxStyle);
        GUILayout.BeginHorizontal();

        string status = isInstalled ? "<color=#44ff88>[✓ INSTALLÉ]</color>" : $"<color=#ffd700>[{cost:N0} CR]</color>";
        GUILayout.Label($"<b>{name}</b> {status}", bodyStyle);

        if (!isInstalled)
        {
            bool canAfford = gm.CanAfford(cost);
            GUI.enabled = canAfford;
            if (GUILayout.Button(canAfford ? "Installer Pièce" : "Fonds insuffisants", buttonStyle, GUILayout.Width(160), GUILayout.Height(24)))
            {
                onInstall?.Invoke();
            }
            GUI.enabled = true;
        }

        GUILayout.EndHorizontal();
        GUILayout.Label($"<color=#aaaaaa>{desc}</color>", bodyStyle);
        GUILayout.EndVertical();
        GUILayout.Space(4);
    }

    private void DrawLaunchTab(GameManager gm)
    {
        GUILayout.Label("<b>Centre de Lancement & Navigation Orbitale :</b>", headerStyle);
        GUILayout.Space(8);

        GUILayout.BeginVertical(cardBoxStyle);
        GUILayout.Label("<b>Vérifications pré-vol :</b>", headerStyle);
        GUILayout.Label($"• Vaisseau prêt : <color=#44ff88>OPÉRATIONNEL ({gm.shipStats.shipName})</color>", bodyStyle);
        GUILayout.Label($"• Station Spatiale : {(isStationLaunched ? "<color=#44ff88>DÉPLOYÉE EN ORBITE</color>" : "<color=#ffaa00>EN CHANTIER AU SOL</color>")}", bodyStyle);
        GUILayout.Label($"• Carburant embarqué : <color=#00e5ff>{gm.shipStats.currentFuel:F0} / {gm.shipStats.maxFuel:F0} Litres</color>", bodyStyle);
        GUILayout.Label($"• Intégrité coque : <color=#00e5ff>{gm.shipStats.currentHull:F0} / {gm.shipStats.maxHull:F0} PV</color>", bodyStyle);
        GUILayout.EndVertical();

        GUILayout.Space(16);

        if (GUILayout.Button("DÉCOLLER VERS L'ESPACE (MISE EN ORBITE TERRESTRE)", activeBtnStyle, GUILayout.Height(48)))
        {
            LaunchMissionToSpace();
        }
    }

    private void DrawCorporationTab(GameManager gm)
    {
        PlayerStats p = gm.playerStats;
        if (p == null) return;

        GUILayout.Label($"<b>Corporation :</b> <color=#00ffff>{p.corporationName}</color>", headerStyle);
        GUILayout.Label($"<b>Commandant en chef :</b> {p.pilotName} ({p.title})", titleStyle);
        GUILayout.Space(8);

        GUILayout.BeginVertical(cardBoxStyle);
        GUILayout.Label("<b>Ressources de la Compagnie :</b>", headerStyle);
        GUILayout.Label($"• Trésorerie disponible : <color=#ffd700>{gm.Credits:N0} CR</color>", bodyStyle);
        GUILayout.Label($"• Niveau exécutif : Rang {p.level} (XP: {p.currentXP} / {p.xpToNextLevel})", bodyStyle);
        GUILayout.Label($"• Mode de départ : {p.difficultyName}", bodyStyle);
        GUILayout.EndVertical();

        GUILayout.Space(8);

        GUILayout.BeginVertical(cardBoxStyle);
        GUILayout.Label("<b>Influence Diplomatique & Relations de Faction :</b>", headerStyle);
        GUILayout.Label($"• <b>Coalition Terrestre :</b> {p.repEarthCoalition:F0}%", bodyStyle);
        GUILayout.Label($"• <b>République de Mars :</b> {p.repMarsRepublic:F0}%", bodyStyle);
        GUILayout.Label($"• <b>Alliance de la Ceinture :</b> {p.repBeltAlliance:F0}%", bodyStyle);
        GUILayout.Label($"• <b>Consortium Jovien :</b> {p.repJovianConsortium:F0}%", bodyStyle);
        GUILayout.EndVertical();
    }

    #endregion
}
