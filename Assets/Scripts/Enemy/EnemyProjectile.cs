using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class EnemyProjectile : MonoBehaviour
{
    private float speed;
    private float damage;
    private float lifetime;
    private Vector3 direction;
    private bool initialized = false;
    private bool hasHit = false; // NEU: Überprüft, ob das Projektil bereits etwas getroffen hat
    private Rigidbody rb;

    public void Init(Vector3 dir, float spd, float dmg, float life)
    {
        direction = dir;
        speed = spd;
        damage = dmg;
        lifetime = life;
        initialized = true;

        rb = GetComponent<Rigidbody>();
        rb.useGravity = true; // GEÄNDERT: Schwerkraft an für die Parabel
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.linearVelocity = direction * speed;

        // Collider als Trigger setzen
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;

        Destroy(gameObject, lifetime);
    }

    // NEU: Lässt das Projektil in die Flugrichtung schauen (wie ein Pfeil)
    void Update()
    {
        if (initialized && !hasHit && rb.linearVelocity != Vector3.zero)
        {
            transform.forward = rb.linearVelocity.normalized;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!initialized || hasHit) return;

        // Eigenen Gegner ignorieren
        EnemyMelee melee = other.GetComponentInParent<EnemyMelee>();
        EnemyRanged ranged = other.GetComponentInParent<EnemyRanged>();
        if (melee != null || ranged != null) return;

        // Andere Projektile ignorieren
        if (other.GetComponent<EnemyProjectile>() != null) return;

        // Spieler treffen
        PlayerHealth player = other.GetComponentInParent<PlayerHealth>();
        if (player != null)
        {
            player.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        // GEÄNDERT: Wand oder Boden treffen -> Stecken bleiben
        StickToSurface();
    }

    // NEU: Logik zum Steckenbleiben
    void StickToSurface()
    {
        hasHit = true;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true; // Physik ausschalten, damit es in der Luft/im Boden stehen bleibt
        }

        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false; // Verhindert weitere Kollisionen/Triggers

        // Vorherigen Destroy-Befehl überschreiben und in 3 Sekunden despawnen
        Destroy(gameObject, 3f);
    }
}