using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    [System.Serializable]
    public class ButtonSoundEntry
    {
        public Button button;
        public AudioClip clickSound;
        public AudioClip hoverSound;
    }

    [Header("Musik Tracks")]
    public List<AudioClip> musicTracks;

    [Header("Button Sounds")]
    public List<ButtonSoundEntry> buttonSounds;

    [Header("Slider Sounds")]
    public AudioClip musicSliderSound;
    public AudioClip sfxSliderSound;
    public float sliderSoundCooldown = 0.08f;

    [Header("UI Slider")]
    public Slider musicSlider;
    public Slider sfxSlider;

    [Header("UI Wert Texte")]
    public TMP_Text musicValueText;
    public TMP_Text sfxValueText;

    private AudioSource musicSource;
    private AudioSource sfxSource;
    private float musicSliderCooldown = 0f;
    private float sfxSliderCooldown = 0f;

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

        musicSlider.value = savedMusic;
        sfxSlider.value = savedSfx;

        musicSource.volume = savedMusic;
        sfxSource.volume = savedSfx;

        UpdateMusicText(savedMusic);
        UpdateSFXText(savedSfx);

        if (musicTracks.Count > 0)
        {
            musicSource.clip = musicTracks[0];
            musicSource.loop = true;
            musicSource.Play();
        }

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

        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        if (musicSliderCooldown > 0f) musicSliderCooldown -= Time.deltaTime;
        if (sfxSliderCooldown > 0f) sfxSliderCooldown -= Time.deltaTime;
    }

    void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip);
    }

    public void OnMusicSliderChanged()
    {
        float value = musicSlider.value;
        musicSource.volume = value;
        PlayerPrefs.SetFloat("MusicVolume", value);
        UpdateMusicText(value);

        if (musicSliderSound != null && musicSliderCooldown <= 0f)
        {
            sfxSource.PlayOneShot(musicSliderSound, value);
            musicSliderCooldown = sliderSoundCooldown;
        }
    }

    public void OnSFXSliderChanged()
    {
        float value = sfxSlider.value;
        sfxSource.volume = value;
        PlayerPrefs.SetFloat("SFXVolume", value);
        UpdateSFXText(value);

        if (sfxSliderSound != null && sfxSliderCooldown <= 0f)
        {
            sfxSource.PlayOneShot(sfxSliderSound, value);
            sfxSliderCooldown = sliderSoundCooldown;
        }
    }

    void UpdateMusicText(float value)
    {
        musicValueText.text = Mathf.RoundToInt(value * 100f) + "%";
    }

    void UpdateSFXText(float value)
    {
        sfxValueText.text = Mathf.RoundToInt(value * 100f) + "%";
    }
}