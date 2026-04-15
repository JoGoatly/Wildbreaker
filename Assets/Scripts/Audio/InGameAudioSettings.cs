using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InGameAudioSettings : MonoBehaviour
{
    [Header("InGame UI Slider")]
    public Slider musicSlider;
    public Slider sfxSlider;

    [Header("InGame Wert Texte")]
    public TMP_Text musicValueText;
    public TMP_Text sfxValueText;

    void Start()
    {
        musicSlider.minValue = 0f;
        musicSlider.maxValue = 1f;
        sfxSlider.minValue = 0f;
        sfxSlider.maxValue = 1f;
    }

    void OnEnable()
    {
        float music = PlayerPrefs.GetFloat("MusicVolume", 1f);
        float sfx = PlayerPrefs.GetFloat("SFXVolume", 1f);

        musicSlider.SetValueWithoutNotify(music);
        sfxSlider.SetValueWithoutNotify(sfx);

        UpdateMusicText(music);
        UpdateSFXText(sfx);

        musicSlider.onValueChanged.AddListener(OnMusicChanged);
        sfxSlider.onValueChanged.AddListener(OnSFXChanged);
    }

    void OnDisable()
    {
        musicSlider.onValueChanged.RemoveListener(OnMusicChanged);
        sfxSlider.onValueChanged.RemoveListener(OnSFXChanged);
    }

    void OnMusicChanged(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMusicVolume(value);

        UpdateMusicText(value);
    }

    void OnSFXChanged(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetSFXVolume(value);

        UpdateSFXText(value);
    }

    void UpdateMusicText(float value)
    {
        if (musicValueText != null)
            musicValueText.text = Mathf.RoundToInt(value * 100f) + "%";
    }

    void UpdateSFXText(float value)
    {
        if (sfxValueText != null)
            sfxValueText.text = Mathf.RoundToInt(value * 100f) + "%";
    }
}