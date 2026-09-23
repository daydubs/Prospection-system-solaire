import re

with open("Assets/Scripts/EarthBaseController.cs", "r") as f:
    content = f.read()

# Add new fields to UI Text References
header_text_ref = '[Header("UI Text References")]'
new_fields = """[Header("UI Text References")]
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

    public UnityEngine.UI.Button btnLaunchMission;"""

content = content.replace(
    '[Header("UI Text References")]\n    public TMPro.TMP_Text creditsText;\n    public TMPro.TMP_Text corporationNameText;',
    new_fields
)

# Add listeners in Start()
start_marker = "SelectTab(0);"
new_listeners = """
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

        SelectTab(0);"""

content = content.replace(start_marker, new_listeners)

# Update RefreshUIValues()
refresh_marker = "public void RefreshUIValues()\n    {"
new_refresh = """public void RefreshUIValues()
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

    // Ancien code conservé temporairement pour le regex
    private void OldRefresh()
    {"""

old_refresh = """public void RefreshUIValues()
    {
        GameManager gm = GameManager.Instance;
        string credText = gm != null ? $"Crédits : {gm.Credits:N0} CR" : "Crédits : 50,000 CR";
        string corpText = (gm != null && gm.playerStats != null) ? gm.playerStats.corporationName : "Astra Corp";

        if (creditsText != null)
        {
            creditsText.text = credText;
        }

        if (corporationNameText != null)
        {
            corporationNameText.text = $"Commandant : {corpText}   |   {credText}   |   Statut : Opérationnel";
        }
    }"""

content = content.replace(old_refresh, new_refresh)
content = content.replace("    // Ancien code conservé temporairement pour le regex\n    private void OldRefresh()\n    {\n", "")

# Add RefreshUIValues calls
content = content.replace("CheckShipReadiness();\n    }", "CheckShipReadiness();\n        RefreshUIValues();\n    }")
content = content.replace("UpdateStationVisuals();\n                }", "UpdateStationVisuals();\n                    RefreshUIValues();\n                }")
content = content.replace("Debug.Log(\"[EarthBase] 🛰️ La Station Spatiale Terrestre 'Station Orbitale Alpha' a été déployée en orbite avec succès !\");", "Debug.Log(\"[EarthBase] 🛰️ La Station Spatiale Terrestre 'Station Orbitale Alpha' a été déployée en orbite avec succès !\");\n        RefreshUIValues();")


with open("Assets/Scripts/EarthBaseController.cs", "w") as f:
    f.write(content)
