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

    [Header("Attack Timing")]
    public float slashHitDelay = 0.5f;
    public float stabHitDelay = 0.4f;

    [Header("Attack Hit Zones")]
    public float stabHitRange = 3.5f;
    public float stabHitAngle = 30f;
    public float slashHitRange = 2.5f;
    public float slashHitAngle = 120f;

    [Header("Stab Wind-Up (Zurückgehen)")]
    public float stabWindUpDistance = 1.0f;
    public float stabWindUpDuration = 0.3f;

    [Header("Stab Dash")]
    public float stabDashDistance = 2f;
    public float stabDashDuration = 0.3f;
    public float stabDashDelay = 0.3f;

    [Header("Attack Lock")]
    public float attackLockDuration = 1.0f;

    [Header("Animation")]
    public Animator animator;

    [Header("Trigger Names")]
    public string triggerAttack1 = "AttackSlash01";
    public string triggerAttack2 = "AttackSlash02";
    public string triggerAttack3 = "AttackStab";
    public string triggerTakeDamage = "TakeDamage";
    public string triggerDeath = "Death";
    public string triggerSpawn = "Spawn";
    public string boolIsRunning = "IsRunning";

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
    private PlayerController playerController;
    private float lastAttackTime = 0f;
    private bool isDead = false;
    private string[] attackTriggers;
    private int pendingAttackType = -1;

    // Attack Lock
    private bool isAttackLocked = false;
    private float attackLockTimer = 0f;

    // Stab Wind-Up (Zurückgehen)
    private bool isWindingUp = false;
    private Vector3 windUpDirection;
    private float windUpTimer = 0f;
    private float windUpSpeed;

    // Stab Dash
    private bool isStabDashing = false;
    private Vector3 stabDashDirection;
    private float stabDashTimer = 0f;
    private float stabDashSpeed;

    void Start()
    {
        currentHealth = maxHealth;
        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;
        player = FindFirstObjectByType<PlayerHealth>();
        playerController = FindFirstObjectByType<PlayerController>();

        if (enemyRenderer == null)
            enemyRenderer = GetComponentInChildren<Renderer>();

        if (enemyRenderer != null)
            enemyRenderer.material.color = normalColor;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        attackTriggers = new string[] { triggerAttack1, triggerAttack2, triggerAttack3 };

        if (healthBarSlider != null)
        {
            healthBarSlider.minValue = 0f;
            healthBarSlider.maxValue = maxHealth;
            healthBarSlider.value = maxHealth;
        }

        if (hideHealthBarWhenFull && healthBarObject != null)
            healthBarObject.SetActive(false);

        FireTrigger(triggerSpawn);
    }

    void Update()
    {
        if (isDead)
        {
            if (agent.enabled) agent.ResetPath();
            return;
        }

        // Wind-Up (Zurückgehen vor Stich)
        if (isWindingUp)
        {
            UpdateWindUp();
            UpdateHealthBar();
            return;
        }

        // Stab Dash
        if (isStabDashing)
        {
            UpdateStabDash();
            UpdateHealthBar();
            return;
        }

        // Attack Lock
        if (isAttackLocked)
        {
            attackLockTimer -= Time.deltaTime;

            if (agent.enabled)
            {
                agent.ResetPath();
                agent.velocity = Vector3.zero;
            }

            SetRunning(false);

            if (attackLockTimer <= 0f)
                isAttackLocked = false;

            UpdateHealthBar();
            return;
        }

        if (player == null || player.isDead)
        {
            if (agent.enabled) agent.ResetPath();
            SetRunning(false);
            return;
        }

        float distance = Vector3.Distance(transform.position, player.transform.position);

        if (distance <= detectionRange)
        {
            if (distance > attackRange)
            {
                if (agent.enabled)
                    agent.SetDestination(player.transform.position);

                SetRunning(true);
            }
            else
            {
                if (agent.enabled)
                {
                    agent.ResetPath();
                    agent.velocity = Vector3.zero;
                }

                SetRunning(false);

                if (Time.time >= lastAttackTime + attackCooldown)
                    Attack();
            }

            if (!isAttackLocked && !isStabDashing && !isWindingUp)
                LookAtPlayer();
        }
        else
        {
            if (agent.enabled) agent.ResetPath();
            SetRunning(false);
        }

        UpdateHealthBar();
    }

    void UpdateWindUp()
    {
        windUpTimer -= Time.deltaTime;

        if (windUpTimer <= 0f)
        {
            isWindingUp = false;
            return;
        }

        if (agent.enabled)
        {
            agent.Move(windUpDirection * windUpSpeed * Time.deltaTime);
        }
    }

    void StartWindUp()
    {
        if (isDead) return;
        if (!agent.enabled) return;

        // Richtung: weg vom Spieler (zurück)
        Vector3 awayFromPlayer = transform.position - player.transform.position;
        awayFromPlayer.y = 0f;
        windUpDirection = awayFromPlayer.normalized;

        windUpSpeed = stabWindUpDistance / stabWindUpDuration;
        windUpTimer = stabWindUpDuration;
        isWindingUp = true;

        agent.ResetPath();
        agent.velocity = Vector3.zero;
    }

    void UpdateStabDash()
    {
        stabDashTimer -= Time.deltaTime;

        if (stabDashTimer <= 0f)
        {
            isStabDashing = false;
            return;
        }

        if (agent.enabled)
        {
            agent.Move(stabDashDirection * stabDashSpeed * Time.deltaTime);
        }
    }

    void StartStabDash()
    {
        if (isDead) return;
        if (!agent.enabled) return;
        if (player == null) return;

        // Richtung: zum Spieler (nach vorne, eingefroren)
        Vector3 toPlayer = player.transform.position - transform.position;
        toPlayer.y = 0f;
        stabDashDirection = toPlayer.normalized;

        stabDashSpeed = stabDashDistance / stabDashDuration;
        stabDashTimer = stabDashDuration;
        isStabDashing = true;

        agent.ResetPath();
        agent.velocity = Vector3.zero;
    }

    void LookAtPlayer()
    {
        if (player == null) return;

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
        if (player == null || player.isDead) return;

        lastAttackTime = Time.time;

        pendingAttackType = Random.Range(0, 3);

        FireTrigger(attackTriggers[pendingAttackType]);

        isAttackLocked = true;
        attackLockTimer = attackLockDuration;

        if (agent.enabled)
        {
            agent.ResetPath();
            agent.velocity = Vector3.zero;
        }

        SetRunning(false);

        if (pendingAttackType == 2)
        {
            // Stab: Zurückgehen → Dash → Hit
            StartWindUp();
            Invoke(nameof(StartStabDash), stabWindUpDuration + stabDashDelay);
            Invoke(nameof(DelayedHit), stabWindUpDuration + stabDashDelay + stabHitDelay);
        }
        else
        {
            // Slash: Stehen bleiben → Hit
            Invoke(nameof(DelayedHit), slashHitDelay);
        }

        if (attackSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(attackSound);

        if (enemyRenderer != null)
        {
            enemyRenderer.material.color = attackColor;
            CancelInvoke(nameof(ResetColor));
            Invoke(nameof(ResetColor), 0.2f);
        }
    }

    void DelayedHit()
    {
        if (isDead) return;
        if (player == null || player.isDead) return;

        if (playerController != null && playerController.IsInvincible) return;

        Vector3 toPlayer = player.transform.position - transform.position;
        float distance = toPlayer.magnitude;
        toPlayer.y = 0f;
        float angle = Vector3.Angle(transform.forward, toPlayer.normalized);

        bool hit = false;

        if (pendingAttackType == 2)
        {
            if (distance <= stabHitRange && angle <= stabHitAngle * 0.5f)
                hit = true;
        }
        else
        {
            if (distance <= slashHitRange && angle <= slashHitAngle * 0.5f)
                hit = true;
        }

        if (hit)
        {
            player.TakeDamage(attackDamage);
        }

        pendingAttackType = -1;
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0f);

        isStabDashing = false;
        isWindingUp = false;
        isAttackLocked = false;
        CancelInvoke(nameof(StartStabDash));
        CancelInvoke(nameof(StartWindUp));
        CancelInvoke(nameof(DelayedHit));

        FireTrigger(triggerTakeDamage);

        if (hitSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(hitSound);

        if (enemyRenderer != null)
        {
            enemyRenderer.material.color = hitColor;
            CancelInvoke(nameof(ResetColor));
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

        isStabDashing = false;
        isWindingUp = false;
        isAttackLocked = false;
        CancelInvoke(nameof(DelayedHit));
        CancelInvoke(nameof(StartStabDash));
        CancelInvoke(nameof(StartWindUp));

        FireTrigger(triggerDeath);

        if (deathSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(deathSound);

        if (agent.enabled)
        {
            agent.ResetPath();
            agent.velocity = Vector3.zero;
            agent.enabled = false;
        }

        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        if (healthBarObject != null)
            healthBarObject.SetActive(false);

        Destroy(gameObject, 3f);
    }

    void FireTrigger(string triggerName)
    {
        if (animator == null) return;

        foreach (string t in attackTriggers)
            animator.ResetTrigger(t);
        animator.ResetTrigger(triggerTakeDamage);
        animator.ResetTrigger(triggerDeath);
        animator.ResetTrigger(triggerSpawn);

        animator.SetTrigger(triggerName);
    }

    void SetRunning(bool running)
    {
        if (animator == null) return;
        animator.SetBool(boolIsRunning, running);
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

        Gizmos.color = Color.cyan;
        Vector3 stabLeft = Quaternion.Euler(0, -stabHitAngle * 0.5f, 0) * transform.forward * stabHitRange;
        Vector3 stabRight = Quaternion.Euler(0, stabHitAngle * 0.5f, 0) * transform.forward * stabHitRange;
        Gizmos.DrawLine(transform.position, transform.position + stabLeft);
        Gizmos.DrawLine(transform.position, transform.position + stabRight);

        Gizmos.color = Color.magenta;
        Vector3 slashLeft = Quaternion.Euler(0, -slashHitAngle * 0.5f, 0) * transform.forward * slashHitRange;
        Vector3 slashRight = Quaternion.Euler(0, slashHitAngle * 0.5f, 0) * transform.forward * slashHitRange;
        Gizmos.DrawLine(transform.position, transform.position + slashLeft);
        Gizmos.DrawLine(transform.position, transform.position + slashRight);
    }
}