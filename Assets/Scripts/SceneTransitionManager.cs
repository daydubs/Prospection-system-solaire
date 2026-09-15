using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class SceneTransitionManager : MonoBehaviour
{
    private static SceneTransitionManager instance;
    public static SceneTransitionManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<SceneTransitionManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("SceneTransitionManager");
                    instance = go.AddComponent<SceneTransitionManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }

    public const string SCENE_MAIN_MENU = "MainMenuScene";
    public const string SCENE_EARTH_BASE = "EarthBaseScene";
    public const string SCENE_SOLAR_SYSTEM = "SolarSystemScene";

    [Header("Transition State")]
    public bool isTransitioning = false;
    public float fadeDuration = 0.5f;
    private float fadeAlpha = 0f;
    private Texture2D fadeTexture;

    [Header("Pause State")]
    public bool isGamePaused = false;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        // Create black fade texture
        fadeTexture = new Texture2D(1, 1);
        fadeTexture.SetPixel(0, 0, Color.black);
        fadeTexture.Apply();
    }

    private void Update()
    {
        // Universal Pause Menu Toggle on Escape in gameplay scenes
        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene != SCENE_MAIN_MENU)
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                // If EarthBase Hub UI is open, let EarthBase handle closing its modal first
                if (EarthBaseController.Instance != null && EarthBaseController.Instance.isHubUIOpen)
                {
                    // EarthBaseController closes its own modal
                    return;
                }

                // If MainMenuController exists as a pause overlay
                if (MainMenuController.Instance != null)
                {
                    MainMenuController.Instance.ToggleMenu();
                }
            }
        }
    }

    public void LoadScene(string sceneName, Action onComplete = null)
    {
        if (isTransitioning) return;
        StartCoroutine(TransitionRoutine(sceneName, onComplete));
    }

    public void LoadMainMenu()
    {
        SetPauseState(false);
        LoadScene(SCENE_MAIN_MENU);
    }

    public void LoadEarthBase()
    {
        SetPauseState(false);
        LoadScene(SCENE_EARTH_BASE);
    }

    public void LoadSolarSystem()
    {
        SetPauseState(false);
        LoadScene(SCENE_SOLAR_SYSTEM);
    }

    private IEnumerator TransitionRoutine(string sceneName, Action onComplete)
    {
        isTransitioning = true;

        // Fade to black
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            fadeAlpha = Mathf.Clamp01(timer / fadeDuration);
            yield return null;
        }
        fadeAlpha = 1f;

        // Load scene asynchronously
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (asyncLoad != null && !asyncLoad.isDone)
        {
            yield return null;
        }

        // Wait a frame for Awake/Start to settle physics and transforms
        Physics.SyncTransforms();
        yield return new WaitForEndOfFrame();

        // Trigger callback if any
        onComplete?.Invoke();

        // Fade from black
        timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            fadeAlpha = 1f - Mathf.Clamp01(timer / fadeDuration);
            yield return null;
        }
        fadeAlpha = 0f;
        isTransitioning = false;
    }

    public void SetPauseState(bool pause)
    {
        isGamePaused = pause;
        Time.timeScale = pause ? 0f : 1f;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetTimePaused(pause);
        }

        if (pause)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            string curScene = SceneManager.GetActiveScene().name;
            if (curScene == SCENE_EARTH_BASE)
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

    private void OnGUI()
    {
        if (fadeAlpha > 0f && fadeTexture != null)
        {
            GUI.color = new Color(0, 0, 0, fadeAlpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), fadeTexture);
            GUI.color = Color.white;
        }
    }
}
