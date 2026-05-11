using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyRanged : MonoBehaviour
{
    [Header("Stats")]
    public float maxHealth = 40f;
    public float detectionRange = 25f;
    public float attackRange = 15f;
    public float minDistance = 10f;
    public float moveSpeed = 3f;
    public float fleeSpeed = 6f; // NEU: Geschwindigkeit beim Weglaufen
    public float rotationSpeed = 5f;

    [Header("Projectile")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float projectileSpeed = 15f;
    public float projectileDamage = 10f;
    public float fireRate = 2f;
    public float projectileLifetime = 5f;
    public float aimHeightOffset = 1.0f; // NEU: Zielhöhe einstellbar (vorher fest auf 1.2f)

    // NEU: Streuung für Ungenauigkeit
    public float horizontalSpread = 1.5f;
    public float verticalSpread = 0.5f;

    [Header("Sound")]
    public AudioClip shootSound;
    public AudioClip hitSound;
    public AudioClip deathSound;
    public AudioClip detectSound;

    [Header("Visual Feedback")]
    public Renderer enemyRenderer;
    public Color normalColor = Color.blue;
    public Color attackColor = Color.cyan;
    public Color hitColor = Color.white;

    [Header("Health Bar")]
    public Slider healthBarSlider;
    public GameObject healthBarObject;
    public bool hideHealthBarWhenFull = true;

    private float currentHealth;
    private NavMeshAgent agent;
    private PlayerHealth player;
    private float lastFireTime = 0f;
    private bool isDead = false;
    private bool playerDetected = false;

    void Start()
    {
        currentHealth = maxHealth;
        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;
        player = FindFirstObjectByType<PlayerHealth>();

        if (enemyRenderer == null)
            enemyRenderer = GetComponentInChildren<Renderer>();

        if (enemyRenderer != null)
            enemyRenderer.material.color = normalColor;

        if (healthBarSlider != null)
        {
            healthBarSlider.minValue = 0f;
            healthBarSlider.maxValue = maxHealth;
            healthBarSlider.value = maxHealth;
        }

        if (hideHealthBarWhenFull && healthBarObject != null)
            healthBarObject.SetActive(false);

        // FirePoint automatisch erstellen wenn keiner zugewiesen
        if (firePoint == null)
        {
            GameObject fp = new GameObject("FirePoint");
            fp.transform.SetParent(transform);
            fp.transform.localPosition = new Vector3(0f, 1.5f, 1f);
            firePoint = fp.transform;
        }
    }

    void Update()
    {
        if (isDead)
        {
            agent.ResetPath();
            return;
        }

        if (player == null || player.isDead)
        {
            agent.ResetPath();
            return;
        }

        float distance = Vector3.Distance(transform.position, player.transform.position);

        if (distance <= detectionRange)
        {
            // Erster Sichtkontakt
            if (!playerDetected)
            {
                playerDetected = true;
                if (detectSound != null && AudioManager.Instance != null)
                    AudioManager.Instance.PlaySFX(detectSound);
            }

            // Zu nah → weglaufen
            if (distance < minDistance)
            {
                agent.speed = fleeSpeed; // NEU: Fluchtgeschwindigkeit setzen
                Vector3 fleeDir = (transform.position - player.transform.position).normalized;
                Vector3 fleePos = transform.position + fleeDir * 5f;
                agent.SetDestination(fleePos);
            }
            // Zu weit → näherkommen
            else if (distance > attackRange)
            {
                agent.speed = moveSpeed; // NEU: Wieder auf normale Geschwindigkeit setzen
                agent.SetDestination(player.transform.position);
            }
            // In Schussreichweite → stehen bleiben und schießen
            else
            {
                agent.ResetPath();

                if (Time.time >= lastFireTime + (1f / fireRate))
                {
                    Shoot();
                    lastFireTime = Time.time;
                }
            }

            LookAtPlayer();
        }
        else
        {
            agent.ResetPath();
            playerDetected = false;
        }

        UpdateHealthBar();
    }

    void LookAtPlayer()
    {
        Vector3 dir = player.transform.position - transform.position;
        dir.y = 0f;

        if (dir.magnitude > 0.1f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                Time.deltaTime * rotationSpeed
            );
        }
    }

    void Shoot()
    {
        if (player.isDead) return;
        if (projectilePrefab == null) return;

        // GEÄNDERT: Perfekte Zielposition berechnen
        Vector3 targetPos = player.transform.position + Vector3.up * aimHeightOffset;

        // GEÄNDERT: Zufälligen Offset generieren (Ungenauigkeit)
        float randomX = Random.Range(-horizontalSpread, horizontalSpread);
        float randomY = Random.Range(-verticalSpread, verticalSpread);

        // Offset relativ zur Blickrichtung des Gegners anwenden
        targetPos += transform.right * randomX + transform.up * randomY;

        // Finale Richtung berechnen
        Vector3 direction = (targetPos - firePoint.position).normalized;

        // Projektil erstellen
        GameObject projectile = Instantiate(
            projectilePrefab,
            firePoint.position,
            Quaternion.LookRotation(direction)
        );

        // Projektil Script hinzufügen
        EnemyProjectile proj = projectile.GetComponent<EnemyProjectile>();
        if (proj == null)
            proj = projectile.AddComponent<EnemyProjectile>();

        proj.Init(direction, projectileSpeed, projectileDamage, projectileLifetime);

        // Sound
        if (shootSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(shootSound);

        // Farbe kurz ändern
        if (enemyRenderer != null)
        {
            enemyRenderer.material.color = attackColor;
            Invoke(nameof(ResetColor), 0.2f);
        }
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0f);

        if (hitSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(hitSound);

        if (enemyRenderer != null)
        {
            enemyRenderer.material.color = hitColor;
            Invoke(nameof(ResetColor), 0.1f);
        }

        // HealthBar updaten
        if (healthBarSlider != null)
            healthBarSlider.value = currentHealth;

        if (hideHealthBarWhenFull && healthBarObject != null)
            healthBarObject.SetActive(currentHealth < maxHealth);

        if (currentHealth <= 0f)
            Die();
    }

    void Die()
    {
        isDead = true;

        if (deathSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(deathSound);

        agent.ResetPath();
        agent.enabled = false;

        if (healthBarObject != null)
            healthBarObject.SetActive(false);

        transform.Rotate(Vector3.right, 90f);

        Destroy(gameObject, 3f);
    }

    void ResetColor()
    {
        if (enemyRenderer != null && !isDead)
            enemyRenderer.material.color = normalColor;
    }

    void UpdateHealthBar()
    {
        if (healthBarObject == null) return;
        if (!healthBarObject.activeSelf) return;

        Camera cam = Camera.main;
        if (cam != null)
        {
            healthBarObject.transform.LookAt(
                healthBarObject.transform.position + cam.transform.forward
            );
        }
    }

    void OnDrawGizmosSelected()
    {
        // Gelb = Erkennung
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Rot = Angriffsreichweite
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Grün = Minimaldistanz
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, minDistance);
    }
}