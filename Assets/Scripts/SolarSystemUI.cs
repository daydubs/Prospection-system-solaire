using System.Collections.Generic;
using UnityEngine;

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
    public Color hudBgColor = new Color(0.04f, 0.08f, 0.15f, 0.88f);

    private Vector2 planetScrollPos;
    private Vector2 infoScrollPos;
    private GUIStyle headerStyle;
    private GUIStyle titleStyle;
    private GUIStyle bodyStyle;
    private GUIStyle buttonStyle;
    private GUIStyle panelStyle;
    private GUIStyle activeBtnStyle;
    private Texture2D panelTex;
    private Texture2D activeBtnTex;
    private Texture2D normalBtnTex;

    private void Start()
    {
        if (systemManager == null) systemManager = SolarSystemManager.Instance;
        if (playerShip == null && systemManager != null) playerShip = systemManager.playerShip;
        if (mainCam == null) mainCam = Camera.main;
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
        DrawBottomFlightHUD();
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
            string distText = dist > 100f ? $"{(dist / 10f):F1} AU" : $"{dist:F0} km";
            string labelText = $"▶ {dest.bodyName.ToUpper()} [{distText}]";
            
            GUIStyle markerStyle = new GUIStyle(bodyStyle);
            markerStyle.fontSize = 13;
            markerStyle.fontStyle = FontStyle.Bold;
            markerStyle.normal.textColor = accentColor;
            GUI.Label(new Rect(x + size + 4, y - 10, 200, 25), labelText, markerStyle);

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
        GUILayout.Label($"• <b>Distance vaisseau :</b> {dist:F1} unités", bodyStyle);
        GUILayout.Label($"• <b>Rayon orbital :</b> {dest.orbitRadius:F1} UA", bodyStyle);
        GUILayout.Label($"• <b>Vitesse orbitale :</b> {dest.orbitSpeed:F1}°/s", bodyStyle);
        GUILayout.Label($"• <b>Taille / Diamètre :</b> {dest.bodyRadius * 2f:F1} km-eq", bodyStyle);
        
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
        float barW = 580f;
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

        if (GUILayout.Button("1x", systemManager.timeScale == 1f && !systemManager.isPaused ? activeBtnStyle : buttonStyle, GUILayout.Width(40), GUILayout.Height(26)))
        {
            systemManager.isPaused = false;
            systemManager.SetTimeScale(1f);
        }
        if (GUILayout.Button("5x", systemManager.timeScale == 5f ? activeBtnStyle : buttonStyle, GUILayout.Width(40), GUILayout.Height(26)))
        {
            systemManager.isPaused = false;
            systemManager.SetTimeScale(5f);
        }
        if (GUILayout.Button("20x", systemManager.timeScale == 20f ? activeBtnStyle : buttonStyle, GUILayout.Width(45), GUILayout.Height(26)))
        {
            systemManager.isPaused = false;
            systemManager.SetTimeScale(20f);
        }
        if (GUILayout.Button("50x", systemManager.timeScale == 50f ? activeBtnStyle : buttonStyle, GUILayout.Width(45), GUILayout.Height(26)))
        {
            systemManager.isPaused = false;
            systemManager.SetTimeScale(50f);
        }

        GUILayout.Space(10);
        if (GUILayout.Button(systemManager.showOrbitLines ? "Orbites: OUI" : "Orbites: NON", buttonStyle, GUILayout.Width(100), GUILayout.Height(26)))
        {
            systemManager.ToggleOrbitLines(!systemManager.showOrbitLines);
        }

        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    private void DrawBottomFlightHUD()
    {
        if (playerShip == null) return;

        float hudW = 620f;
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

        GUILayout.BeginVertical();
        GUILayout.Label($"<b>MODE DE VOL :</b> {modeStr}  |  <b>VITESSE :</b> {playerShip.currentSpeed:F1} km/s", bodyStyle);
        GUILayout.Label("Contrôles: [Z/Q/S/D] ou [W/A/S/D] Déplacer | [Shift] Boost | [Clic Droit] Orienter caméra | [Molette] Zoom", bodyStyle);
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
