using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    private static AudioManager instance;
    public static AudioManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<AudioManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("AudioManager");
                    instance = go.AddComponent<AudioManager>();
                }
            }
            return instance;
        }
    }

    [Header("Volume Settings (0.0 to 1.0)")]
    [Range(0f, 1f)] [SerializeField] private float masterVolume = 0.8f;
    [Range(0f, 1f)] [SerializeField] private float musicVolume = 0.75f;
    [Range(0f, 1f)] [SerializeField] private float sfxVolume = 0.9f;
    [Range(0f, 1f)] [SerializeField] private float ambienceVolume = 0.6f;
    [Range(0f, 1f)] [SerializeField] private float uiVolume = 0.85f;
    [SerializeField] private bool isMuted = false;

    public float MasterVolume => masterVolume;
    public float MusicVolume => musicVolume;
    public float SfxVolume => sfxVolume;
    public float AmbienceVolume => ambienceVolume;
    public float UiVolume => uiVolume;
    public bool IsMuted => isMuted;

    [Header("Music Tracks")]
    public AudioClip mainMenuMusic;
    public AudioClip spaceMusic;
    public AudioClip earthBaseMusic;
    public AudioClip moonMusic;

    [Header("Ambience Tracks")]
    public AudioClip spaceAmbience;
    public AudioClip baseAmbience;
    public AudioClip moonAmbience;

    [Header("UI Sound Effects")]
    public AudioClip buttonClickSound;
    public AudioClip buttonHoverSound;
    public AudioClip tabSwitchSound;
    public AudioClip menuOpenSound;
    public AudioClip menuCloseSound;
    public AudioClip notificationSound;

    [Header("Gameplay Sound Effects")]
    public AudioClip footstepSound;
    public AudioClip jumpSound;
    public AudioClip landSound;
    public AudioClip itemPickupSound;
    public AudioClip doorOpenSound;
    public AudioClip doorCloseSound;
    public AudioClip miningLaserSound;
    public AudioClip resourceMinedSound;
    public AudioClip craftSuccessSound;
    public AudioClip constructionSound;
    public AudioClip shipWarpSound;
    public AudioClip shipPassingSound;

    // AudioSources
    private AudioSource musicSourceA;
    private AudioSource musicSourceB;
    private bool isSourceAPlaying = false;
    private Coroutine musicCrossfadeCoroutine;

    private AudioSource ambienceSource;
    private AudioSource uiSource;
    private List<AudioSource> sfxSourcePool = new List<AudioSource>();
    private const int SFX_POOL_SIZE = 12;

    private float footstepTimer = 0f;
    private const float WALK_STEP_INTERVAL = 0.45f;
    private const float SPRINT_STEP_INTERVAL = 0.3f;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        LoadSavedVolumePreferences();
        InitializeAudioSources();
        AutoLoadClipsIfNull();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        // Play appropriate music for currently active scene
        PlayMusicForScene(SceneManager.GetActiveScene().name);
    }

    private void InitializeAudioSources()
    {
        // Music Sources for Crossfade
        musicSourceA = CreateAudioSource("Music_Source_A", true);
        musicSourceB = CreateAudioSource("Music_Source_B", true);
        musicSourceA.ignoreListenerPause = true; // Music continues during menu pause
        musicSourceB.ignoreListenerPause = true;

        // Ambience Source
        ambienceSource = CreateAudioSource("Ambience_Source", true);
        ambienceSource.ignoreListenerPause = false;

        // UI Source (ignoreListenerPause = true so UI sounds work while paused)
        uiSource = CreateAudioSource("UI_Source", false);
        uiSource.ignoreListenerPause = true;

        // SFX Source Pool
        sfxSourcePool.Clear();
        for (int i = 0; i < SFX_POOL_SIZE; i++)
        {
            AudioSource src = CreateAudioSource($"SFX_Source_{i}", false);
            src.ignoreListenerPause = false;
            sfxSourcePool.Add(src);
        }

        UpdateAllSourceVolumes();
    }

    private AudioSource CreateAudioSource(string name, bool loop)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(transform, false);
        AudioSource source = child.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f; // 2D by default
        return source;
    }

    private void AutoLoadClipsIfNull()
    {
#if UNITY_EDITOR
        if (mainMenuMusic == null)
            mainMenuMusic = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Various Genres Music Collection/Loop/Ambient Loop/OvertureMenuMusic.ogg");

        if (spaceMusic == null)
            spaceMusic = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Various Genres Music Collection/Loop/Ambient Loop/Dark Ambient 01.ogg");

        if (earthBaseMusic == null)
            earthBaseMusic = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Various Genres Music Collection/Loop/Dreamy Loop/Movie Dreamy 01.ogg");

        if (moonMusic == null)
            moonMusic = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Various Genres Music Collection/Loop/Cinema Loop/Emotional Cinema 01.ogg");

        if (spaceAmbience == null)
            spaceAmbience = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Libraries/Soundbits_freeSFX_2025/Sounds/dark_scifi_drone_mixed_017.wav");

        if (baseAmbience == null)
            baseAmbience = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Free Pack/Cavern Atmosphere - Loop.wav");

        if (buttonClickSound == null)
            buttonClickSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/UI Soundpack/MP3/Modern1.mp3");

        if (buttonHoverSound == null)
            buttonHoverSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/UI Soundpack/MP3/Modern2.mp3");

        if (tabSwitchSound == null)
            tabSwitchSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/UI Soundpack/MP3/Modern4.mp3");

        if (menuCloseSound == null)
            menuCloseSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Libraries/Soundbits_freeSFX_2025/Sounds/csfx-2_menu-close_17.wav");

        if (notificationSound == null)
            notificationSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/UI Soundpack/MP3/Modern6.mp3");

        if (footstepSound == null)
            footstepSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Libraries/Soundbits_freeSFX_2025/Sounds/jc_footsteps-addon_12.wav");

        if (jumpSound == null)
            jumpSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Libraries/Soundbits_freeSFX_2025/Sounds/wi2_whooshes_024_04.wav");

        if (landSound == null)
            landSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Libraries/Soundbits_freeSFX_2025/Sounds/wi_impact_01.wav");

        if (itemPickupSound == null)
            itemPickupSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Libraries/Soundbits_freeSFX_2025/Sounds/blacksmith_metal_pickup_putdown_07.wav");

        if (doorOpenSound == null)
            doorOpenSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Libraries/Soundbits_freeSFX_2025/Sounds/oc_door-metal-02-01.wav");

        if (doorCloseSound == null)
            doorCloseSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Libraries/Soundbits_freeSFX_2025/Sounds/oc_door-metal-02-01.wav");

        if (miningLaserSound == null)
            miningLaserSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Free Pack/Laser Gun Salve.wav");

        if (resourceMinedSound == null)
            resourceMinedSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Libraries/Soundbits_freeSFX_2025/Sounds/cs-d_stone_crash_designed_017.wav");

        if (craftSuccessSound == null)
            craftSuccessSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Libraries/Soundbits_freeSFX_2025/Sounds/emt2_fine-blanking-tool_03.wav");

        if (constructionSound == null)
            constructionSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Libraries/Soundbits_freeSFX_2025/Sounds/ji-e2_metal_hit_crash-200.wav");

        if (shipWarpSound == null)
            shipWarpSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Free Pack/Spaceship passing by.wav");

        if (shipPassingSound == null)
            shipPassingSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Libraries/Soundbits_freeSFX_2025/Sounds/jw4_spacecrafts-scifi-022.wav");
#endif
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CleanDuplicateAudioListeners();
        PlayMusicForScene(scene.name);
    }

    public void CleanDuplicateAudioListeners()
    {
        var listeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude);
        if (listeners.Length > 1)
        {
            // Keep the one on Player or Main Camera, disable others
            bool keptOne = false;
            foreach (var l in listeners)
            {
                if (!keptOne && (l.gameObject.name.Contains("Player") || l.gameObject.CompareTag("MainCamera")))
                {
                    keptOne = true;
                    l.enabled = true;
                }
                else if (keptOne)
                {
                    l.enabled = false;
                }
            }
        }
    }

    private void PlayMusicForScene(string sceneName)
    {
        switch (sceneName)
        {
            case SceneTransitionManager.SCENE_MAIN_MENU:
                PlayMusic(mainMenuMusic, 1.2f);
                StopAmbience(1.0f);
                break;
            case SceneTransitionManager.SCENE_SOLAR_SYSTEM:
                PlayMusic(spaceMusic, 1.5f);
                PlayAmbience(spaceAmbience, 2.0f);
                break;
            case SceneTransitionManager.SCENE_EARTH_BASE:
                PlayMusic(earthBaseMusic, 1.5f);
                PlayAmbience(baseAmbience, 2.0f);
                break;
            case SceneTransitionManager.SCENE_MOON:
                PlayMusic(moonMusic, 1.5f);
                PlayAmbience(moonAmbience != null ? moonAmbience : spaceAmbience, 2.0f);
                break;
            default:
                if (mainMenuMusic != null) PlayMusic(mainMenuMusic, 1.5f);
                break;
        }
    }

    #region Music & Ambience

    public void PlayMusic(AudioClip newTrack, float fadeDuration = 1.2f, bool loop = true)
    {
        if (newTrack == null) return;

        AudioSource currentSource = isSourceAPlaying ? musicSourceA : musicSourceB;
        AudioSource newSource = isSourceAPlaying ? musicSourceB : musicSourceA;

        // If the same clip is already playing, just ensure volume is correct
        if (currentSource != null && currentSource.isPlaying && currentSource.clip == newTrack)
        {
            return;
        }

        if (musicCrossfadeCoroutine != null)
        {
            StopCoroutine(musicCrossfadeCoroutine);
        }

        musicCrossfadeCoroutine = StartCoroutine(CrossfadeMusicRoutine(currentSource, newSource, newTrack, fadeDuration, loop));
    }

    private IEnumerator CrossfadeMusicRoutine(AudioSource fromSource, AudioSource toSource, AudioClip newTrack, float duration, bool loop)
    {
        toSource.clip = newTrack;
        toSource.loop = loop;
        toSource.volume = 0f;
        toSource.Play();

        float startFromVol = fromSource != null ? fromSource.volume : 0f;
        float targetToVol = GetEffectiveMusicVolume();

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            if (fromSource != null && fromSource.isPlaying)
            {
                fromSource.volume = Mathf.Lerp(startFromVol, 0f, t);
            }
            toSource.volume = Mathf.Lerp(0f, targetToVol, t);

            yield return null;
        }

        if (fromSource != null)
        {
            fromSource.Stop();
            fromSource.clip = null;
        }

        toSource.volume = targetToVol;
        isSourceAPlaying = !isSourceAPlaying;
        musicCrossfadeCoroutine = null;
    }

    public void StopMusic(float fadeDuration = 1.0f)
    {
        if (musicCrossfadeCoroutine != null)
        {
            StopCoroutine(musicCrossfadeCoroutine);
        }
        StartCoroutine(FadeOutSource(isSourceAPlaying ? musicSourceA : musicSourceB, fadeDuration));
    }

    public void PlayAmbience(AudioClip clip, float fadeDuration = 1.5f)
    {
        if (ambienceSource == null) return;
        if (clip == null)
        {
            StopAmbience(fadeDuration);
            return;
        }

        if (ambienceSource.isPlaying && ambienceSource.clip == clip)
        {
            ambienceSource.volume = GetEffectiveAmbienceVolume();
            return;
        }

        StartCoroutine(FadeInAmbience(clip, fadeDuration));
    }

    public void StopAmbience(float fadeDuration = 1.0f)
    {
        if (ambienceSource != null && ambienceSource.isPlaying)
        {
            StartCoroutine(FadeOutSource(ambienceSource, fadeDuration));
        }
    }

    private IEnumerator FadeInAmbience(AudioClip clip, float duration)
    {
        ambienceSource.clip = clip;
        ambienceSource.loop = true;
        ambienceSource.volume = 0f;
        ambienceSource.Play();

        float targetVol = GetEffectiveAmbienceVolume();
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            ambienceSource.volume = Mathf.Lerp(0f, targetVol, elapsed / duration);
            yield return null;
        }
        ambienceSource.volume = targetVol;
    }

    private IEnumerator FadeOutSource(AudioSource src, float duration)
    {
        if (src == null || !src.isPlaying) yield break;

        float startVol = src.volume;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            src.volume = Mathf.Lerp(startVol, 0f, elapsed / duration);
            yield return null;
        }
        src.Stop();
        src.clip = null;
    }

    #endregion

    #region SFX & UI

    public void PlaySFX(AudioClip clip, float volumeScale = 1f, float pitchMin = 0.95f, float pitchMax = 1.05f)
    {
        if (clip == null || isMuted) return;

        AudioSource source = GetAvailableSFXSource();
        if (source != null)
        {
            source.pitch = Random.Range(pitchMin, pitchMax);
            source.volume = GetEffectiveSFXVolume() * Mathf.Clamp01(volumeScale);
            source.PlayOneShot(clip);
        }
    }

    public void PlaySFXAtPosition(AudioClip clip, Vector3 position, float volumeScale = 1f, float minDistance = 1f, float maxDistance = 35f)
    {
        if (clip == null || isMuted) return;

        GameObject tempAudio = new GameObject($"TempAudio_{clip.name}");
        tempAudio.transform.position = position;

        AudioSource src = tempAudio.AddComponent<AudioSource>();
        src.clip = clip;
        src.spatialBlend = 1f; // 3D
        src.minDistance = minDistance;
        src.maxDistance = maxDistance;
        src.rolloffMode = AudioRolloffMode.Linear;
        src.volume = GetEffectiveSFXVolume() * Mathf.Clamp01(volumeScale);
        src.pitch = Random.Range(0.96f, 1.04f);
        src.Play();

        Destroy(tempAudio, clip.length + 0.1f);
    }

    public void PlayUISound(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null || isMuted || uiSource == null) return;
        uiSource.pitch = 1f;
        uiSource.PlayOneShot(clip, GetEffectiveUIVolume() * Mathf.Clamp01(volumeScale));
    }

    private AudioSource GetAvailableSFXSource()
    {
        foreach (var src in sfxSourcePool)
        {
            if (!src.isPlaying) return src;
        }
        // If all are playing, reuse first one
        return sfxSourcePool.Count > 0 ? sfxSourcePool[0] : null;
    }

    #endregion

    #region Dedicated Action Triggers

    public void PlayButtonClick()
    {
        PlayUISound(buttonClickSound, 0.9f);
    }

    public void PlayButtonHover()
    {
        PlayUISound(buttonHoverSound, 0.6f);
    }

    public void PlayTabSwitch()
    {
        PlayUISound(tabSwitchSound, 0.85f);
    }

    public void PlayMenuClose()
    {
        PlayUISound(menuCloseSound, 0.85f);
    }

    public void PlayNotification()
    {
        PlayUISound(notificationSound, 1f);
    }

    public void StepUpdate(bool isMoving, bool isSprinting)
    {
        if (!isMoving)
        {
            footstepTimer = 0f;
            return;
        }

        float interval = isSprinting ? SPRINT_STEP_INTERVAL : WALK_STEP_INTERVAL;
        footstepTimer += Time.deltaTime;

        if (footstepTimer >= interval)
        {
            footstepTimer = 0f;
            PlayFootstep();
        }
    }

    public void PlayFootstep()
    {
        PlaySFX(footstepSound, 0.7f, 0.88f, 1.15f);
    }

    public void PlayJump()
    {
        PlaySFX(jumpSound, 0.8f, 0.95f, 1.05f);
    }

    public void PlayLand()
    {
        PlaySFX(landSound, 0.85f, 0.9f, 1.1f);
    }

    public void PlayItemPickup()
    {
        PlaySFX(itemPickupSound, 0.9f, 0.95f, 1.1f);
    }

    public void PlayDoorOpen()
    {
        PlaySFX(doorOpenSound, 0.85f, 0.95f, 1.05f);
    }

    public void PlayDoorClose()
    {
        PlaySFX(doorCloseSound, 0.85f, 0.92f, 1.02f);
    }

    public void PlayMiningHit()
    {
        PlaySFX(miningLaserSound, 0.8f, 0.9f, 1.1f);
    }

    public void PlayResourceMined()
    {
        PlaySFX(resourceMinedSound, 0.9f, 0.95f, 1.05f);
    }

    public void PlayCraftSuccess()
    {
        PlaySFX(craftSuccessSound, 1.0f, 1.0f, 1.0f);
    }

    public void PlayConstructionSound()
    {
        PlaySFX(constructionSound, 0.85f, 0.95f, 1.05f);
    }

    public void PlayShipWarp()
    {
        PlaySFX(shipWarpSound, 1.0f, 0.95f, 1.05f);
    }

    #endregion

    #region Volume & Preference Management

    public void SetMasterVolume(float val)
    {
        masterVolume = Mathf.Clamp01(val);
        UpdateAllSourceVolumes();
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
        PlayerPrefs.Save();
    }

    public void SetMusicVolume(float val)
    {
        musicVolume = Mathf.Clamp01(val);
        UpdateMusicVolume();
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
        PlayerPrefs.Save();
    }

    public void SetSFXVolume(float val)
    {
        sfxVolume = Mathf.Clamp01(val);
        UpdateAllSourceVolumes();
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
        PlayerPrefs.Save();
    }

    public void SetAmbienceVolume(float val)
    {
        ambienceVolume = Mathf.Clamp01(val);
        if (ambienceSource != null) ambienceSource.volume = GetEffectiveAmbienceVolume();
        PlayerPrefs.SetFloat("AmbienceVolume", ambienceVolume);
        PlayerPrefs.Save();
    }

    public void SetMute(bool mute)
    {
        isMuted = mute;
        AudioListener.volume = isMuted ? 0f : 1f;
        UpdateAllSourceVolumes();
        PlayerPrefs.SetInt("IsMuted", isMuted ? 1 : 0);
        PlayerPrefs.Save();
    }

    public float GetEffectiveMusicVolume() => isMuted ? 0f : masterVolume * musicVolume;
    public float GetEffectiveSFXVolume() => isMuted ? 0f : masterVolume * sfxVolume;
    public float GetEffectiveAmbienceVolume() => isMuted ? 0f : masterVolume * ambienceVolume;
    public float GetEffectiveUIVolume() => isMuted ? 0f : masterVolume * uiVolume;

    private void UpdateMusicVolume()
    {
        float vol = GetEffectiveMusicVolume();
        if (isSourceAPlaying && musicSourceA != null) musicSourceA.volume = vol;
        else if (!isSourceAPlaying && musicSourceB != null) musicSourceB.volume = vol;
    }

    private void UpdateAllSourceVolumes()
    {
        UpdateMusicVolume();
        if (ambienceSource != null) ambienceSource.volume = GetEffectiveAmbienceVolume();
        if (uiSource != null) uiSource.volume = GetEffectiveUIVolume();
        foreach (var s in sfxSourcePool)
        {
            if (s != null) s.volume = GetEffectiveSFXVolume();
        }
    }

    private void LoadSavedVolumePreferences()
    {
        if (PlayerPrefs.HasKey("MasterVolume")) masterVolume = PlayerPrefs.GetFloat("MasterVolume");
        if (PlayerPrefs.HasKey("MusicVolume")) musicVolume = PlayerPrefs.GetFloat("MusicVolume");
        if (PlayerPrefs.HasKey("SFXVolume")) sfxVolume = PlayerPrefs.GetFloat("SFXVolume");
        if (PlayerPrefs.HasKey("AmbienceVolume")) ambienceVolume = PlayerPrefs.GetFloat("AmbienceVolume");
        if (PlayerPrefs.HasKey("IsMuted")) isMuted = PlayerPrefs.GetInt("IsMuted") == 1;

        AudioListener.volume = isMuted ? 0f : 1f;
    }

    #endregion
}
