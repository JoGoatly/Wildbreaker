using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [System.Serializable]
    public class ButtonSoundEntry
    {
        public Button button;
        public AudioClip clickSound;
        public AudioClip hoverSound;
    }

    [Header("Musik Tracks")]
    // Everything in here is treated as Music
    public List<AudioClip> musicTracks;

    [Header("Button Sounds")]
    public List<ButtonSoundEntry> buttonSounds;

    [Header("Slider Sounds")]
    public AudioClip musicSliderSound;
    public AudioClip sfxSliderSound;
    public float sliderSoundCooldown = 0.08f;

    private AudioSource musicSource;
    private AudioSource sfxSource;
    private float musicSliderCooldown = 0f;
    private float sfxSliderCooldown = 0f;

    // All AudioSources in the scene that are NOT music
    private List<AudioSource> sfxSources = new List<AudioSource>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshSFXSources();
    }

    void Start()
    {
        AudioSource[] sources = GetComponents<AudioSource>();

        if (sources.Length < 2)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            sfxSource = gameObject.AddComponent<AudioSource>();
        }
        else
        {
            musicSource = sources[0];
            sfxSource = sources[1];
        }

        sfxSource.playOnAwake = false;
        sfxSource.loop = false;

        float savedMusic = PlayerPrefs.GetFloat("MusicVolume", 1f);
        float savedSfx = PlayerPrefs.GetFloat("SFXVolume", 1f);

        musicSource.volume = savedMusic;
        sfxSource.volume = savedSfx;

        if (musicTracks.Count > 0)
        {
            musicSource.clip = musicTracks[0];
            musicSource.loop = true;
            musicSource.Play();
        }

        SetupButtonSounds();
        RefreshSFXSources();
    }

    void Update()
    {
        if (musicSliderCooldown > 0f) musicSliderCooldown -= Time.deltaTime;
        if (sfxSliderCooldown > 0f) sfxSliderCooldown -= Time.deltaTime;
    }

    // Finds every AudioSource in the scene and registers it as SFX
    // unless its clip is a music track
    public void RefreshSFXSources()
    {
        sfxSources.Clear();

        AudioSource[] allSources = FindObjectsOfType<AudioSource>();
        float savedSfx = PlayerPrefs.GetFloat("SFXVolume", 1f);

        foreach (var source in allSources)
        {
            // Skip our own AudioSources on this GameObject
            if (source.gameObject == gameObject) continue;

            // Skip if its clip is a music track
            if (source.clip != null && musicTracks.Contains(source.clip)) continue;

            // Register as SFX and apply current volume
            source.volume = savedSfx;
            sfxSources.Add(source);
        }
    }

    // Call this when a new object with an AudioSource is spawned at runtime
    public void RegisterSFXSource(AudioSource source)
    {
        if (source == null) return;
        if (source.gameObject == gameObject) return;
        if (source.clip != null && musicTracks.Contains(source.clip)) return;
        if (sfxSources.Contains(source)) return;

        float savedSfx = PlayerPrefs.GetFloat("SFXVolume", 1f);
        source.volume = savedSfx;
        sfxSources.Add(source);
    }

    void SetupButtonSounds()
    {
        foreach (var entry in buttonSounds)
        {
            if (entry.button == null) continue;

            var clickSound = entry.clickSound;
            var hoverSound = entry.hoverSound;

            entry.button.onClick.AddListener(() => PlaySFX(clickSound));

            EventTrigger trigger = entry.button.gameObject.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = entry.button.gameObject.AddComponent<EventTrigger>();

            EventTrigger.Entry hoverEntry = new EventTrigger.Entry();
            hoverEntry.eventID = EventTriggerType.PointerEnter;
            hoverEntry.callback.AddListener((_) => PlaySFX(hoverSound));
            trigger.triggers.Add(hoverEntry);
        }
    }

    public float GetMusicVolume() => PlayerPrefs.GetFloat("MusicVolume", 1f);
    public float GetSFXVolume() => PlayerPrefs.GetFloat("SFXVolume", 1f);

    public void SetMusicVolume(float value)
    {
        value = Mathf.Clamp01(value);
        if (musicSource != null)
            musicSource.volume = value;
        PlayerPrefs.SetFloat("MusicVolume", value);

        if (musicSliderSound != null && musicSliderCooldown <= 0f)
        {
            sfxSource.PlayOneShot(musicSliderSound);
            musicSliderCooldown = sliderSoundCooldown;
        }
    }

    public void SetSFXVolume(float value)
    {
        value = Mathf.Clamp01(value);
        if (sfxSource != null)
            sfxSource.volume = value;
        PlayerPrefs.SetFloat("SFXVolume", value);

        // Apply to ALL registered SFX sources in the scene
        foreach (var source in sfxSources)
        {
            if (source != null)
                source.volume = value;
        }

        if (sfxSliderSound != null && sfxSliderCooldown <= 0f)
        {
            sfxSource.PlayOneShot(sfxSliderSound);
            sfxSliderCooldown = sliderSoundCooldown;
        }
    }

    // Used by Destructible and anything else that needs to play a one-shot SFX
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip);
    }
    // Exposes the sfxSource so ItemCollisionReporter can use it directly
    public AudioSource GetSFXSource()
    {
        return sfxSource;
    }
}