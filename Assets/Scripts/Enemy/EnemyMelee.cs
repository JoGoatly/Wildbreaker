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

    [Header("Animation")]
    public Animator animator;

    // NEU: Hier die exakten Namen der Animation-Clips/States eintragen
    [Header("Animation State Names")]
    public string idleStateName = "Idle";
    public string walkStateName = "Walk Forward";
    public string runStateName = "Run Forward";
    public string attackSlash01StateName = "Attack Slash 01";
    public string attackSlash02StateName = "Attack Slash 02";
    public string attackStabStateName = "Attack Stab";
    public string takeDamageStateName = "Take Damage";
    public string deathStateName = "Death";
    public string spawnStateName = "Spawn";

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
    private float lastAttackTime = -99f;
    private bool isDead = false;
    private bool isLocked = false; // NEU: Sperrt Bewegung während Animation
    private string[] attackStates; // NEU: Array für zufällige Angriffe

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

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        // NEU: Angriffsanimationen in Array
        attackStates = new string[] { attackSlash01StateName, attackSlash02StateName, attackStabStateName };

        if (healthBarSlider != null)
        {
            healthBarSlider.minValue = 0f;
            healthBarSlider.maxValue = maxHealth;
            healthBarSlider.value = maxHealth;
        }

        if (hideHealthBarWhenFull && healthBarObject != null)
            healthBarObject.SetActive(false);

        // Spawn-Animation direkt abspielen
        if (animator != null)
        {
            animator.Play(spawnStateName, 0, 0f);
            isLocked = true;
        }
    }

    void Update()
    {
        if (isDead)
        {
            agent.ResetPath();
            return;
        }

        // NEU: Prüfen ob die aktuelle Animation fertig ist
        if (isLocked && animator != null)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

            // Ist die Animation fertig? (normalizedTime >= 1 = Animation ist durchgelaufen)
            // UND ist der Animator nicht gerade in einer Transition?
            if (!animator.IsInTransition(0) && stateInfo.normalizedTime >= 0.95f)
            {
                isLocked = false;
            }
            else
            {
                // Während Lock: Stehen bleiben, zum Spieler schauen
                agent.ResetPath();
                if (player != null && !player.isDead)
                    LookAtPlayer();
                UpdateHealthBar();
                return;
            }
        }

        if (player == null || player.isDead)
        {
            agent.ResetPath();
            PlayLocomotion(0f);
            return;
        }

        float distance = Vector3.Distance(transform.position, player.transform.position);

        if (distance <= detectionRange)
        {
            if (distance > attackRange)
            {
                agent.SetDestination(player.transform.position);
                PlayLocomotion(agent.velocity.magnitude);
            }
            else
            {
                agent.ResetPath();
                PlayLocomotion(0f);

                if (Time.time >= lastAttackTime + attackCooldown)
                    Attack();
            }

            LookAtPlayer();
        }
        else
        {
            agent.ResetPath();
            PlayLocomotion(0f);
        }

        UpdateHealthBar();
    }

    // NEU: Spielt Idle, Walk oder Run basierend auf Geschwindigkeit
    void PlayLocomotion(float speed)
    {
        if (animator == null || isLocked) return;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        if (speed < 0.1f)
        {
            if (!stateInfo.IsName(idleStateName))
                animator.CrossFade(idleStateName, 0.15f);
        }
        else if (speed < 3f)
        {
            if (!stateInfo.IsName(walkStateName))
                animator.CrossFade(walkStateName, 0.15f);
        }
        else
        {
            if (!stateInfo.IsName(runStateName))
                animator.CrossFade(runStateName, 0.15f);
        }
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
        if (player.isDead) return;

        lastAttackTime = Time.time;

        // NEU: Zufällige Angriffsanimation direkt abspielen
        if (animator != null)
        {
            string attackState = attackStates[Random.Range(0, attackStates.Length)];
            animator.Play(attackState, 0, 0f);
            isLocked = true;
        }

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

        // NEU: TakeDamage direkt abspielen
        if (animator != null)
        {
            animator.Play(takeDamageStateName, 0, 0f);
            isLocked = true;
        }

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

        // NEU: Death direkt abspielen
        if (animator != null)
            animator.Play(deathStateName, 0, 0f);

        if (deathSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(deathSound);

        agent.ResetPath();
        agent.enabled = false;

        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        if (healthBarObject != null)
            healthBarObject.SetActive(false);

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
    }
}