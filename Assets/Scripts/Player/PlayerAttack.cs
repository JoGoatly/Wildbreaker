using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerAttack : MonoBehaviour
{
    [Header("Attack Settings (Linksklick)")]
    public float attackRange = 2.5f;
    public float attackDamage = 20f;
    public float attackCooldown = 0.5f;

    [Header("Hit Settings (X-Taste)")]
    public float hitDamage = 34f;
    public float hitRange = 5f;
    public LayerMask ignoreLayers;

    [Header("Sound")]
    public AudioClip attackSound;
    public AudioClip hitSound;
    public AudioClip missSound;

    [Header("Camera Shake")]
    public float attackShakeDuration = 0.1f;
    public float attackShakeMagnitude = 0.03f;

    [Header("Hit Feedback")]
    public Color hitFlashColor = Color.white;
    public float hitFlashDuration = 0.1f;

    private float lastAttackTime = 0f;
    private Camera playerCamera;
    private PlayerHealth playerHealth;
    private Collider[] ownColliders;

    void Start()
    {
        playerCamera = GetComponentInChildren<Camera>();
        playerHealth = GetComponent<PlayerHealth>();
        ownColliders = GetComponentsInChildren<Collider>();
    }

    void Update()
    {
        if (playerHealth != null && playerHealth.isDead) return;

        PlayerController controller = GetComponent<PlayerController>();
        if (controller != null && !controller.canMove) return;

        // Linksklick = Angreifen
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                Attack();
                lastAttackTime = Time.time;
            }
        }

        // X-Taste = Destructibles zerstören
        if (Keyboard.current.xKey.wasPressedThisFrame)
        {
            TryHit();
        }
    }

    void Attack()
    {
        if (attackSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(attackSound);

        Vector3 origin = playerCamera.transform.position;
        Vector3 direction = playerCamera.transform.forward;

        int layerMask = ~ignoreLayers;
        RaycastHit[] hits = Physics.RaycastAll(origin, direction, attackRange, layerMask);

        if (hits.Length == 0)
        {
            PlayMissSound();
            return;
        }

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in hits)
        {
            if (IsOwnCollider(hit.collider))
                continue;

            // Melee Gegner
            EnemyMelee melee = hit.collider.GetComponentInParent<EnemyMelee>();
            if (melee != null)
            {
                melee.TakeDamage(attackDamage);
                FlashEnemy(hit.collider.gameObject);

                if (hitSound != null && AudioManager.Instance != null)
                    AudioManager.Instance.PlaySFX(hitSound);

                if (CameraShaker.Instance != null)
                    CameraShaker.Instance.ShakeHit(attackShakeDuration, attackShakeMagnitude);

                return;
            }

            // Fernkampf Gegner
            EnemyRanged ranged = hit.collider.GetComponentInParent<EnemyRanged>();
            if (ranged != null)
            {
                ranged.TakeDamage(attackDamage);
                FlashEnemy(hit.collider.gameObject);

                if (hitSound != null && AudioManager.Instance != null)
                    AudioManager.Instance.PlaySFX(hitSound);

                if (CameraShaker.Instance != null)
                    CameraShaker.Instance.ShakeHit(attackShakeDuration, attackShakeMagnitude);

                return;
            }

            // Destructible
            Destructible destructible = hit.collider.GetComponentInParent<Destructible>();
            if (destructible != null)
            {
                destructible.TakeDamage(attackDamage);

                if (CameraShaker.Instance != null)
                    CameraShaker.Instance.ShakeHit(attackShakeDuration, attackShakeMagnitude);

                return;
            }
        }

        PlayMissSound();
    }

    bool IsOwnCollider(Collider col)
    {
        foreach (var own in ownColliders)
        {
            if (col == own)
                return true;
        }
        return false;
    }

    void PlayMissSound()
    {
        if (missSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(missSound);
    }

    void FlashEnemy(GameObject enemyObj)
    {
        Renderer renderer = enemyObj.GetComponentInChildren<Renderer>();
        if (renderer != null)
            StartCoroutine(EnemyFlashRoutine(renderer));
    }

    IEnumerator EnemyFlashRoutine(Renderer renderer)
    {
        Color originalColor = renderer.material.color;
        renderer.material.color = hitFlashColor;
        yield return new WaitForSeconds(hitFlashDuration);
        renderer.material.color = originalColor;
    }

    void TryHit()
    {
        Vector3 origin = playerCamera.transform.position;
        Vector3 direction = playerCamera.transform.forward;

        int layerMask = ~ignoreLayers;
        RaycastHit[] hits = Physics.RaycastAll(origin, direction, hitRange, layerMask);

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in hits)
        {
            if (IsOwnCollider(hit.collider))
                continue;

            Destructible destructible = hit.collider.GetComponentInParent<Destructible>();
            if (destructible != null)
            {
                destructible.TakeDamage(hitDamage);
                break;
            }
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

            Gizmos.color = Color.blue;
            Gizmos.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * hitRange);
        }
    }
}