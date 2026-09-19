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
    public bool isCoreBuilt = true;
    public bool isSolarPanelsBuilt = true;
    public bool isScienceLabBuilt = true;
    public bool isDockingBayBuilt = true;
    public bool isStationLaunched = false;
    public GameObject orbitalStationPrefab;
    public CelestialBody earthCelestialBody;

    [Header("Modular Mesh References for Station")]
    public GameObject stationCorePart;
    public GameObject stationLabPart;
    public GameObject stationSolarPanelsPart;
    public GameObject stationDockingBayPart;

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


    [Header("Hub Canvas References (World Space)")]
    public GameObject mainHubCanvas; // Le Canvas principal (l'écran géant)
    public GameObject interactionPromptCanvas; // Le petit "Appuyez sur E"

    [Header("Hub Tabs Arrays")]
    public UnityEngine.UI.Button[] tabButtons; // Boutons pour changer d'onglet
    public GameObject[] tabPanels; // 0: Station, 1: Vaisseau, 2: Lancement, 3: Corporation

    [Header("UI Text References")]
    public TMPro.TMP_Text creditsText;
    public TMPro.TMP_Text corporationNameText;


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
        UpdateStationVisuals();

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
        if (currentLocation != GameLocationState.EarthBase) return;

        // Interaction prompt when near screen and UI is closed
        if (interactionPromptCanvas != null)
        {
            interactionPromptCanvas.SetActive(!isHubUIOpen && playerController != null && playerController.isNearHubScreen);
        }

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

        if (mainHubCanvas != null)
        {
            mainHubCanvas.SetActive(open);
        }

        if (open)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            RefreshUIValues(); // Actualise les données à l'ouverture
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

    // Nouvelle méthode pour changer d'onglet via les boutons
    public void SelectTab(int tabIndex)
    {
        hubCurrentTab = tabIndex;

        // Active/Désactive les panneaux
        if (tabPanels != null)
        {
            for (int i = 0; i < tabPanels.Length; i++)
            {
                if (tabPanels[i] != null)
                {
                    tabPanels[i].SetActive(i == tabIndex);
                }
            }
        }

        // Optionnel : Changer la couleur des boutons pour montrer l'onglet actif
        if (tabButtons != null)
        {
            for (int i = 0; i < tabButtons.Length; i++)
            {
                if (tabButtons[i] != null)
                {
                    var colors = tabButtons[i].colors;
                    // Ex: gris pour inactif, bleu pour actif
                    colors.normalColor = (i == tabIndex) ? new Color(0f, 0.55f, 0.95f, 0.9f) : new Color(0.08f, 0.16f, 0.28f, 0.85f);
                    tabButtons[i].colors = colors;
                }
            }
        }

        RefreshUIValues();
    }

    // Nouvelle méthode pour rafraichir les valeurs textuelles de l'UI si nécessaire
    public void RefreshUIValues()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        if (creditsText != null)
        {
            creditsText.text = $"Crédits : {gm.Credits:N0} CR";
        }

        if (corporationNameText != null && gm.playerStats != null)
        {
            corporationNameText.text = gm.playerStats.corporationName;
        }

        // Vous pouvez ajouter ici l'actualisation du texte des boutons de construction/lancement
        // selon l'état actuel (fonds suffisants, etc.)
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

    public void UpdateStationVisuals()
    {
        if (stationCorePart != null) stationCorePart.SetActive(isCoreBuilt);
        if (stationLabPart != null) stationLabPart.SetActive(isScienceLabBuilt);
        if (stationSolarPanelsPart != null) stationSolarPanelsPart.SetActive(isSolarPanelsBuilt);
        if (stationDockingBayPart != null) stationDockingBayPart.SetActive(isDockingBayBuilt);
    }

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
                    UpdateStationVisuals();
                }
                break;

            case "solar":
                if (!isSolarPanelsBuilt && gm.SpendCredits(6000, "Panneaux Solaires Station Spatiale"))
                {
                    isSolarPanelsBuilt = true;
                    gm.playerStats.AddXP(150);
                    UpdateStationVisuals();
                }
                break;

            case "lab":
                if (!isScienceLabBuilt && gm.SpendCredits(8000, "Laboratoire de Recherche Orbitale"))
                {
                    isScienceLabBuilt = true;
                    gm.playerStats.AddXP(200);
                    UpdateStationVisuals();
                }
                break;

            case "dock":
                if (!isDockingBayBuilt && gm.SpendCredits(7000, "Baie d'Amarrage & Logistique"))
                {
                    isDockingBayBuilt = true;
                    gm.playerStats.AddXP(200);
                    UpdateStationVisuals();
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

        Transform parentTransform = earthCelestialBody.transform.parent != null ? earthCelestialBody.transform.parent : earthCelestialBody.transform;
        GameObject stationObj;

        if (orbitalStationPrefab != null)
        {
            stationObj = Instantiate(orbitalStationPrefab, parentTransform);
            stationObj.name = "Station Orbitale Alpha";
        }
        else
        {
            stationObj = new GameObject("Station Orbitale Alpha");
            stationObj.transform.SetParent(parentTransform, false);

#if UNITY_EDITOR
            GameObject fbx = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/models/StationOrbital.fbx");
            if (fbx != null)
            {
                GameObject model = Instantiate(fbx, stationObj.transform);
                model.name = "Model";
                model.transform.localPosition = new Vector3(0f, -8.68f, 12.93f);
                model.transform.localScale = Vector3.one * 0.08f;
            }
#endif
        }

        CelestialBody stationBody = stationObj.GetComponent<CelestialBody>();
        if (stationBody == null)
        {
            stationBody = stationObj.AddComponent<CelestialBody>();
        }

        stationBody.bodyName = "Station Orbitale Alpha";
        stationBody.bodyType = CelestialBodyType.Moon;
        stationBody.orbitRadius = 6.5f;
        stationBody.orbitPeriodDays = 15f;
        stationBody.bodyRadius = 0.6f;
        stationBody.rotationPeriodDays = 5f;
        stationBody.orbitCenter = earthCelestialBody.transform;
        stationBody.orbitLineColor = new Color(0f, 0.85f, 1f, 0.6f);
        stationBody.description = "La station spatiale orbitale terrestre construite et déployée par votre corporation. Hub de ravitaillement et de recherche scientifique de pointe comprenant le Core, le Lab, les Panneaux Solaires et la Baie d'Amarrage.";
        stationBody.physicalCharacteristics = "• Altitude orbitale : 420 km\n• Énergie : Panneaux solaires photovoltaïques\n• Modules actifs : Core, Lab, Panneaux Solaires & Docking Bay";

        if (!earthCelestialBody.satellites.Contains(stationBody))
        {
            earthCelestialBody.satellites.Add(stationBody);
        }
        if (SolarSystemManager.Instance != null && !SolarSystemManager.Instance.allBodies.Contains(stationBody))
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


}
