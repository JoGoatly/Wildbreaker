using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;
    public bool isDead { get; private set; }

    [Header("UI")]
    public Slider healthBarSlider;
    public TMP_Text healthText;
    public GameObject gameOverPanel;

    [Header("Damage Feedback")]
    public AudioClip hurtSound;
    public float hurtShakeDuration = 0.15f;
    public float hurtShakeMagnitude = 0.05f;
    public Image damageFlash;
    public float flashDuration = 0.3f;

    [Header("Downed State")]
    public float downedMoveSpeed = 1f;

    private float flashTimer = 0f;
    private PlayerController playerController;
    private float originalMoveSpeed;
    private float originalSprintSpeed;

    void Start()
    {
        currentHealth = maxHealth;
        playerController = GetComponent<PlayerController>();

        if (playerController != null)
        {
            originalMoveSpeed = playerController.moveSpeed;
            originalSprintSpeed = playerController.sprintSpeed;
        }

        if (healthBarSlider != null)
        {
            healthBarSlider.minValue = 0f;
            healthBarSlider.maxValue = maxHealth;
            healthBarSlider.value = maxHealth;
        }

        UpdateUI();

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (damageFlash != null)
            damageFlash.gameObject.SetActive(false);
    }

    void Update()
    {
        if (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
            if (damageFlash != null)
            {
                float alpha = Mathf.Lerp(0f, 0.3f, flashTimer / flashDuration);
                damageFlash.color = new Color(1f, 0f, 0f, alpha);

                if (flashTimer <= 0f)
                    damageFlash.gameObject.SetActive(false);
            }
        }
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0f);
        UpdateUI();

        if (hurtSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(hurtSound);

        if (CameraShaker.Instance != null)
            CameraShaker.Instance.ShakeHit(hurtShakeDuration, hurtShakeMagnitude);

        if (damageFlash != null)
        {
            damageFlash.gameObject.SetActive(true);
            damageFlash.color = new Color(1f, 0f, 0f, 0.3f);
            flashTimer = flashDuration;
        }

        if (currentHealth <= 0f)
            Die();
    }

    void Die()
    {
        isDead = true;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        // Langsames Krabbeln
        if (playerController != null)
        {
            playerController.moveSpeed = downedMoveSpeed;
            playerController.sprintSpeed = downedMoveSpeed;
        }

        // Maus aktivieren
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Respawn()
    {
        isDead = false;

        if (CheckpointManager.Instance != null)
            CheckpointManager.Instance.RespawnPlayer();

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        // Normale Geschwindigkeit wiederherstellen
        if (playerController != null)
        {
            playerController.moveSpeed = originalMoveSpeed;
            playerController.sprintSpeed = originalSprintSpeed;
        }

        // Maus wieder sperren
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        UpdateUI();
    }

    public void Revive()
    {
        isDead = false;
        currentHealth = maxHealth;
        UpdateUI();

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        // Normale Geschwindigkeit wiederherstellen
        if (playerController != null)
        {
            playerController.moveSpeed = originalMoveSpeed;
            playerController.sprintSpeed = originalSprintSpeed;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void RestoreFullHealth()
    {
        currentHealth = maxHealth;
        isDead = false;
        UpdateUI();

        // Normale Geschwindigkeit wiederherstellen
        if (playerController != null)
        {
            playerController.moveSpeed = originalMoveSpeed;
            playerController.sprintSpeed = originalSprintSpeed;
        }
    }

    void UpdateUI()
    {
        if (healthBarSlider != null)
            healthBarSlider.value = currentHealth;

        if (healthText != null)
            healthText.text = Mathf.CeilToInt(currentHealth) + " / " + Mathf.CeilToInt(maxHealth);
    }
}