using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }

    [System.Serializable]
    public class CheckpointEntry
    {
        [Tooltip("Das Checkpoint-Prefab aus der Szene")]
        public CheckpointPrefab checkpoint;

        [Tooltip("Je höher der Index, desto weiter im Spiel")]
        public int index;

        [Tooltip("Wie nah der Spieler sein muss")]
        public float activationRange = 5f;
    }

    [Header("Checkpoint Liste")]
    public List<CheckpointEntry> checkpoints = new List<CheckpointEntry>();

    [Header("Lagerfeuer Effekte")]
    public AudioClip bonfireActivateSound;

    [Header("Debug")]
    [SerializeField] private int highestCheckpointIndex = -1;
    [SerializeField] private bool showGizmos = true;

    private PlayerHealth playerHealth;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        playerHealth = FindFirstObjectByType<PlayerHealth>();

        // Sortiere nach Index
        checkpoints.Sort((a, b) => a.index.CompareTo(b.index));

        // Lade gespeicherten Fortschritt
        int savedIndex = SaveSystem.LoadCheckpointIndex();
        if (savedIndex >= 0)
            ActivateUpTo(savedIndex);
    }

    void Update()
    {
        // Delete drücken = Spielstand löschen
        if (Keyboard.current.deleteKey.wasPressedThisFrame)
        {
            SaveSystem.DeleteSave();
            Debug.Log("Spielstand gelöscht!");
        }
        if (playerHealth == null || playerHealth.isDead) return;

        foreach (var entry in checkpoints)
        {
            if (entry.checkpoint == null) continue;
            if (entry.checkpoint.isActivated) continue;

            float distance = Vector3.Distance(
                playerHealth.transform.position,
                entry.checkpoint.transform.position
            );

            if (distance <= entry.activationRange)
                OnCheckpointReached(entry.index);
        }
    }

    void OnCheckpointReached(int index)
    {
        if (index <= highestCheckpointIndex) return;

        ActivateUpTo(index);
        SaveSystem.SaveCheckpoint(index);
    }

    void ActivateUpTo(int index)
    {
        highestCheckpointIndex = index;

        foreach (var entry in checkpoints)
        {
            if (entry.checkpoint == null) continue;

            if (entry.index <= highestCheckpointIndex)
                entry.checkpoint.Activate();
            else
                entry.checkpoint.Deactivate();
        }

        if (bonfireActivateSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(bonfireActivateSound);
    }

    public void RespawnPlayer()
    {
        if (playerHealth == null) return;

        CheckpointPrefab active = GetHighestCheckpoint();
        if (active == null) return;

        playerHealth.transform.position = active.GetRespawnPosition();
        playerHealth.transform.rotation = active.GetRespawnRotation();
        playerHealth.RestoreFullHealth();
    }

    CheckpointPrefab GetHighestCheckpoint()
    {
        foreach (var entry in checkpoints)
        {
            if (entry.checkpoint != null && entry.index == highestCheckpointIndex)
                return entry.checkpoint;
        }
        return null;
    }

    public int GetHighestCheckpointIndex()
    {
        return highestCheckpointIndex;
    }

    void OnDrawGizmos()
    {
        if (!showGizmos) return;

        foreach (var entry in checkpoints)
        {
            if (entry.checkpoint == null) continue;

            bool active = entry.checkpoint.isActivated;
            Vector3 pos = entry.checkpoint.transform.position;

            // Kreis für Activation Range
            Gizmos.color = active
                ? new Color(0f, 1f, 0f, 0.3f)
                : new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawWireSphere(pos, entry.activationRange);
            Gizmos.DrawSphere(pos, 0.3f);

            // Index anzeigen
#if UNITY_EDITOR
            UnityEditor.Handles.Label(pos + Vector3.up * 2f, $"Index: {entry.index}");
#endif
        }
    }
}