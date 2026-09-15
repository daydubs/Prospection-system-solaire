using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SolarSystemUI : MonoBehaviour
{
    [Header("References")]
    public SolarSystemManager systemManager;
    public SpaceshipFlightController playerShip;
    public Camera mainCam;

    [Header("UI Styling")]
    public Color primaryColor = new Color(0f, 0.85f, 1f, 1f);
    public Color accentColor = new Color(1f, 0.7f, 0f, 1f);
    public Color moonColor = new Color(0.7f, 0.9f, 0.7f, 1f);
    public Color successColor = new Color(0.2f, 0.9f, 0.4f, 1f);
    public Color warningColor = new Color(1f, 0.4f, 0.2f, 1f);
    public Color hudBgColor = new Color(0.04f, 0.08f, 0.15f, 0.92f);

    [Header("Prospector Console Modal")]
    public bool isConsoleOpen = false;
    private int consoleTab = 0; // 0: Ship, 1: Tech, 2: Profile, 3: Cargo
    private Vector2 techScrollPos;
    private Vector2 cargoScrollPos;
    private Vector2 profileScrollPos;

    private Vector2 planetScrollPos;
    private Vector2 infoScrollPos;
    private GUIStyle headerStyle;
    private GUIStyle titleStyle;
    private GUIStyle bodyStyle;
    private GUIStyle buttonStyle;
    private GUIStyle panelStyle;
    private GUIStyle activeBtnStyle;
    private GUIStyle statusBadgeStyle;
    private Texture2D panelTex;
    private Texture2D activeBtnTex;
    private Texture2D normalBtnTex;

    private void Start()
    {
        if (systemManager == null) systemManager = SolarSystemManager.Instance;
        if (playerShip == null && systemManager != null) playerShip = systemManager.playerShip;
        if (mainCam == null) mainCam = Camera.main;

        // Ensure GameManager exists
        if (GameManager.Instance == null)
        {
            GameObject gm = new GameObject("GameManager");
            gm.AddComponent<GameManager>();
        }
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame)
        {
            // Do not open if main menu is open
            if (MainMenuController.Instance == null || !MainMenuController.Instance.isMenuOpen)
            {
                ToggleProspectorConsole();
            }
        }
    }

    public void ToggleProspectorConsole()
    {
        isConsoleOpen = !isConsoleOpen;
    }

    private void InitStyles()
    {
        if (panelTex == null)
        {
            panelTex = MakeTex(2, 2, hudBgColor);
            activeBtnTex = MakeTex(2, 2, new Color(0f, 0.45f, 0.8f, 0.9f));
            normalBtnTex = MakeTex(2, 2, new Color(0.1f, 0.18f, 0.28f, 0.85f));
        }

        if (panelStyle == null)
        {
            panelStyle = new GUIStyle(GUI.skin.box);
            panelStyle.normal.background = panelTex;
            panelStyle.border = new RectOffset(4, 4, 4, 4);

            headerStyle = new GUIStyle(GUI.skin.label);
            headerStyle.fontSize = 15;
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.normal.textColor = primaryColor;

            titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontSize = 18;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.normal.textColor = Color.white;

            bodyStyle = new GUIStyle(GUI.skin.label);
            bodyStyle.fontSize = 12;
            bodyStyle.normal.textColor = new Color(0.9f, 0.95f, 1f, 0.95f);
            bodyStyle.wordWrap = true;

            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 12;
            buttonStyle.fontStyle = FontStyle.Bold;
            buttonStyle.normal.background = normalBtnTex;
            buttonStyle.normal.textColor = Color.white;
            buttonStyle.hover.textColor = accentColor;

            activeBtnStyle = new GUIStyle(buttonStyle);
            activeBtnStyle.normal.background = activeBtnTex;
            activeBtnStyle.normal.textColor = accentColor;

            statusBadgeStyle = new GUIStyle(bodyStyle);
            statusBadgeStyle.fontSize = 13;
            statusBadgeStyle.fontStyle = FontStyle.Bold;
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
        if (MainMenuController.Instance != null && MainMenuController.Instance.isMenuOpen)
        {
            return;
        }

        if (EarthBaseController.Instance != null && EarthBaseController.Instance.currentLocation == GameLocationState.EarthBase)
        {
            return;
        }

        InitStyles();

        if (systemManager == null)
        {
            systemManager = SolarSystemManager.Instance;
            if (systemManager == null) return;
        }

        DrawDestinationMarkerOnScreen();
        DrawLeftDestinationPanel();
        DrawRightTargetInfoPanel();
        DrawTopSimulationControlBar();
        DrawTopStatusStrip();
        DrawBottomFlightHUD();

        if (isConsoleOpen)
        {
            DrawProspectorConsoleModal();
        }
    }

    private void DrawTopStatusStrip()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        float stripW = 620f;
        float stripH = 34f;
        Rect stripRect = new Rect((Screen.width - stripW) * 0.5f, 54f, stripW, stripH);

        GUI.Box(stripRect, GUIContent.none, panelStyle);
        GUILayout.BeginArea(stripRect);
        GUILayout.BeginHorizontal();
        GUILayout.Space(8);

        // Date display
        GUILayout.Label($"📅 <color=#00ffff>{gm.gameTime.ToDateString()}</color> ({gm.gameTime.ToTimeString()})", statusBadgeStyle, GUILayout.Width(190));

        // Credits display
        GUILayout.Label($"💳 <color=#ffd700>{gm.Credits:N0} CR</color>", statusBadgeStyle, GUILayout.Width(130));

        // Fuel & Hull status
        if (gm.shipStats != null)
        {
            float fuelPct = (gm.shipStats.currentFuel / gm.shipStats.maxFuel) * 100f;
            GUILayout.Label($"⛽ {fuelPct:F0}% | 🛡️ {gm.shipStats.currentHull:F0}/{gm.shipStats.maxHull:F0} PV", bodyStyle, GUILayout.Width(150));
        }

        // Toggle Console Button
        GUIStyle cBtnStyle = isConsoleOpen ? activeBtnStyle : buttonStyle;
        if (GUILayout.Button(isConsoleOpen ? "✕ Fermer [P]" : "📊 Console [P]", cBtnStyle, GUILayout.Width(110), GUILayout.Height(24)))
        {
            ToggleProspectorConsole();
        }

        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    private void DrawProspectorConsoleModal()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        float winW = 720f;
        float winH = 460f;
        Rect winRect = new Rect((Screen.width - winW) * 0.5f, (Screen.height - winH) * 0.5f, winW, winH);

        GUI.Box(winRect, GUIContent.none, panelStyle);
        GUILayout.BeginArea(winRect);
        GUILayout.Space(10);

        // Title Header
        GUILayout.BeginHorizontal();
        GUILayout.Space(12);
        GUILayout.Label("🛰️ TERMINAL DE BORD DU PROSPECTEUR", titleStyle);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("✕", buttonStyle, GUILayout.Width(30), GUILayout.Height(24)))
        {
            isConsoleOpen = false;
        }
        GUILayout.Space(12);
        GUILayout.EndHorizontal();

        GUILayout.Space(6);

        // Tab selection buttons
        GUILayout.BeginHorizontal();
        GUILayout.Space(12);
        if (GUILayout.Button("🚀 VAISSEAU", consoleTab == 0 ? activeBtnStyle : buttonStyle, GUILayout.Height(30))) consoleTab = 0;
        if (GUILayout.Button("🔬 TECHNOLOGIES", consoleTab == 1 ? activeBtnStyle : buttonStyle, GUILayout.Height(30))) consoleTab = 1;
        if (GUILayout.Button("👤 PILOTE & FACTIONS", consoleTab == 2 ? activeBtnStyle : buttonStyle, GUILayout.Height(30))) consoleTab = 2;
        if (GUILayout.Button("📦 SOUTE & RESSOURCES", consoleTab == 3 ? activeBtnStyle : buttonStyle, GUILayout.Height(30))) consoleTab = 3;
        GUILayout.Space(12);
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        // Tab Content
        GUILayout.BeginHorizontal();
        GUILayout.Space(14);
        GUILayout.BeginVertical();

        switch (consoleTab)
        {
            case 0:
                DrawShipTab(gm);
                break;
            case 1:
                DrawTechTab(gm);
                break;
            case 2:
                DrawProfileTab(gm);
                break;
            case 3:
                DrawCargoTab(gm);
                break;
        }

        GUILayout.EndVertical();
        GUILayout.Space(14);
        GUILayout.EndHorizontal();

        GUILayout.EndArea();
    }

    private void DrawShipTab(GameManager gm)
    {
        ShipStats ship = gm.shipStats;
        if (ship == null) return;

        GUILayout.Label($"<b>Vaisseau :</b> <color=#00ffff>{ship.shipName}</color> ({ship.shipClass})", headerStyle);
        GUILayout.Space(8);

        // Vitals
        GUILayout.BeginHorizontal();
        GUILayout.Label($"• <b>Coque :</b> {ship.currentHull:F0} / {ship.maxHull:F0} PV", bodyStyle, GUILayout.Width(220));
        if (ship.currentHull < ship.maxHull)
        {
            if (GUILayout.Button("🔧 Réparer (+25 PV / 250 CR)", buttonStyle, GUILayout.Height(24)))
            {
                gm.RepairHull(25f, 10.0);
            }
        }
        else
        {
            GUILayout.Label("<color=#33ff77>✓ Intégrité optimale</color>", bodyStyle);
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label($"• <b>Boucliers :</b> {ship.currentShield:F0} / {ship.maxShield:F0} MW (Régén: {ship.shieldRegenRate:F1}/s)", bodyStyle);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label($"• <b>Carburant :</b> {ship.currentFuel:F0} / {ship.maxFuel:F0} L", bodyStyle, GUILayout.Width(220));
        if (ship.currentFuel < ship.maxFuel)
        {
            if (GUILayout.Button("⛽ Faire le plein (+100 L / 200 CR)", buttonStyle, GUILayout.Height(24)))
            {
                gm.Refuel(100f, 2.0);
            }
        }
        else
        {
            GUILayout.Label("<color=#33ff77>✓ Réservoirs pleins</color>", bodyStyle);
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(8);
        GUILayout.Label("<b>Équipements & Performances :</b>", headerStyle);
        GUILayout.Label($"• <b>Portée des capteurs :</b> {ship.scannerRange:F0} UA-eq", bodyStyle);
        GUILayout.Label($"• <b>Puissance du laser de minage :</b> {ship.miningLaserPower:F1} MW", bodyStyle);
        GUILayout.Label($"• <b>Générateur Warp :</b> Niveau {ship.warpDriveTier}", bodyStyle);
        GUILayout.Label($"• <b>Capacité de charge totale :</b> {ship.CurrentCargoTons:F1} / {ship.maxCargoCapacity:F1} tonnes", bodyStyle);
    }

    private void DrawTechTab(GameManager gm)
    {
        GUILayout.Label("<b>Arbre Technologique & Améliorations de Prospection :</b>", headerStyle);
        techScrollPos = GUILayout.BeginScrollView(techScrollPos, false, true, GUILayout.Height(320));

        foreach (var tech in gm.allTechnologies)
        {
            GUILayout.BeginVertical("box");
            GUILayout.BeginHorizontal();

            string statusIcon = tech.isUnlocked ? "<color=#33ff77>[✓ ACQUIS]</color>" : $"<color=#ffd700>[{tech.researchCost:N0} CR]</color>";
            GUILayout.Label($"<b>{tech.displayName}</b> {statusIcon}", bodyStyle);

            if (!tech.isUnlocked)
            {
                bool canAfford = gm.CanAfford(tech.researchCost);
                GUI.enabled = canAfford;
                if (GUILayout.Button(canAfford ? "Débloquer" : "Crédits insuffisants", buttonStyle, GUILayout.Width(140), GUILayout.Height(24)))
                {
                    gm.UnlockTechnology(tech.id);
                }
                GUI.enabled = true;
            }

            GUILayout.EndHorizontal();
            GUILayout.Label($"<color=#aaaaaa>{tech.description}</color>", bodyStyle);
            GUILayout.EndVertical();
            GUILayout.Space(4);
        }

        GUILayout.EndScrollView();
    }

    private void DrawProfileTab(GameManager gm)
    {
        PlayerStats player = gm.playerStats;
        if (player == null) return;

        profileScrollPos = GUILayout.BeginScrollView(profileScrollPos, false, false, GUILayout.Height(320));

        GUILayout.Label($"<b>Commandant :</b> <color=#00ffff>{player.pilotName}</color>  |  <b>Corporation :</b> <color=#ffd700>{player.corporationName}</color>", headerStyle);
        GUILayout.Label($"<b>Titre :</b> {player.title}  |  <b>Mode :</b> {player.difficultyName}", bodyStyle);
        GUILayout.Label($"<b>Niveau {player.level}</b> (XP: {player.currentXP} / {player.xpToNextLevel})", titleStyle);
        GUILayout.Space(8);

        GUILayout.Label("<b>Compétences de Prospection :</b>", headerStyle);
        GUILayout.Label($"• <b>Minage & Extraction :</b> Rang {player.miningSkill}", bodyStyle);
        GUILayout.Label($"• <b>Analyse & Détection :</b> Rang {player.scanningSkill}", bodyStyle);
        GUILayout.Label($"• <b>Pilotage Spatial :</b> Rang {player.pilotingSkill}", bodyStyle);
        GUILayout.Label($"• <b>Négociation Commerciale :</b> Rang {player.tradingSkill}", bodyStyle);

        GUILayout.Space(8);
        GUILayout.Label("<b>Réputations de Factions :</b>", headerStyle);
        GUILayout.Label($"• <b>Coalition Terrestre :</b> {player.repEarthCoalition:F0}%", bodyStyle);
        GUILayout.Label($"• <b>République Martienne :</b> {player.repMarsRepublic:F0}%", bodyStyle);
        GUILayout.Label($"• <b>Alliance de la Ceinture d'Astéroïdes :</b> {player.repBeltAlliance:F0}%", bodyStyle);
        GUILayout.Label($"• <b>Consortium Jovien :</b> {player.repJovianConsortium:F0}%", bodyStyle);

        GUILayout.Space(8);
        GUILayout.Label("<b>Statistiques de Carrière :</b>", headerStyle);
        GUILayout.Label($"• Mondes visités : {player.totalBodiesVisited}  |  Astéroïdes forés : {player.totalAsteroidsMined}", bodyStyle);
        GUILayout.Label($"• Total crédits engrangés : {player.totalCreditsEarned:N0} CR  |  Fret extrait : {player.totalCargoMinedTons:F1} tonnes", bodyStyle);

        GUILayout.EndScrollView();
    }

    private void DrawCargoTab(GameManager gm)
    {
        ShipStats ship = gm.shipStats;
        if (ship == null) return;

        GUILayout.BeginHorizontal();
        GUILayout.Label($"<b>Capacité Soute :</b> <color=#ffd700>{ship.CurrentCargoTons:F1} / {ship.maxCargoCapacity:F1} tonnes</color>", headerStyle);
        GUILayout.FlexibleSpace();
        
        // Quick button to add sample mined resources for demonstration
        if (GUILayout.Button("+ Miner Minerais Test", buttonStyle, GUILayout.Width(160), GUILayout.Height(24)))
        {
            gm.AddCargo("res_iron", "Minerai de Fer", 5f, 250);
            gm.AddCargo("res_water_ice", "Glace Volatile", 8f, 180);
            gm.AddCargo("res_platinum", "Platine Natif", 1.5f, 3200);
            gm.playerStats.totalAsteroidsMined++;
            gm.playerStats.AddXP(50);
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(6);

        if (ship.cargo.Count == 0)
        {
            GUILayout.Label("<i>Votre soute est actuellement vide. Approchez-vous d'un astéroïde ou d'une lune pour prospecter et extraire des minerais.</i>", bodyStyle);
            return;
        }

        cargoScrollPos = GUILayout.BeginScrollView(cargoScrollPos, false, true, GUILayout.Height(260));

        for (int i = 0; i < ship.cargo.Count; i++)
        {
            CargoItem item = ship.cargo[i];
            if (item == null) continue;

            double itemTotalValue = item.quantity * item.basePricePerTon;

            GUILayout.BeginVertical("box");
            GUILayout.BeginHorizontal();
            GUILayout.Label($"<b>{item.resourceName}</b> : {item.quantity:F1} t (<color=#ffd700>~{itemTotalValue:N0} CR</color>)", bodyStyle);
            
            if (GUILayout.Button($"Vendre tout (+{itemTotalValue:N0} CR)", buttonStyle, GUILayout.Width(170), GUILayout.Height(24)))
            {
                gm.SellCargo(item.resourceId, item.quantity);
                break;
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.Space(3);
        }

        GUILayout.EndScrollView();
    }

    private void DrawDestinationMarkerOnScreen()
    {
        if (systemManager.currentDestination == null || mainCam == null) return;

        CelestialBody dest = systemManager.currentDestination;
        Vector3 screenPos = mainCam.WorldToScreenPoint(dest.transform.position);

        if (screenPos.z > 0)
        {
            float x = screenPos.x;
            float y = Screen.height - screenPos.y;

            // Draw diamond / target reticle
            float size = 18f;
            Color prevCol = GUI.color;
            GUI.color = accentColor;

            // Reticle box
            GUI.Box(new Rect(x - size, y - size, size * 2, size * 2), GUIContent.none);

            // Label
            float dist = Vector3.Distance(playerShip != null ? playerShip.transform.position : mainCam.transform.position, dest.transform.position);
            string distText = dist < 50f ? $"{(dist * 15376f):N0} km" : $"{(dist / 600f):F2} UA";
            string labelText = $"▶ {dest.bodyName.ToUpper()} [{distText}]";
            
            GUIStyle markerStyle = new GUIStyle(bodyStyle);
            markerStyle.fontSize = 13;
            markerStyle.fontStyle = FontStyle.Bold;
            markerStyle.normal.textColor = accentColor;
            GUI.Label(new Rect(x + size + 4, y - 10, 240, 25), labelText, markerStyle);

            GUI.color = prevCol;
        }
    }

    private void DrawLeftDestinationPanel()
    {
        float panelW = 280f;
        float panelH = Screen.height - 120f;
        Rect panelRect = new Rect(15, 50, panelW, panelH);

        GUI.Box(panelRect, GUIContent.none, panelStyle);
        GUILayout.BeginArea(panelRect);
        GUILayout.Space(10);

        GUILayout.Label("🪐 DESTINATIONS SPATIALES", headerStyle);
        GUILayout.Label("Cliquez pour définir la destination", bodyStyle);
        GUILayout.Space(5);

        planetScrollPos = GUILayout.BeginScrollView(planetScrollPos, false, true);

        // Sun / Central Star
        if (systemManager.centralStar != null)
        {
            DrawBodyButton(systemManager.centralStar, "☀️ ");
        }

        // Planets and their moons
        for (int i = 0; i < systemManager.planets.Count; i++)
        {
            CelestialBody planet = systemManager.planets[i];
            DrawBodyButton(planet, "🪐 ");

            // Draw satellites / moons under each planet
            List<CelestialBody> moons = systemManager.GetMoonsForPlanet(planet);
            for (int m = 0; m < moons.Count; m++)
            {
                CelestialBody moon = moons[m];
                GUILayout.BeginHorizontal();
                GUILayout.Space(25);
                DrawBodyButton(moon, " ↳ 🌕 ", moonColor);
                GUILayout.EndHorizontal();
            }
        }

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void DrawBodyButton(CelestialBody body, string prefix, Color? customColor = null)
    {
        bool isSelected = systemManager.currentDestination == body;
        GUIStyle st = isSelected ? activeBtnStyle : buttonStyle;

        Color prevColor = GUI.contentColor;
        if (customColor.HasValue) GUI.contentColor = customColor.Value;

        string label = $"{prefix}{body.bodyName}";
        if (GUILayout.Button(label, st, GUILayout.Height(28)))
        {
            systemManager.SetDestination(body);
        }

        GUI.contentColor = prevColor;
    }

    private void DrawRightTargetInfoPanel()
    {
        CelestialBody dest = systemManager.currentDestination;
        if (dest == null) return;

        float panelW = 320f;
        float panelH = 340f;
        Rect panelRect = new Rect(Screen.width - panelW - 15, 50, panelW, panelH);

        GUI.Box(panelRect, GUIContent.none, panelStyle);
        GUILayout.BeginArea(panelRect);
        GUILayout.Space(10);

        GUILayout.Label($"TELEMETRIE: {dest.bodyName.ToUpper()}", headerStyle);
        GUILayout.Label($"Type: {GetBodyTypeString(dest.bodyType)}", titleStyle);
        GUILayout.Space(5);

        infoScrollPos = GUILayout.BeginScrollView(infoScrollPos, false, false);

        float dist = playerShip != null ? Vector3.Distance(playerShip.transform.position, dest.transform.position) : 0f;
        string distFormatted = dist < 50f ? $"{(dist * 15376f):N0} km ({dist:F1} u)" : $"{(dist / 600f):F2} UA ({(dist * 249333f / 1000000f):F1} M km)";
        GUILayout.Label($"• <b>Distance vaisseau :</b> {distFormatted}", bodyStyle);
        GUILayout.Label($"• <b>Rayon orbital :</b> {(dest.orbitRadius / 600f):F2} UA ({dest.orbitRadius:F0} u)", bodyStyle);
        GUILayout.Label($"• <b>Période orbitale :</b> {Mathf.Abs(dest.orbitPeriodDays):F2} jours", bodyStyle);
        
        if (!string.IsNullOrEmpty(dest.description))
        {
            GUILayout.Space(5);
            GUILayout.Label($"<b>Description :</b>\n{dest.description}", bodyStyle);
        }

        if (!string.IsNullOrEmpty(dest.physicalCharacteristics))
        {
            GUILayout.Space(5);
            GUILayout.Label($"<b>Données physiques :</b>\n{dest.physicalCharacteristics}", bodyStyle);
        }

        GUILayout.EndScrollView();

        GUILayout.Space(8);

        // Action Buttons for this destination
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("🚀 Pilote Auto [T]", buttonStyle, GUILayout.Height(32)))
        {
            if (playerShip != null) playerShip.EngageAutopilot();
        }
        if (GUILayout.Button("⚡ Warp [J]", buttonStyle, GUILayout.Height(32)))
        {
            if (playerShip != null) playerShip.WarpToDestination();
        }
        if (GUILayout.Button("🔍 Observer [F]", buttonStyle, GUILayout.Height(32)))
        {
            if (playerShip != null) playerShip.FocusOnDestination();
        }
        GUILayout.EndHorizontal();

        GUILayout.EndArea();
    }

    private void DrawTopSimulationControlBar()
    {
        float barW = 680f;
        float barH = 40f;
        Rect barRect = new Rect((Screen.width - barW) * 0.5f, 10, barW, barH);

        GUI.Box(barRect, GUIContent.none, panelStyle);
        GUILayout.BeginArea(barRect);
        GUILayout.BeginHorizontal();
        GUILayout.Space(8);

        GUILayout.Label("⏱️ SIMULATION:", headerStyle, GUILayout.Width(110));

        if (GUILayout.Button(systemManager.isPaused ? "▶ Reprendre" : "⏸ Pause", buttonStyle, GUILayout.Width(90), GUILayout.Height(26)))
        {
            systemManager.TogglePause();
        }

        if (GUILayout.Button("1x", systemManager.timeScale == 1f && !systemManager.isPaused ? activeBtnStyle : buttonStyle, GUILayout.Width(35), GUILayout.Height(26)))
        {
            systemManager.isPaused = false;
            systemManager.SetTimeScale(1f);
        }
        if (GUILayout.Button("5x", systemManager.timeScale == 5f ? activeBtnStyle : buttonStyle, GUILayout.Width(35), GUILayout.Height(26)))
        {
            systemManager.isPaused = false;
            systemManager.SetTimeScale(5f);
        }
        if (GUILayout.Button("20x", systemManager.timeScale == 20f ? activeBtnStyle : buttonStyle, GUILayout.Width(40), GUILayout.Height(26)))
        {
            systemManager.isPaused = false;
            systemManager.SetTimeScale(20f);
        }
        if (GUILayout.Button("50x", systemManager.timeScale == 50f ? activeBtnStyle : buttonStyle, GUILayout.Width(40), GUILayout.Height(26)))
        {
            systemManager.isPaused = false;
            systemManager.SetTimeScale(50f);
        }
        if (GUILayout.Button("100x", systemManager.timeScale == 100f ? activeBtnStyle : buttonStyle, GUILayout.Width(45), GUILayout.Height(26)))
        {
            systemManager.isPaused = false;
            systemManager.SetTimeScale(100f);
        }

        GUILayout.Space(8);
        if (GUILayout.Button(systemManager.showOrbitLines ? "Orbites: OUI" : "Orbites: NON", buttonStyle, GUILayout.Width(95), GUILayout.Height(26)))
        {
            systemManager.ToggleOrbitLines(!systemManager.showOrbitLines);
        }

        GUILayout.Space(6);
        if (GUILayout.Button("🌍 BASE", buttonStyle, GUILayout.Width(70), GUILayout.Height(26)))
        {
            if (SceneTransitionManager.Instance != null)
            {
                SceneTransitionManager.Instance.LoadEarthBase();
            }
            else if (EarthBaseController.Instance != null)
            {
                EarthBaseController.Instance.SetLocationState(GameLocationState.EarthBase);
            }
        }

        GUILayout.Space(4);
        if (GUILayout.Button("☰ MENU [Esc]", buttonStyle, GUILayout.Width(90), GUILayout.Height(26)))
        {
            if (MainMenuController.Instance != null)
            {
                MainMenuController.Instance.SetMenuVisibility(true);
            }
        }

        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    private void DrawBottomFlightHUD()
    {
        if (playerShip == null) return;

        float hudW = 660f;
        float hudH = 55f;
        Rect hudRect = new Rect((Screen.width - hudW) * 0.5f, Screen.height - hudH - 15, hudW, hudH);

        GUI.Box(hudRect, GUIContent.none, panelStyle);
        GUILayout.BeginArea(hudRect);
        GUILayout.BeginHorizontal();
        GUILayout.Space(10);

        string modeStr = playerShip.currentMode switch
        {
            FlightMode.Autopilot => "<color=#00ffff>PILOTE AUTOMATIQUE</color>",
            FlightMode.OrbitInspect => "<color=#ffd700>ORBITE & INSPECTION</color>",
            _ => "<color=#ffffff>VOL LIBRE</color>"
        };

        // Realistic speed conversion: base cruise speed 0.08 units/s = 1.78 km/s (~6,400 km/h, transit Terre-Lune ~2.5 jours)
        float speedDisplayKmS = playerShip.currentSpeed * 22.25f;

        GUILayout.BeginVertical();
        GUILayout.Label($"<b>MODE DE VOL :</b> {modeStr}  |  <b>VITESSE DE PROPULSION :</b> {speedDisplayKmS:F1} km/s  (Terre-Lune : ~2.5 jours)", bodyStyle);
        GUILayout.Label("Contrôles: [Z/Q/S/D] ou [W/A/S/D] Déplacer | [Shift] Boost | [T] Pilote Auto | [J] Warp | [P] Console | [Esc] Menu", bodyStyle);
        GUILayout.EndVertical();

        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    private string GetBodyTypeString(CelestialBodyType type)
    {
        return type switch
        {
            CelestialBodyType.Star => "Étoile centrale",
            CelestialBodyType.Planet => "Planète",
            CelestialBodyType.Moon => "Satellite naturel (Lune)",
            CelestialBodyType.DwarfPlanet => "Planète naine",
            CelestialBodyType.Asteroid => "Astéroïde",
            _ => "Corps céleste"
        };
    }
}

