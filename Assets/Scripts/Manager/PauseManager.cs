using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class PauseManager : MonoBehaviour
{
    [Header("Haupt Panels")]
    public GameObject pausePanel;
    public GameObject settingsMenuPanel;

    [Header("Unter-Einstellungen")]
    public GameObject audioSettingsPanel;
    public GameObject keybindingsPanel;

    [Header("Sensitivity (im Keybindings Panel)")]
    public Slider sensitivitySlider;
    public TMP_Text sensitivityValueText;

    [Header("Player")]
    public PlayerController playerController;

    private bool isPaused = false;

    void Update()
    {
        if (Keyboard.current.pKey.wasPressedThisFrame)
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    void PauseGame()
    {
        isPaused = true;
        CloseAllPanels();
        pausePanel.SetActive(true);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        playerController.canMove = false;

        if (sensitivitySlider != null && sensitivityValueText != null)
            sensitivityValueText.text = sensitivitySlider.value.ToString("F1");
    }

    void ResumeGame()
    {
        isPaused = false;
        CloseAllPanels();
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        playerController.canMove = true;
    }

    void CloseAllPanels()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsMenuPanel != null) settingsMenuPanel.SetActive(false);
        if (audioSettingsPanel != null) audioSettingsPanel.SetActive(false);
        if (keybindingsPanel != null) keybindingsPanel.SetActive(false);
    }

    // -------- Pause Panel --------
    public void OnResumeButtonClicked()
    {
        ResumeGame();
    }

    public void OnSettingsButtonClicked()
    {
        pausePanel.SetActive(false);
        settingsMenuPanel.SetActive(true);
    }

    public void OnMainMenuButtonClicked()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    // -------- Settings Menu --------
    public void OnOpenAudioSettings()
    {
        settingsMenuPanel.SetActive(false);
        audioSettingsPanel.SetActive(true);
    }

    public void OnOpenKeybindings()
    {
        settingsMenuPanel.SetActive(false);
        keybindingsPanel.SetActive(true);
    }

    // -------- Zurück Buttons --------
    public void OnBackToPauseMenu()
    {
        settingsMenuPanel.SetActive(false);
        pausePanel.SetActive(true);
    }

    public void OnBackToSettingsMenu()
    {
        audioSettingsPanel.SetActive(false);
        keybindingsPanel.SetActive(false);
        settingsMenuPanel.SetActive(true);
    }

    // -------- Sensitivity --------
    public void OnSensitivityChanged()
    {
        playerController.mouseSensitivity = sensitivitySlider.value;
        sensitivityValueText.text = sensitivitySlider.value.ToString("F1");
    }
}