using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    public void OnRespawnButton()
    {
        PlayerHealth player = FindFirstObjectByType<PlayerHealth>();
        if (player != null)
            player.Respawn();
    }

    public void OnMainMenuButton()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}