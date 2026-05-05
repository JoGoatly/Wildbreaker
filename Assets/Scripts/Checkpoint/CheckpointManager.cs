using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using TMPro;

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

        [Tooltip("Text der beim Erreichen angezeigt wird")]
        public string areaName = "Unbekanntes Gebiet";

        [HideInInspector]
        public bool playerInside = false;

        [HideInInspector]
        public bool canShowText = true;

        [HideInInspector]
        public float lastExitTime = -999f;
    }

    [Header("Checkpoint Liste")]
    public List<CheckpointEntry> checkpoints = new List<CheckpointEntry>();

    [Header("Gebietsname UI")]
    public TMP_Text areaNameText;
    public CanvasGroup areaNameGroup;
    public float fadeInDuration = 0.5f;
    public float displayDuration = 3f;
    public float fadeOutDuration = 1.5f;

    [Header("Gebietsname Cooldown")]
    [Tooltip("Wie lange nach Verlassen der Zone gewartet wird bevor der Text erneut erscheinen kann")]
    public float areaNameCooldown = 10f;

    [Header("Sound")]
    [Tooltip("Spielt beim ersten Aktivieren eines Checkpoints")]
    public AudioClip bonfireActivateSound;

    [Tooltip("Spielt beim erneuten Betreten eines bereits aktivierten Checkpoints")]
    public AudioClip areaEnterSound;

    [Header("Debug")]
    [SerializeField] private int highestCheckpointIndex = -1;
    [SerializeField] private bool showGizmos = true;

    private PlayerHealth playerHealth;
    private Coroutine areaNameCoroutine;

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

        SaveSystem.DeleteSave();

        checkpoints.Sort((a, b) => a.index.CompareTo(b.index));

        if (areaNameGroup != null)
            areaNameGroup.alpha = 0f;

        // Alle Checkpoints zurücksetzen
        foreach (var entry in checkpoints)
        {
            entry.playerInside = false;
            entry.canShowText = true;
            entry.lastExitTime = -999f;
        }

        int savedIndex = SaveSystem.LoadCheckpointIndex();
        if (savedIndex >= 0)
            ActivateUpTo(savedIndex, false);
    }

    void Update()
    {
        // Delete = Spielstand löschen
        if (Keyboard.current != null && Keyboard.current.deleteKey.wasPressedThisFrame)
        {
            SaveSystem.DeleteSave();
            highestCheckpointIndex = -1;

            foreach (var entry in checkpoints)
            {
                if (entry.checkpoint != null)
                    entry.checkpoint.Deactivate();

                entry.playerInside = false;
                entry.canShowText = true;
                entry.lastExitTime = -999f;
            }

            Debug.Log("Spielstand gelöscht! Alle Checkpoints zurückgesetzt.");
        }

        if (playerHealth == null || playerHealth.isDead) return;

        foreach (var entry in checkpoints)
        {
            if (entry.checkpoint == null) continue;

            float distance = Vector3.Distance(
                playerHealth.transform.position,
                entry.checkpoint.transform.position
            );

            bool isInRange = distance <= entry.activationRange;
            bool wasInside = entry.playerInside;

            // Spieler betritt die Zone
            if (isInRange && !wasInside)
            {
                entry.playerInside = true;

                // Erstes Mal — Checkpoint aktivieren
                if (!entry.checkpoint.isActivated)
                {
                    OnCheckpointReached(entry);
                    entry.canShowText = false;
                }
                // Erneutes Betreten
                else
                {
                    // Prüfe ob genug Zeit vergangen ist seit dem letzten Verlassen
                    float timeSinceExit = Time.time - entry.lastExitTime;

                    if (timeSinceExit >= areaNameCooldown)
                    {
                        ShowAreaName(entry.areaName);
                        PlayAreaEnterSound();
                    }
                }
            }

            // Spieler verlässt die Zone
            if (!isInRange && wasInside)
            {
                entry.playerInside = false;
                entry.lastExitTime = Time.time;
            }
        }
    }

    void OnCheckpointReached(CheckpointEntry entry)
    {
        if (entry.index <= highestCheckpointIndex) return;

        ActivateUpTo(entry.index, true);
        ShowAreaName(entry.areaName);
        SaveSystem.SaveCheckpoint(entry.index);
    }

    void ActivateUpTo(int index, bool playSound)
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

        if (playSound && bonfireActivateSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(bonfireActivateSound);
    }

    void PlayAreaEnterSound()
    {
        if (areaEnterSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(areaEnterSound);
    }

    void ShowAreaName(string name)
    {
        if (areaNameText == null || areaNameGroup == null) return;

        areaNameText.text = name;

        if (areaNameCoroutine != null)
            StopCoroutine(areaNameCoroutine);

        areaNameCoroutine = StartCoroutine(AreaNameAnimation());
    }

    System.Collections.IEnumerator AreaNameAnimation()
    {
        // Fade In
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            areaNameGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            yield return null;
        }
        areaNameGroup.alpha = 1f;

        // Anzeigen
        yield return new WaitForSeconds(displayDuration);

        // Fade Out
        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            areaNameGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
            yield return null;
        }
        areaNameGroup.alpha = 0f;
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

            Gizmos.color = active
                ? new Color(0f, 1f, 0f, 0.3f)
                : new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawWireSphere(pos, entry.activationRange);
            Gizmos.DrawSphere(pos, 0.3f);

#if UNITY_EDITOR
            UnityEditor.Handles.Label(pos + Vector3.up * 2f, $"[{entry.index}] {entry.areaName}");
#endif
        }
    }
}