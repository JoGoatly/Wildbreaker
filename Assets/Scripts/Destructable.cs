using UnityEngine;
using System.Collections;

public class Destructible : MonoBehaviour
{
    [Header("Einstellungen")]
    public float health = 100f;
    public AudioClip breakSound;
    public AudioClip hitSound;

    [Header("Bruch Effekt")]
    public GameObject brokenPrefab;
    public int pieceCountX = 4;
    public int pieceCountY = 6;
    public int pieceCountZ = 1;
    public float pieceSize = 0.45f;
    public float destroyDelay = 4f;

    [Header("Zerbröckel-Physik")]
    [Range(0f, 0.5f)]
    public float lateralForce = 0.15f;
    [Range(0f, 0.3f)]
    public float upwardForce = 0.1f;
    public float tumbleForce = 1.5f;

    [Header("Spawn-Variation")]
    [Range(0f, 0.1f)]
    public float horizontalJitter = 0.05f;
    [Range(0.7f, 1f)]
    public float sizeVariation = 0.85f;

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

    // Wird vom PlayerAttack aufgerufen!
    public void TakeDamage(float damage)
    {
        if (isDestroyed) return;

        health -= damage;

        Debug.Log($"Wand getroffen! Noch {health} HP übrig.");

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
        if (isDestroyed) return;
        isDestroyed = true;

        if (breakSound != null)
            AudioSource.PlayClipAtPoint(breakSound, transform.position);

        Renderer rend = GetComponent<Renderer>();
        Bounds bounds = rend != null ? rend.bounds : new Bounds(transform.position, transform.lossyScale);

        float stepX = bounds.size.x / pieceCountX;
        float stepY = bounds.size.y / pieceCountY;
        float stepZ = bounds.size.z / Mathf.Max(1, pieceCountZ);

        for (int x = 0; x < pieceCountX; x++)
        {
            for (int y = 0; y < pieceCountY; y++)
            {
                for (int z = 0; z < pieceCountZ; z++)
                {
                    Vector3 spawnPos = new Vector3(
                        bounds.min.x + stepX * (x + 0.5f),
                        bounds.min.y + stepY * (y + 0.5f),
                        bounds.min.z + stepZ * (z + 0.5f)
                    );

                    spawnPos.x += Random.Range(-horizontalJitter, horizontalJitter);
                    spawnPos.z += Random.Range(-horizontalJitter, horizontalJitter);

                    GameObject piece = Instantiate(brokenPrefab, spawnPos, Random.rotation);

                    float size = pieceSize * Random.Range(sizeVariation, 1f);
                    piece.transform.localScale = new Vector3(size, size, size);

                    Rigidbody rb = piece.GetComponent<Rigidbody>();
                    if (rb == null)
                        rb = piece.AddComponent<Rigidbody>();

                    Vector3 force = new Vector3(
                        Random.Range(-lateralForce, lateralForce),
                        Random.Range(-0.05f, upwardForce),
                        Random.Range(-lateralForce, lateralForce)
                    );

                    rb.AddForce(force, ForceMode.Impulse);
                    rb.AddTorque(Random.insideUnitSphere * tumbleForce, ForceMode.Impulse);

                    Destroy(piece, destroyDelay);
                }
            }
        }

        Destroy(gameObject);
    }
}