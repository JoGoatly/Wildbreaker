using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public GameObject settingsPanel;

    public void OnPlayButtonClicked()
    {
        SceneManager.LoadScene("GameScene");
    }

    public void OnSettingsButtonClicked()
    {
        settingsPanel.SetActive(true);
    }

    public void OnCloseSettingsClicked()
    {
        settingsPanel.SetActive(false);
    }

    public void OnQuitButtonClicked()
    {
        Application.Quit();
        Debug.Log("Spiel beendet"); // Nur im Editor sichtbar
    }
}