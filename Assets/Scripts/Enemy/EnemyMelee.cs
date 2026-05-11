using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyMelee : MonoBehaviour
{
    [Header("Stats")]
    public float maxHealth = 50f;
    public float attackDamage = 15f;
    public float attackRange = 2.5f;
    public float attackCooldown = 2f;
    public float detectionRange = 15f;
    public float moveSpeed = 3.5f;
    public float rotationSpeed = 5f;

    [Header("Sound")]
    public AudioClip attackSound;
    public AudioClip hitSound;
    public AudioClip deathSound;

    [Header("Visual Feedback")]
    public Renderer enemyRenderer;
    public Color normalColor = Color.red;
    public Color attackColor = Color.yellow;
    public Color hitColor = Color.white;

    [Header("Health Bar")]
    public Slider healthBarSlider;
    public GameObject healthBarObject;
    public bool hideHealthBarWhenFull = true;

    private float currentHealth;
    private NavMeshAgent agent;
    private PlayerHealth player;
    private float lastAttackTime = 0f;
    private bool isDead = false;

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
            if (distance > attackRange)
            {
                agent.SetDestination(player.transform.position);
            }
            else
            {
                agent.ResetPath();

                if (Time.time >= lastAttackTime + attackCooldown)
                    Attack();
            }

            LookAtPlayer();
        }
        else
        {
            agent.ResetPath();
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

    void Attack()
    {
        if (player.isDead) return;

        lastAttackTime = Time.time;

        player.TakeDamage(attackDamage);

        if (attackSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(attackSound);

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

        // HealthBar dreht sich immer zur Kamera
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
    }
}