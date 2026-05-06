using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombat : MonoBehaviour
{
    [Header("Attack Settings")]
    public float attackRange = 2.5f;
    public float attackDamage = 20f;
    public float attackCooldown = 0.5f;
    public LayerMask enemyLayer;

    [Header("Sound")]
    public AudioClip attackSound;
    public AudioClip hitSound;
    public AudioClip missSound;

    [Header("Camera Shake")]
    public float attackShakeDuration = 0.1f;
    public float attackShakeMagnitude = 0.03f;

    private float lastAttackTime = 0f;
    private Camera playerCamera;
    private PlayerHealth playerHealth;

    void Start()
    {
        playerCamera = GetComponentInChildren<Camera>();
        playerHealth = GetComponent<PlayerHealth>();
    }

    void Update()
    {
        if (playerHealth != null && playerHealth.isDead) return;

        // Nicht angreifen wenn Karte offen ist
        if (MapSystem.Instance != null && MapSystem.Instance.gameObject.activeSelf)
        {
            // Prüfe ob MapPanel aktiv ist
            // Wir checken ob der Spieler sich bewegen kann
        }

        // Nicht angreifen wenn Spieler sich nicht bewegen kann (Karte offen etc.)
        PlayerController controller = GetComponent<PlayerController>();
        if (controller != null && !controller.canMove) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                Attack();
                lastAttackTime = Time.time;
            }
        }
    }

    void Attack()
    {
        // Attack Sound
        if (attackSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(attackSound);

        // Raycast vom Bildschirm-Zentrum
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, attackRange, enemyLayer))
        {
            // Gegner getroffen
            EnemyMelee enemy = hit.collider.GetComponent<EnemyMelee>();
            if (enemy != null)
            {
                enemy.TakeDamage(attackDamage);

                // Hit Sound
                if (hitSound != null && AudioManager.Instance != null)
                    AudioManager.Instance.PlaySFX(hitSound);

                // Camera Shake
                if (CameraShaker.Instance != null)
                    CameraShaker.Instance.ShakeHit(attackShakeDuration, attackShakeMagnitude);
            }

            // Auch Wände zerstören
            Destructible destructible = hit.collider.GetComponent<Destructible>();
            if (destructible != null)
            {
                destructible.TakeDamage(attackDamage);
            }
        }
        else
        {
            // Nichts getroffen
            if (missSound != null && AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(missSound);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();

        if (playerCamera != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * attackRange);
        }
    }
}