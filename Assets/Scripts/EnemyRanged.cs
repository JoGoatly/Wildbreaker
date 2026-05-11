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
    public float fleeSpeed = 6f;
    public float rotationSpeed = 5f;

    [Header("Projectile")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float projectileSpeed = 25f; // GEÄNDERT: höher für flachere Parabel
    public float projectileDamage = 10f;
    public float fireRate = 2f;
    public float projectileLifetime = 5f;
    public float aimHeightOffset = 1.0f;

    // GEÄNDERT: Viel kleinere Streuung
    [Header("Accuracy")]
    [Range(0f, 1f)] public float nearMissChance = 0.30f;  // 30% knapp daneben
    public float hitSpreadHorizontal = 0.08f;              // Normal: fast perfekt
    public float hitSpreadVertical = 0.04f;
    public float nearMissMin = 0.3f;                       // Daneben: mindestens so weit
    public float nearMissMax = 0.6f;                       // Daneben: höchstens so weit

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
            if (!playerDetected)
            {
                playerDetected = true;
                if (detectSound != null && AudioManager.Instance != null)
                    AudioManager.Instance.PlaySFX(detectSound);
            }

            if (distance < minDistance)
            {
                agent.speed = fleeSpeed;
                Vector3 fleeDir = (transform.position - player.transform.position).normalized;
                Vector3 fleePos = transform.position + fleeDir * 5f;
                agent.SetDestination(fleePos);
            }
            else if (distance > attackRange)
            {
                agent.speed = moveSpeed;
                agent.SetDestination(player.transform.position);
            }
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

    // GEÄNDERT: Ballistisch korrekte Schussberechnung
    void Shoot()
    {
        if (player.isDead) return;
        if (projectilePrefab == null) return;

        // Zielposition mit Offset
        Vector3 targetPos = player.transform.position + Vector3.up * aimHeightOffset;

        // Streuung anwenden
        float offsetX;
        float offsetY;

        if (Random.value < nearMissChance)
        {
            // Knapp daneben
            float side = Random.value < 0.5f ? -1f : 1f;
            offsetX = side * Random.Range(nearMissMin, nearMissMax);
            offsetY = Random.Range(-0.1f, 0.1f);
        }
        else
        {
            // Fast perfekt
            offsetX = Random.Range(-hitSpreadHorizontal, hitSpreadHorizontal);
            offsetY = Random.Range(-hitSpreadVertical, hitSpreadVertical);
        }

        targetPos += transform.right * offsetX + Vector3.up * offsetY;

        // Ballistisch korrekte Abschussrichtung berechnen
        Vector3 launchVelocity;
        if (!TryGetBallisticVelocity(firePoint.position, targetPos, projectileSpeed, out launchVelocity))
        {
            // Fallback: einfach direkt drauf zielen (sollte selten vorkommen)
            launchVelocity = (targetPos - firePoint.position).normalized * projectileSpeed;
        }

        Vector3 direction = launchVelocity.normalized;

        // Projektil erstellen
        GameObject projectile = Instantiate(
            projectilePrefab,
            firePoint.position,
            Quaternion.LookRotation(direction)
        );

        EnemyProjectile proj = projectile.GetComponent<EnemyProjectile>();
        if (proj == null)
            proj = projectile.AddComponent<EnemyProjectile>();

        // GEÄNDERT: Übergibt die volle berechnete Velocity statt direction * speed
        proj.Init(direction, launchVelocity.magnitude, projectileDamage, projectileLifetime);

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

    // NEU: Berechnet die Abschussgeschwindigkeit für eine ballistische Parabel
    bool TryGetBallisticVelocity(Vector3 start, Vector3 target, float speed, out Vector3 velocity)
    {
        Vector3 toTarget = target - start;
        Vector3 toTargetXZ = new Vector3(toTarget.x, 0f, toTarget.z);

        float x = toTargetXZ.magnitude;
        float y = toTarget.y;
        float g = Mathf.Abs(Physics.gravity.y);

        float speedSq = speed * speed;
        float discriminant = speedSq * speedSq - g * (g * x * x + 2f * y * speedSq);

        if (discriminant < 0f)
        {
            velocity = Vector3.zero;
            return false;
        }

        float sqrtDisc = Mathf.Sqrt(discriminant);

        // Flachere Flugbahn wählen (minus) für direkteren Schuss
        float angle = Mathf.Atan2(speedSq - sqrtDisc, g * x);

        Vector3 dirXZ = toTargetXZ.normalized;
        velocity = dirXZ * speed * Mathf.Cos(angle) + Vector3.up * speed * Mathf.Sin(angle);
        return true;
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
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, minDistance);
    }
}