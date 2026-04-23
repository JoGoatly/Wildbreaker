using UnityEngine;

public class CheckpointPrefab : MonoBehaviour
{
    [Header("Respawn")]
    [Tooltip("Optional — wo der Spieler spawnt (wenn leer: Checkpoint Position)")]
    public Transform respawnPoint;

    [Header("Lagerfeuer")]
    [Tooltip("Das FireEffect Objekt direkt reinziehen")]
    public GameObject fireEffect;

    [HideInInspector]
    public bool isActivated = false;

    public void Activate()
    {
        if (isActivated) return;
        isActivated = true;

        if (fireEffect != null)
            fireEffect.SetActive(true);
    }

    public void Deactivate()
    {
        isActivated = false;

        if (fireEffect != null)
            fireEffect.SetActive(false);
    }

    public Vector3 GetRespawnPosition()
    {
        if (respawnPoint != null)
            return respawnPoint.position;
        return transform.position;
    }

    public Quaternion GetRespawnRotation()
    {
        if (respawnPoint != null)
            return respawnPoint.rotation;
        return transform.rotation;
    }
}