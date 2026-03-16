using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class PauseManager : MonoBehaviour
{
    public GameObject pausePanel;
    public GameObject pauseSettingsPanel;
    public Slider sensitivitySlider;
    public TMP_Text sensitivityValueText;
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
        pausePanel.SetActive(true);
        pauseSettingsPanel.SetActive(false);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        playerController.canMove = false;
        sensitivityValueText.text = sensitivitySlider.value.ToString("F1");
    }

    void ResumeGame()
    {
        isPaused = false;
        pausePanel.SetActive(false);
        pauseSettingsPanel.SetActive(false);
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        playerController.canMove = true;
    }

    public void OnResumeButtonClicked()
    {
        ResumeGame();
    }

    public void OnSettingsButtonClicked()
    {
        pausePanel.SetActive(false);
        pauseSettingsPanel.SetActive(true);
    }

    public void OnCloseSettingsClicked()
    {
        pauseSettingsPanel.SetActive(false);
        pausePanel.SetActive(true); // zurück zum PausePanel, NICHT alles schließen
    }

    public void OnMainMenuButtonClicked()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void OnSensitivityChanged()
    {
        playerController.mouseSensitivity = sensitivitySlider.value;
        sensitivityValueText.text = sensitivitySlider.value.ToString("F1");
    }
}