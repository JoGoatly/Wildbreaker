using UnityEngine;
using System.Collections;

public class Destructible : MonoBehaviour
{
    [Header("Einstellungen")]
    public float health = 100f;

    [Header("Sound")]
    public AudioClip hitSound;
    public AudioClip lastHitBreakSound;

    [Header("Bruch Effekt")]
    public GameObject brokenPrefab;
    public Vector3 rotationOffset = new Vector3(0f, -90f, 0f);
    public float pieceForce = 3f;
    public float pieceForceRange = 1f;
    public float destroyDelay = 5f;
    public bool randomizeForce = true;

    [Header("Shake Animation")]
    public float shakeDuration = 0.15f;
    public float shakeMagnitude = 0.05f;

    private Vector3 originalPosition;
    private bool isDestroyed = false;

    void Start()
    {
        originalPosition = transform.position;
    }

    public void TakeDamage(float damage)
    {
        if (isDestroyed) return;

        health -= damage;

        if (health <= 0f)
        {
            // Final punch — play break sound through AudioManager
            if (lastHitBreakSound != null && AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(lastHitBreakSound);

            Break();
        }
        else
        {
            // Normal hit — play hit sound through AudioManager
            if (hitSound != null && AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(hitSound);

            StartCoroutine(ShakeAnimation());
        }
    }

    IEnumerator ShakeAnimation()
    {
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            transform.position = originalPosition + Random.insideUnitSphere * shakeMagnitude;
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = originalPosition;
    }

    void Break()
    {
        isDestroyed = true;

        if (brokenPrefab != null)
        {
            Quaternion correctedRotation = transform.rotation * Quaternion.Euler(rotationOffset);

            GameObject broken = Instantiate(
                brokenPrefab,
                transform.position,
                correctedRotation
            );

            Rigidbody[] pieces = broken.GetComponentsInChildren<Rigidbody>();

            foreach (var rb in pieces)
            {
                if (randomizeForce)
                {
                    Vector3 dir = (rb.transform.position - transform.position).normalized;
                    dir += Random.insideUnitSphere * 0.5f;
                    float force = Random.Range(pieceForce - pieceForceRange, pieceForce + pieceForceRange);

                    rb.AddForce(dir * force, ForceMode.Impulse);
                    rb.AddTorque(Random.insideUnitSphere * force * 0.5f, ForceMode.Impulse);
                }
                else
                {
                    rb.AddExplosionForce(pieceForce * 100f, transform.position, 5f);
                }
            }

            Destroy(broken, destroyDelay);
        }

        Destroy(gameObject);
    }
}