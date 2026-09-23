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
    public Transform hubCameraTarget; // Position/angle parfait pour cadrer et interagir avec l'écran géant
    public PlayerBaseController playerController;
    public Camera baseCamera;
    public Camera spaceCamera;
    public GameObject baseEnvironmentRoot;
    public GameObject spaceshipHangarModel;
    public GameObject spaceStationHoloModel;

    [Header("Camera Transition")]
    public float cameraTransitionDuration = 0.45f;
    private Coroutine cameraTransitionCoroutine;

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
    public TMPro.TMP_Text txtStationStatus;
    public TMPro.TMP_Text txtShipStatus;

    [Header("UI Button References")]
    public UnityEngine.UI.Button btnBuildCore;
    public UnityEngine.UI.Button btnBuildSolar;
    public UnityEngine.UI.Button btnBuildLab;
    public UnityEngine.UI.Button btnBuildDock;
    public UnityEngine.UI.Button btnLaunchStation;

    public UnityEngine.UI.Button btnBuildLaser;
    public UnityEngine.UI.Button btnBuildShield;
    public UnityEngine.UI.Button btnBuildCargo;

    public UnityEngine.UI.Button btnLaunchMission;


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

        // Auto-find hub camera viewpoint if not assigned
        if (hubCameraTarget == null)
        {
            var camPoint = GameObject.Find("Hub_Camera_Viewpoint");
            if (camPoint != null) hubCameraTarget = camPoint.transform;
        }

        // Initialize World Space Canvas with Base Camera
        if (mainHubCanvas != null)
        {
            var canvasComp = mainHubCanvas.GetComponent<Canvas>();
            if (canvasComp != null && baseCamera != null)
            {
                canvasComp.worldCamera = baseCamera;
            }

            // Auto-wire exit button
            var exitBtn = mainHubCanvas.transform.Find("Principal/ExitBt")?.GetComponent<UnityEngine.UI.Button>();
            if (exitBtn != null)
            {
                exitBtn.onClick.RemoveAllListeners();
                exitBtn.onClick.AddListener(() => SetHubUIVisibility(false));
            }
        }

        // Auto-wire tab buttons if assigned
        if (tabButtons != null)
        {
            for (int i = 0; i < tabButtons.Length; i++)
            {
                if (tabButtons[i] != null)
                {
                    int tabIndex = i;
                    tabButtons[i].onClick.RemoveAllListeners();
                    tabButtons[i].onClick.AddListener(() => SelectTab(tabIndex));
                }
            }
        }

        // Make sure tab 0 is initialized

        // Wire up station buttons
        if (btnBuildCore != null) { btnBuildCore.onClick.RemoveAllListeners(); btnBuildCore.onClick.AddListener(() => BuildStationModule("core")); }
        if (btnBuildSolar != null) { btnBuildSolar.onClick.RemoveAllListeners(); btnBuildSolar.onClick.AddListener(() => BuildStationModule("solar")); }
        if (btnBuildLab != null) { btnBuildLab.onClick.RemoveAllListeners(); btnBuildLab.onClick.AddListener(() => BuildStationModule("lab")); }
        if (btnBuildDock != null) { btnBuildDock.onClick.RemoveAllListeners(); btnBuildDock.onClick.AddListener(() => BuildStationModule("dock")); }
        if (btnLaunchStation != null) { btnLaunchStation.onClick.RemoveAllListeners(); btnLaunchStation.onClick.AddListener(() => LaunchStationToOrbit()); }

        // Wire up ship buttons
        if (btnBuildLaser != null) { btnBuildLaser.onClick.RemoveAllListeners(); btnBuildLaser.onClick.AddListener(() => BuildShipPart("mining_laser")); }
        if (btnBuildShield != null) { btnBuildShield.onClick.RemoveAllListeners(); btnBuildShield.onClick.AddListener(() => BuildShipPart("shield")); }
        if (btnBuildCargo != null) { btnBuildCargo.onClick.RemoveAllListeners(); btnBuildCargo.onClick.AddListener(() => BuildShipPart("cargo")); }

        // Wire up launch button
        if (btnLaunchMission != null) { btnLaunchMission.onClick.RemoveAllListeners(); btnLaunchMission.onClick.AddListener(() => LaunchMissionToSpace()); }

        SelectTab(0);

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

        // Toggle / interact on E or Enter
        if (Keyboard.current != null && (Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame))
        {
            if (isHubUIOpen)
            {
                SetHubUIVisibility(false);
            }
            else if (playerController != null && playerController.isNearHubScreen)
            {
                SetHubUIVisibility(true);
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
            var canvasComp = mainHubCanvas.GetComponent<Canvas>();
            if (canvasComp != null && baseCamera != null)
            {
                canvasComp.worldCamera = baseCamera;
            }
            mainHubCanvas.SetActive(open);
        }

        if (open)
        {
            if (playerController != null)
            {
                playerController.canMove = false;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            SelectTab(hubCurrentTab);
            RefreshUIValues();

            StartCameraTransition(true);
        }
        else
        {
            StartCameraTransition(false);

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

    private void StartCameraTransition(bool toHub)
    {
        if (cameraTransitionCoroutine != null)
        {
            StopCoroutine(cameraTransitionCoroutine);
            cameraTransitionCoroutine = null;
        }

        if (baseCamera != null)
        {
            cameraTransitionCoroutine = StartCoroutine(CameraTransitionRoutine(toHub));
        }
    }

    private System.Collections.IEnumerator CameraTransitionRoutine(bool toHub)
    {
        float duration = Mathf.Max(0.05f, cameraTransitionDuration);
        float elapsed = 0f;

        Vector3 startPos = baseCamera.transform.position;
        Quaternion startRot = baseCamera.transform.rotation;

        Vector3 targetPos;
        Quaternion targetRot;

        if (toHub)
        {
            targetPos = hubCameraTarget != null ? hubCameraTarget.position : new Vector3(-12f, 5.60f, 8.50f);
            targetRot = hubCameraTarget != null ? hubCameraTarget.rotation : Quaternion.identity;
        }
        else
        {
            Vector3 localPos = playerController != null ? playerController.DefaultCameraLocalPos : new Vector3(0f, 0.75f, 0f);
            float pitch = playerController != null ? playerController.CameraPitch : 0f;
            targetPos = playerController != null ? playerController.transform.TransformPoint(localPos) : startPos;
            targetRot = playerController != null ? playerController.transform.rotation * Quaternion.Euler(pitch, 0f, 0f) : startRot;
        }

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            if (!toHub && playerController != null)
            {
                Vector3 localPos = playerController.DefaultCameraLocalPos;
                float pitch = playerController.CameraPitch;
                targetPos = playerController.transform.TransformPoint(localPos);
                targetRot = playerController.transform.rotation * Quaternion.Euler(pitch, 0f, 0f);
            }

            baseCamera.transform.position = Vector3.Lerp(startPos, targetPos, smoothT);
            baseCamera.transform.rotation = Quaternion.Slerp(startRot, targetRot, smoothT);
            yield return null;
        }

        if (toHub)
        {
            baseCamera.transform.position = targetPos;
            baseCamera.transform.rotation = targetRot;
        }
        else
        {
            if (playerController != null)
            {
                baseCamera.transform.localPosition = playerController.DefaultCameraLocalPos;
                baseCamera.transform.localRotation = Quaternion.Euler(playerController.CameraPitch, 0f, 0f);
                playerController.canMove = true;
            }
        }

        cameraTransitionCoroutine = null;
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
        double currentCredits = gm != null ? gm.Credits : 50000;
        string credText = gm != null ? $"Crédits : {gm.Credits:N0} CR" : "Crédits : 50,000 CR";
        string corpText = (gm != null && gm.playerStats != null) ? gm.playerStats.corporationName : "Astra Corp";

        if (creditsText != null) creditsText.text = credText;
        if (corporationNameText != null) corporationNameText.text = $"Commandant : {corpText}   |   {credText}   |   Statut : Opérationnel";

        // Update Station UI
        if (btnBuildCore != null)
        {
            btnBuildCore.interactable = !isCoreBuilt && currentCredits >= 10000;
            var txt = btnBuildCore.GetComponentInChildren<TMPro.TMP_Text>();
            if (txt != null) txt.text = isCoreBuilt ? "Construit" : "10 000 CR";
        }
        if (btnBuildSolar != null)
        {
            btnBuildSolar.interactable = !isSolarPanelsBuilt && currentCredits >= 6000;
            var txt = btnBuildSolar.GetComponentInChildren<TMPro.TMP_Text>();
            if (txt != null) txt.text = isSolarPanelsBuilt ? "Construit" : "6 000 CR";
        }
        if (btnBuildLab != null)
        {
            btnBuildLab.interactable = !isScienceLabBuilt && currentCredits >= 8000;
            var txt = btnBuildLab.GetComponentInChildren<TMPro.TMP_Text>();
            if (txt != null) txt.text = isScienceLabBuilt ? "Construit" : "8 000 CR";
        }
        if (btnBuildDock != null)
        {
            btnBuildDock.interactable = !isDockingBayBuilt && currentCredits >= 7000;
            var txt = btnBuildDock.GetComponentInChildren<TMPro.TMP_Text>();
            if (txt != null) txt.text = isDockingBayBuilt ? "Construit" : "7 000 CR";
        }
        if (btnLaunchStation != null)
        {
            btnLaunchStation.interactable = IsStationReadyToLaunch && !isStationLaunched;
            var txt = btnLaunchStation.GetComponentInChildren<TMPro.TMP_Text>();
            if (txt != null)
            {
                if (isStationLaunched) txt.text = "En Orbite";
                else if (!IsStationReadyToLaunch) txt.text = "Incomplet";
                else txt.text = "LANCER !";
            }
        }
        if (txtStationStatus != null)
        {
            if (isStationLaunched) txtStationStatus.text = "Statut : Déployée en orbite";
            else if (IsStationReadyToLaunch) txtStationStatus.text = "Statut : Prête au lancement";
            else txtStationStatus.text = "Statut : En construction";
        }

        // Update Ship UI
        if (btnBuildLaser != null)
        {
            btnBuildLaser.interactable = !hasMiningLaser && currentCredits >= 5000;
            var txt = btnBuildLaser.GetComponentInChildren<TMPro.TMP_Text>();
            if (txt != null) txt.text = hasMiningLaser ? "Installé" : "5 000 CR";
        }
        if (btnBuildShield != null)
        {
            btnBuildShield.interactable = !hasShieldGenerators && currentCredits >= 8000;
            var txt = btnBuildShield.GetComponentInChildren<TMPro.TMP_Text>();
            if (txt != null) txt.text = hasShieldGenerators ? "Installés" : "8 000 CR";
        }
        if (btnBuildCargo != null)
        {
            btnBuildCargo.interactable = !hasExtraCargoBay && currentCredits >= 6000;
            var txt = btnBuildCargo.GetComponentInChildren<TMPro.TMP_Text>();
            if (txt != null) txt.text = hasExtraCargoBay ? "Installée" : "6 000 CR";
        }
        if (txtShipStatus != null)
        {
            txtShipStatus.text = isSpaceshipAssembled ? "Vaisseau paré au décollage" : "Vaisseau en maintenance";
        }

        // Update Launch UI
        if (btnLaunchMission != null)
        {
            btnLaunchMission.interactable = isSpaceshipAssembled;
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
        RefreshUIValues();
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
        RefreshUIValues();
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
                    RefreshUIValues();
                }
                break;

            case "solar":
                if (!isSolarPanelsBuilt && gm.SpendCredits(6000, "Panneaux Solaires Station Spatiale"))
                {
                    isSolarPanelsBuilt = true;
                    gm.playerStats.AddXP(150);
                    UpdateStationVisuals();
                    RefreshUIValues();
                }
                break;

            case "lab":
                if (!isScienceLabBuilt && gm.SpendCredits(8000, "Laboratoire de Recherche Orbitale"))
                {
                    isScienceLabBuilt = true;
                    gm.playerStats.AddXP(200);
                    UpdateStationVisuals();
                    RefreshUIValues();
                }
                break;

            case "dock":
                if (!isDockingBayBuilt && gm.SpendCredits(7000, "Baie d'Amarrage & Logistique"))
                {
                    isDockingBayBuilt = true;
                    gm.playerStats.AddXP(200);
                    UpdateStationVisuals();
                    RefreshUIValues();
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
        RefreshUIValues();
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
