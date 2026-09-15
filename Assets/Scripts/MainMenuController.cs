using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    private static MainMenuController instance;
    public static MainMenuController Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<MainMenuController>();
            }
            return instance;
        }
        private set => instance = value;
    }

    [Header("Menu State")]
    public bool isMenuOpen = true;
    public bool hasActiveGame = false;
    public bool isPauseMode = false;

    [Header("Main Panels")]
    public GameObject mainCanvasObject;
    public GameObject mainMenuPanel;
    public GameObject newGamePanel;
    public GameObject loadGamePanel;
    public GameObject optionsPanel;

    [Header("New Game Setup UI")]
    public TMP_InputField playerNameInput;
    public TMP_InputField corpNameInput;
    public UnityEngine.UI.Button easyDifficultyBtn;
    public UnityEngine.UI.Button normalDifficultyBtn;
    public UnityEngine.UI.Button hardDifficultyBtn;
    public TMP_Text difficultyTitleText;
    public TMP_Text difficultyDescText;
    public TMP_Text difficultyCreditsText;
    public TMP_Text difficultyInfluenceText;
    public UnityEngine.UI.Button startNewGameConfirmBtn;
    public UnityEngine.UI.Button backFromNewGameBtn;

    public int selectedDifficulty = 1; // 0 = Facile, 1 = Normal, 2 = Difficile

    [Header("Options Sub-Panels / Tabs")]
    public GameObject soundTabPanel;
    public GameObject graphicsTabPanel;
    public GameObject gameplayTabPanel;

    public UnityEngine.UI.Button soundTabButton;
    public UnityEngine.UI.Button graphicsTabButton;
    public UnityEngine.UI.Button gameplayTabButton;

    public Color activeTabColor = new Color(0f, 0.85f, 1f, 1f);
    public Color inactiveTabColor = new Color(0.2f, 0.35f, 0.5f, 1f);

    [Header("Audio Settings UI")]
    public UnityEngine.UI.Slider masterVolumeSlider;
    public TMP_Text masterVolumeText;
    public UnityEngine.UI.Slider musicVolumeSlider;
    public TMP_Text musicVolumeText;
    public UnityEngine.UI.Slider sfxVolumeSlider;
    public TMP_Text sfxVolumeText;
    public UnityEngine.UI.Slider radioVolumeSlider;
    public TMP_Text radioVolumeText;
    public UnityEngine.UI.Toggle muteToggle;

    [Header("Graphics Settings UI")]
    public TMP_Dropdown qualityDropdown;
    public UnityEngine.UI.Toggle fullscreenToggle;
    public UnityEngine.UI.Toggle vSyncToggle;
    public UnityEngine.UI.Toggle bloomToggle;

    [Header("Gameplay Settings UI")]
    public UnityEngine.UI.Slider sensitivitySlider;
    public TMP_Text sensitivityText;
    public UnityEngine.UI.Toggle invertYToggle;
    public TMP_Dropdown controlSchemeDropdown;
    public UnityEngine.UI.Toggle flightAssistToggle;

    [Header("Notification / Status Toast")]
    public GameObject notificationBanner;
    public TMP_Text notificationText;

    [Header("Buttons")]
    public UnityEngine.UI.Button newGameButton;
    public UnityEngine.UI.Button continueButton;
    public UnityEngine.UI.Button loadGameButton;
    public UnityEngine.UI.Button optionsButton;
    public UnityEngine.UI.Button quitButton;

    [Header("Sound Effect Hook")]
    public AudioSource menuAudioSource;

    private Coroutine notificationCoroutine;

    public bool IsMenuOpen => isMenuOpen;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        bool isMainMenuScene = currentScene == SceneTransitionManager.SCENE_MAIN_MENU;

        InitializeDefaultSettings();
        InitializeButtons();
        SelectDifficulty(1); // Default to normal difficulty
        ShowMainMenu();

        if (isMainMenuScene)
        {
            isPauseMode = false;
            SetMenuVisibility(true);
        }
        else
        {
            // In gameplay scene: menu acts as pause overlay, initially closed
            isPauseMode = true;
            hasActiveGame = true;
            SetMenuVisibility(false);
        }
    }

    private void InitializeButtons()
    {
        if (newGameButton != null) newGameButton.onClick.AddListener(OnClickOpenNewGame);
        if (continueButton != null) continueButton.onClick.AddListener(OnClickContinue);
        if (loadGameButton != null) loadGameButton.onClick.AddListener(OnClickOpenLoadGame);
        if (optionsButton != null) optionsButton.onClick.AddListener(OnClickOpenOptions);
        if (quitButton != null) quitButton.onClick.AddListener(OnClickQuit);

        if (startNewGameConfirmBtn != null) startNewGameConfirmBtn.onClick.AddListener(OnClickConfirmStartNewGame);
        if (backFromNewGameBtn != null) backFromNewGameBtn.onClick.AddListener(ShowMainMenu);

        if (easyDifficultyBtn != null) easyDifficultyBtn.onClick.AddListener(() => SelectDifficulty(0));
        if (normalDifficultyBtn != null) normalDifficultyBtn.onClick.AddListener(() => SelectDifficulty(1));
        if (hardDifficultyBtn != null) hardDifficultyBtn.onClick.AddListener(() => SelectDifficulty(2));

        if (soundTabButton != null) soundTabButton.onClick.AddListener(() => SelectOptionsTab(0));
        if (graphicsTabButton != null) graphicsTabButton.onClick.AddListener(() => SelectOptionsTab(1));
        if (gameplayTabButton != null) gameplayTabButton.onClick.AddListener(() => SelectOptionsTab(2));

        // Wire Load Game panel buttons
        if (loadGamePanel != null)
        {
            var loadBackBtn = loadGamePanel.transform.Find("Btn_Retour")?.GetComponent<UnityEngine.UI.Button>();
            if (loadBackBtn != null) loadBackBtn.onClick.AddListener(ShowMainMenu);

            var slot1 = loadGamePanel.transform.Find("Slots_Container/Slot_1/Btn_LoadSlot")?.GetComponent<UnityEngine.UI.Button>();
            if (slot1 != null) slot1.onClick.AddListener(() => OnClickLoadSlot(1));

            var slot2 = loadGamePanel.transform.Find("Slots_Container/Slot_2/Btn_LoadSlot")?.GetComponent<UnityEngine.UI.Button>();
            if (slot2 != null) slot2.onClick.AddListener(() => OnClickLoadSlot(2));

            var slot3 = loadGamePanel.transform.Find("Slots_Container/Slot_3/Btn_LoadSlot")?.GetComponent<UnityEngine.UI.Button>();
            if (slot3 != null) slot3.onClick.AddListener(() => OnClickLoadSlot(3));
        }

        // Wire Options panel buttons
        if (optionsPanel != null)
        {
            var optBackBtn = optionsPanel.transform.Find("BottomBar/Btn_Retour")?.GetComponent<UnityEngine.UI.Button>();
            if (optBackBtn != null) optBackBtn.onClick.AddListener(ShowMainMenu);

            var optApplyBtn = optionsPanel.transform.Find("BottomBar/Btn_Appliquer")?.GetComponent<UnityEngine.UI.Button>();
            if (optApplyBtn != null) optApplyBtn.onClick.AddListener(OnApplySettings);
        }
    }

    private void Update()
    {
        // Toggle menu on Escape
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (newGamePanel != null && newGamePanel.activeSelf)
            {
                ShowMainMenu();
            }
            else if (optionsPanel != null && optionsPanel.activeSelf)
            {
                ShowMainMenu();
            }
            else if (loadGamePanel != null && loadGamePanel.activeSelf)
            {
                ShowMainMenu();
            }
            else
            {
                // In main menu scene, Escape does nothing or opens main panel
                string currentScene = SceneManager.GetActiveScene().name;
                if (currentScene != SceneTransitionManager.SCENE_MAIN_MENU)
                {
                    ToggleMenu();
                }
            }
        }
    }

    public void SetMenuVisibility(bool visible)
    {
        isMenuOpen = visible;
        if (mainCanvasObject != null)
        {
            mainCanvasObject.SetActive(visible);
        }

        // Control pause state via SceneTransitionManager
        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene != SceneTransitionManager.SCENE_MAIN_MENU)
        {
            if (SceneTransitionManager.Instance != null)
            {
                SceneTransitionManager.Instance.SetPauseState(visible);
            }
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // Pause / Unpause solar system
        if (SolarSystemManager.Instance != null)
        {
            SolarSystemManager.Instance.isPaused = visible;
        }

        // Enable or disable flight controls while menu is active
        SpaceshipFlightController ship = FindAnyObjectByType<SpaceshipFlightController>();
        if (ship != null)
        {
            ship.enabled = !visible;
        }
    }

    public void ToggleMenu()
    {
        SetMenuVisibility(!isMenuOpen);
    }

    #region Navigation Methods

    public void OnClickOpenNewGame()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (loadGamePanel != null) loadGamePanel.SetActive(false);
        if (newGamePanel != null) newGamePanel.SetActive(true);

        // Pre-fill inputs if empty
        if (playerNameInput != null && string.IsNullOrEmpty(playerNameInput.text))
        {
            playerNameInput.text = "Commandant Dany";
        }
        if (corpNameInput != null && string.IsNullOrEmpty(corpNameInput.text))
        {
            corpNameInput.text = "Astraea Mining Corp";
        }

        SelectDifficulty(selectedDifficulty);
    }

    public void SelectDifficulty(int index)
    {
        selectedDifficulty = Mathf.Clamp(index, 0, 2);

        // Update button visual styles
        SetDifficultyBtnState(easyDifficultyBtn, selectedDifficulty == 0, new Color(0.2f, 0.8f, 0.4f, 1f));
        SetDifficultyBtnState(normalDifficultyBtn, selectedDifficulty == 1, new Color(0f, 0.75f, 1f, 1f));
        SetDifficultyBtnState(hardDifficultyBtn, selectedDifficulty == 2, new Color(1f, 0.4f, 0.2f, 1f));

        switch (selectedDifficulty)
        {
            case 0: // Facile
                if (difficultyTitleText != null) difficultyTitleText.text = "<color=#44ff88>[FACILE] PIONNIER SUBVENTIONNÉ</color>";
                if (difficultyDescText != null) difficultyDescText.text = "Subventions gouvernementales complètes et équipement spatial optimisé. Idéal pour explorer le système solaire sans stress financier.";
                if (difficultyCreditsText != null) difficultyCreditsText.text = "<b>Capital de départ :</b> <color=#ffd700>50 000 CR</color>";
                if (difficultyInfluenceText != null) difficultyInfluenceText.text = "<b>Influence initiale :</b> <color=#44ff88>Terre +25% | Mars +20% | Ceinture +20% | Jupiter +20%</color>";
                break;

            case 1: // Normal
                if (difficultyTitleText != null) difficultyTitleText.text = "<color=#00e5ff>[NORMAL] PROSPECTEUR INDÉPENDANT</color>";
                if (difficultyDescText != null) difficultyDescText.text = "Prêt bancaire standard et vaisseau d'exploration d'origine. Équilibre parfait entre gestion des ressources et liberté de navigation.";
                if (difficultyCreditsText != null) difficultyCreditsText.text = "<b>Capital de départ :</b> <color=#ffd700>25 000 CR</color>";
                if (difficultyInfluenceText != null) difficultyInfluenceText.text = "<b>Influence initiale :</b> <color=#00e5ff>Terre +10% | Ceinture +5% | Mars 0% | Jupiter 0%</color>";
                break;

            case 2: // Difficile
                if (difficultyTitleText != null) difficultyTitleText.text = "<color=#ff5533>[DIFFICILE] VÉTÉRAN ENDETTÉ</color>";
                if (difficultyDescText != null) difficultyDescText.text = "Dette colossale, réputation compromise auprès des corporations terriennes et fonds d'urgence réduits au strict minimum.";
                if (difficultyCreditsText != null) difficultyCreditsText.text = "<b>Capital de départ :</b> <color=#ffd700>10 000 CR</color>";
                if (difficultyInfluenceText != null) difficultyInfluenceText.text = "<b>Influence initiale :</b> <color=#ff5533>Terre -10% | Mars -5% | Ceinture +10% | Jupiter -10%</color>";
                break;
        }
    }

    private void SetDifficultyBtnState(UnityEngine.UI.Button btn, bool isSelected, Color activeBorderColor)
    {
        if (btn == null) return;
        var colors = btn.colors;
        colors.normalColor = isSelected ? activeBorderColor : inactiveTabColor;
        colors.selectedColor = isSelected ? activeBorderColor : inactiveTabColor;
        colors.highlightedColor = isSelected ? activeBorderColor : new Color(0.3f, 0.45f, 0.6f, 1f);
        btn.colors = colors;
    }

    public void OnClickConfirmStartNewGame()
    {
        string pilot = playerNameInput != null && !string.IsNullOrWhiteSpace(playerNameInput.text) 
            ? playerNameInput.text.Trim() 
            : "Commandant Dany";

        string corp = corpNameInput != null && !string.IsNullOrWhiteSpace(corpNameInput.text) 
            ? corpNameInput.text.Trim() 
            : "Astraea Mining Corp";

        hasActiveGame = true;
        if (continueButton != null)
        {
            continueButton.interactable = true;
        }

        if (GameManager.Instance == null)
        {
            GameObject gm = new GameObject("GameManager");
            gm.AddComponent<GameManager>();
        }

        GameManager.Instance.StartNewGame(pilot, corp, selectedDifficulty);

        ShowNotification($"Lancement de la mission pour {pilot} ({corp}) !");

        // Transition to EarthBaseScene
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadEarthBase();
        }
        else
        {
            SceneManager.LoadScene(SceneTransitionManager.SCENE_EARTH_BASE);
        }
    }

    public void OnClickNewGame()
    {
        OnClickOpenNewGame();
    }

    public void OnClickContinue()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene == SceneTransitionManager.SCENE_MAIN_MENU)
        {
            if (!hasActiveGame)
            {
                OnClickConfirmStartNewGame();
                return;
            }
            if (SceneTransitionManager.Instance != null)
            {
                SceneTransitionManager.Instance.LoadEarthBase();
            }
            else
            {
                SceneManager.LoadScene(SceneTransitionManager.SCENE_EARTH_BASE);
            }
        }
        else
        {
            // Resume in-game
            SetMenuVisibility(false);
        }
    }

    public void OnClickOpenLoadGame()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (newGamePanel != null) newGamePanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (loadGamePanel != null) loadGamePanel.SetActive(true);
    }

    public void OnClickLoadSlot(int slotIndex)
    {
        hasActiveGame = true;
        if (continueButton != null) continueButton.interactable = true;

        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.HasSaveGame(slotIndex))
            {
                GameManager.Instance.LoadGame(slotIndex);
            }
            else
            {
                GameManager.Instance.SaveGame(slotIndex);
            }
        }

        string slotName = slotIndex switch
        {
            1 => "Secteur Terre - Station Orbitale Alpha",
            2 => "Ceinture Principale - Mine de Cérès",
            3 => "Système Jovien - Base Ganymède",
            _ => $"Sauvegarde #{slotIndex}"
        };

        ShowNotification($"📂 Chargement: {slotName}...");

        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadEarthBase();
        }
        else
        {
            SceneManager.LoadScene(SceneTransitionManager.SCENE_EARTH_BASE);
        }
    }

    public void OnClickOpenOptions()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (newGamePanel != null) newGamePanel.SetActive(false);
        if (loadGamePanel != null) loadGamePanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(true);
        SelectOptionsTab(0); // Default to Sound tab
    }

    public void ShowMainMenu()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (newGamePanel != null) newGamePanel.SetActive(false);
        if (loadGamePanel != null) loadGamePanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);
    }

    public void OnClickQuit()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene != SceneTransitionManager.SCENE_MAIN_MENU)
        {
            // In game: return to Main Menu scene
            ShowNotification("Retour au menu principal...");
            if (SceneTransitionManager.Instance != null)
            {
                SceneTransitionManager.Instance.LoadMainMenu();
            }
            else
            {
                SceneManager.LoadScene(SceneTransitionManager.SCENE_MAIN_MENU);
            }
        }
        else
        {
            ShowNotification("Fermeture du système...");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

    #endregion

    #region Options Tabs

    public void SelectOptionsTab(int tabIndex)
    {
        if (soundTabPanel != null) soundTabPanel.SetActive(tabIndex == 0);
        if (graphicsTabPanel != null) graphicsTabPanel.SetActive(tabIndex == 1);
        if (gameplayTabPanel != null) gameplayTabPanel.SetActive(tabIndex == 2);

        UpdateTabButtonColors(tabIndex);
    }

    private void UpdateTabButtonColors(int selectedIndex)
    {
        SetTabBtnColor(soundTabButton, selectedIndex == 0);
        SetTabBtnColor(graphicsTabButton, selectedIndex == 1);
        SetTabBtnColor(gameplayTabButton, selectedIndex == 2);
    }

    private void SetTabBtnColor(UnityEngine.UI.Button btn, bool isSelected)
    {
        if (btn == null) return;
        var colors = btn.colors;
        colors.normalColor = isSelected ? activeTabColor : inactiveTabColor;
        colors.selectedColor = isSelected ? activeTabColor : inactiveTabColor;
        btn.colors = colors;
    }

    #endregion

    #region Settings Callbacks

    private void InitializeDefaultSettings()
    {
        // Sound
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.value = AudioListener.volume > 0 ? AudioListener.volume : 0.8f;
            OnMasterVolumeChanged(masterVolumeSlider.value);
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        }
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.value = 0.75f;
            OnMusicVolumeChanged(0.75f);
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.value = 0.9f;
            OnSfxVolumeChanged(0.9f);
            sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
        }
        if (radioVolumeSlider != null)
        {
            radioVolumeSlider.value = 0.85f;
            OnRadioVolumeChanged(0.85f);
            radioVolumeSlider.onValueChanged.AddListener(OnRadioVolumeChanged);
        }
        if (muteToggle != null)
        {
            muteToggle.isOn = false;
            muteToggle.onValueChanged.AddListener(OnMuteToggled);
        }

        // Graphics
        if (qualityDropdown != null)
        {
            qualityDropdown.value = QualitySettings.GetQualityLevel();
            qualityDropdown.onValueChanged.AddListener(OnQualityLevelChanged);
        }
        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = Screen.fullScreen;
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggled);
        }
        if (vSyncToggle != null)
        {
            vSyncToggle.isOn = QualitySettings.vSyncCount > 0;
            vSyncToggle.onValueChanged.AddListener(OnVSyncToggled);
        }
        if (bloomToggle != null)
        {
            bloomToggle.isOn = true;
            bloomToggle.onValueChanged.AddListener(OnBloomToggled);
        }

        // Gameplay
        if (sensitivitySlider != null)
        {
            sensitivitySlider.value = 2.0f;
            OnSensitivityChanged(2.0f);
            sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        }
        if (invertYToggle != null)
        {
            invertYToggle.isOn = false;
            invertYToggle.onValueChanged.AddListener(OnInvertYToggled);
        }
        if (controlSchemeDropdown != null)
        {
            controlSchemeDropdown.value = 0; // AZERTY by default
            controlSchemeDropdown.onValueChanged.AddListener(OnControlSchemeChanged);
        }
        if (flightAssistToggle != null)
        {
            flightAssistToggle.isOn = true;
            flightAssistToggle.onValueChanged.AddListener(OnFlightAssistToggled);
        }
    }

    public void OnMasterVolumeChanged(float val)
    {
        AudioListener.volume = muteToggle != null && muteToggle.isOn ? 0f : val;
        if (masterVolumeText != null) masterVolumeText.text = $"{Mathf.RoundToInt(val * 100)}%";
    }

    public void OnMusicVolumeChanged(float val)
    {
        if (musicVolumeText != null) musicVolumeText.text = $"{Mathf.RoundToInt(val * 100)}%";
    }

    public void OnSfxVolumeChanged(float val)
    {
        if (sfxVolumeText != null) sfxVolumeText.text = $"{Mathf.RoundToInt(val * 100)}%";
    }

    public void OnRadioVolumeChanged(float val)
    {
        if (radioVolumeText != null) radioVolumeText.text = $"{Mathf.RoundToInt(val * 100)}%";
    }

    public void OnMuteToggled(bool isMuted)
    {
        AudioListener.volume = isMuted ? 0f : (masterVolumeSlider != null ? masterVolumeSlider.value : 0.8f);
    }

    public void OnQualityLevelChanged(int level)
    {
        QualitySettings.SetQualityLevel(level, true);
        ShowNotification($"Qualité graphique : {QualitySettings.names[level]}");
    }

    public void OnFullscreenToggled(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }

    public void OnVSyncToggled(bool enableVSync)
    {
        QualitySettings.vSyncCount = enableVSync ? 1 : 0;
    }

    public void OnBloomToggled(bool enableBloom)
    {
        ShowNotification(enableBloom ? "Effets Bloom : Activés" : "Effets Bloom : Désactivés");
    }

    public void OnSensitivityChanged(float val)
    {
        if (sensitivityText != null) sensitivityText.text = $"{val:F1}x";
        SpaceshipFlightController ship = FindAnyObjectByType<SpaceshipFlightController>();
        if (ship != null)
        {
            ship.mouseSensitivity = val;
        }
    }

    public void OnInvertYToggled(bool inverted)
    {
        ShowNotification(inverted ? "Axe Y Inversé" : "Axe Y Normal");
    }

    public void OnControlSchemeChanged(int scheme)
    {
        string schemeName = scheme == 0 ? "AZERTY (Z/Q/S/D)" : "QWERTY (W/A/S/D)";
        ShowNotification($"Configuration touches : {schemeName}");
    }

    public void OnFlightAssistToggled(bool assist)
    {
        ShowNotification(assist ? "Stabilisation : Active" : "Stabilisation : Manuelle");
    }

    public void OnApplySettings()
    {
        ShowNotification("✓ Paramètres sauvegardés et appliqués !");
        ShowMainMenu();
    }

    #endregion

    #region Notifications

    public void ShowNotification(string message)
    {
        if (notificationBanner == null || notificationText == null) return;

        notificationText.text = message;
        notificationBanner.SetActive(true);

        if (notificationCoroutine != null)
        {
            StopCoroutine(notificationCoroutine);
        }
        notificationCoroutine = StartCoroutine(HideNotificationAfterDelay(2.5f));
    }

    private IEnumerator HideNotificationAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (notificationBanner != null)
        {
            notificationBanner.SetActive(false);
        }
    }

    #endregion
}

