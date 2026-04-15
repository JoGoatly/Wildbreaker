using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Destructible : MonoBehaviour
{
    [Header("Einstellungen")]
    public float health = 100f;
    public AudioClip breakSound;
    public AudioClip hitSound;

    [Header("Bruch Effekt")]
    public GameObject brokenPrefab;  // WallBroken Prefab reinziehen
    public float pieceForce = 3f;
    public float pieceForceRange = 1f;
    public float destroyDelay = 5f;
    public bool randomizeForce = true;

    [Header("Shake Animation")]
    public float shakeDuration = 0.15f;
    public float shakeMagnitude = 0.05f;

    private AudioSource audioSource;
    private Vector3 originalPosition;
    private bool isDestroyed = false;

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        originalPosition = transform.position;
    }

    public void TakeDamage(float damage)
    {
        if (isDestroyed) return;

        health -= damage;

        if (hitSound != null)
            audioSource.PlayOneShot(hitSound);

        StartCoroutine(ShakeAnimation());

        if (health <= 0f)
            Break();
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

        if (breakSound != null)
            AudioSource.PlayClipAtPoint(breakSound, transform.position);

        if (brokenPrefab != null)
        {
            // Prefab an exakt gleicher Position und Rotation spawnen
            GameObject broken = Instantiate(
                brokenPrefab,
                transform.position,
                transform.rotation
            );

            // Alle Rigidbodies der Bruchstücke mit Kraft versehen
            Rigidbody[] pieces = broken.GetComponentsInChildren<Rigidbody>();

            foreach (var rb in pieces)
            {
                if (randomizeForce)
                {
                    // Zufällige Richtung von der Mitte weg
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