using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class MapSystem : MonoBehaviour
{
    public static MapSystem Instance { get; private set; }

    [System.Serializable]
    public class ChapterEntry
    {
        public string chapterName = "Kapitel 1";
        public List<int> checkpointIndices = new List<int>();
    }

    [Header("Map Kamera")]
    public Camera mapCamera;
    public float mapHeight = 50f;
    public float dragSpeed = 2f;
    public float zoomSpeed = 0.5f;
    public float minZoom = 10f;
    public float maxZoom = 80f;

    [Header("Map UI")]
    public GameObject mapPanel;
    public RawImage mapRenderImage;

    [Header("Spieler Markierung (Map)")]
    public RectTransform playerMarker;
    public Image playerMarkerImage;
    public Color playerMarkerColor = Color.cyan;
    public float markerPulseSpeed = 2f;
    public float markerMinSize = 15f;
    public float markerMaxSize = 25f;

    [Header("Checkpoint Markierungen (Map)")]
    public GameObject checkpointMarkerPrefab;
    public Transform checkpointMarkerParent;

    [Header("Schnellreise")]
    public GameObject fastTravelPanel;
    public TMP_Text fastTravelAreaName;
    public Button fastTravelButton;
    public Button fastTravelCancelButton;

    [Header("Kapitel Liste (Links)")]
    public List<ChapterEntry> chapters = new List<ChapterEntry>();
    public GameObject chapterListPanel;
    public Transform chapterListParent;
    public GameObject chapterHeaderPrefab;
    public GameObject chapterEntryPrefab;

    [Header("Minimap")]
    public Camera minimapCamera;
    public RawImage minimapRenderImage;
    public GameObject minimapPanel;
    public float minimapHeight = 50f;
    public float minimapZoom = 20f;
    public RectTransform minimapPlayerMarker;
    public Image minimapPlayerMarkerImage;
    public Color minimapPlayerColor = Color.cyan;
    public float minimapPlayerSize = 12f;
    public GameObject minimapCheckpointMarkerPrefab;
    public Transform minimapCheckpointMarkerParent;

    [Header("Sounds")]
    public AudioClip mapOpenSound;
    public AudioClip mapCloseSound;
    public AudioClip fastTravelSound;
    public AudioClip checkpointClickSound;
    public AudioClip cancelSound;

    private bool isMapOpen = false;
    private bool isDragging = false;
    private Vector3 lastMousePosition;
    private Vector3 mapCameraPosition;
    private RenderTexture mapRenderTexture;
    private RenderTexture minimapRenderTexture;
    private PlayerHealth playerHealth;
    private PlayerController playerController;

    private List<CheckpointMarkerData> checkpointMarkers = new List<CheckpointMarkerData>();
    private List<MinimapMarkerData> minimapMarkers = new List<MinimapMarkerData>();
    private List<GameObject> chapterListItems = new List<GameObject>();
    private int selectedCheckpointIndex = -1;

    private class CheckpointMarkerData
    {
        public RectTransform rectTransform;
        public TMP_Text nameText;
        public Button button;
        public int checkpointIndex;
        public Vector3 worldPosition;
    }

    private class MinimapMarkerData
    {
        public RectTransform rectTransform;
        public int checkpointIndex;
        public Vector3 worldPosition;
    }

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        playerHealth = FindFirstObjectByType<PlayerHealth>();
        playerController = FindFirstObjectByType<PlayerController>();

        // Map Kamera Setup
        mapRenderTexture = new RenderTexture(1024, 1024, 16);
        mapCamera.targetTexture = mapRenderTexture;
        mapRenderImage.texture = mapRenderTexture;

        mapCamera.orthographic = true;
        mapCamera.orthographicSize = 30f;
        mapCamera.nearClipPlane = 0.3f;
        mapCamera.farClipPlane = 100f;
        mapCamera.clearFlags = CameraClearFlags.SolidColor;
        mapCamera.backgroundColor = new Color(0.12f, 0.12f, 0.12f);
        mapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        mapPanel.SetActive(false);
        mapCamera.gameObject.SetActive(false);

        if (fastTravelPanel != null)
            fastTravelPanel.SetActive(false);

        // Minimap Kamera Setup
        minimapRenderTexture = new RenderTexture(512, 512, 16);
        minimapCamera.targetTexture = minimapRenderTexture;
        minimapRenderImage.texture = minimapRenderTexture;

        minimapCamera.orthographic = true;
        minimapCamera.orthographicSize = minimapZoom;
        minimapCamera.nearClipPlane = 0.3f;
        minimapCamera.farClipPlane = 100f;
        minimapCamera.clearFlags = CameraClearFlags.SolidColor;
        minimapCamera.backgroundColor = new Color(0.12f, 0.12f, 0.12f);

        if (minimapPlayerMarkerImage != null)
            minimapPlayerMarkerImage.color = minimapPlayerColor;
        if (minimapPlayerMarker != null)
            minimapPlayerMarker.sizeDelta = new Vector2(minimapPlayerSize, minimapPlayerSize);

        if (fastTravelButton != null)
            fastTravelButton.onClick.AddListener(OnFastTravel);
        if (fastTravelCancelButton != null)
            fastTravelCancelButton.onClick.AddListener(OnCancel);

        if (playerMarkerImage != null)
            playerMarkerImage.color = playerMarkerColor;

        CreateCheckpointMarkers();
        CreateMinimapMarkers();
        CreateChapterList();
    }

    void Update()
    {
        UpdateMinimap();

        if (Keyboard.current.mKey.wasPressedThisFrame)
        {
            if (isMapOpen)
                CloseMap();
            else
                OpenMap();
        }

        if (!isMapOpen) return;

        HandleMapDrag();

        mapCamera.transform.position = new Vector3(
            mapCameraPosition.x,
            mapHeight,
            mapCameraPosition.z
        );

        UpdatePlayerMarker();
        UpdateCheckpointMarkers();

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
            CloseMap();
    }

    // ──────────────────────────────────────
    // Kapitel Liste
    // ──────────────────────────────────────

    void CreateChapterList()
    {
        if (CheckpointManager.Instance == null) return;
        if (chapterListParent == null) return;

        foreach (var chapter in chapters)
        {
            if (chapterHeaderPrefab != null)
            {
                GameObject header = Instantiate(chapterHeaderPrefab, chapterListParent);
                TMP_Text headerText = header.GetComponentInChildren<TMP_Text>();
                if (headerText != null)
                    headerText.text = chapter.chapterName;
                chapterListItems.Add(header);
            }

            foreach (int cpIndex in chapter.checkpointIndices)
            {
                if (chapterEntryPrefab == null) continue;

                CheckpointManager.CheckpointEntry cpEntry = null;
                foreach (var entry in CheckpointManager.Instance.checkpoints)
                {
                    if (entry.index == cpIndex)
                    {
                        cpEntry = entry;
                        break;
                    }
                }

                if (cpEntry == null || cpEntry.checkpoint == null) continue;

                GameObject entryObj = Instantiate(chapterEntryPrefab, chapterListParent);
                TMP_Text entryText = entryObj.GetComponentInChildren<TMP_Text>();
                Button entryButton = entryObj.GetComponentInChildren<Button>();

                if (entryText != null)
                    entryText.text = cpEntry.areaName;

                int capturedIndex = cpIndex;
                string capturedName = cpEntry.areaName;
                Vector3 capturedPos = cpEntry.checkpoint.transform.position;

                if (entryButton != null)
                {
                    entryButton.onClick.AddListener(() =>
                    {
                        OnChapterEntryClicked(capturedIndex, capturedName, capturedPos);
                    });
                }

                chapterListItems.Add(entryObj);
                entryObj.SetActive(false);
            }
        }
    }

    void UpdateChapterList()
    {
        if (CheckpointManager.Instance == null) return;

        int highestIndex = CheckpointManager.Instance.GetHighestCheckpointIndex();

        if (chapterListPanel != null)
            chapterListPanel.SetActive(highestIndex >= 0);

        if (highestIndex < 0) return;

        int itemIndex = 0;

        foreach (var chapter in chapters)
        {
            bool anyVisible = false;

            if (chapterHeaderPrefab != null)
                itemIndex++;

            foreach (int cpIndex in chapter.checkpointIndices)
            {
                if (itemIndex >= chapterListItems.Count) break;

                bool unlocked = cpIndex <= highestIndex;
                chapterListItems[itemIndex].SetActive(unlocked);

                if (unlocked)
                    anyVisible = true;

                itemIndex++;
            }

            if (chapterHeaderPrefab != null)
            {
                int headerIndex = itemIndex - chapter.checkpointIndices.Count - 1;
                if (headerIndex >= 0 && headerIndex < chapterListItems.Count)
                    chapterListItems[headerIndex].SetActive(anyVisible);
            }
        }
    }

    void OnChapterEntryClicked(int index, string areaName, Vector3 worldPos)
    {
        mapCameraPosition = new Vector3(worldPos.x, mapHeight, worldPos.z);

        mapCamera.transform.position = new Vector3(
            mapCameraPosition.x,
            mapHeight,
            mapCameraPosition.z
        );

        UpdateCheckpointMarkers();

        if (fastTravelPanel != null)
            fastTravelPanel.SetActive(false);

        selectedCheckpointIndex = index;

        if (fastTravelPanel != null)
        {
            foreach (var marker in checkpointMarkers)
            {
                if (marker.checkpointIndex == index)
                {
                    RectTransform panelRect = fastTravelPanel.GetComponent<RectTransform>();
                    Vector2 markerPos = marker.rectTransform.anchoredPosition;

                    panelRect.anchoredPosition = new Vector2(
                        markerPos.x,
                        markerPos.y - 30f
                    );

                    break;
                }
            }

            fastTravelPanel.SetActive(true);

            if (fastTravelAreaName != null)
                fastTravelAreaName.text = areaName;
        }

        PlaySound(checkpointClickSound);
    }

    // ──────────────────────────────────────
    // Minimap
    // ──────────────────────────────────────

    void UpdateMinimap()
    {
        if (playerHealth == null) return;

        Vector3 playerPos = playerHealth.transform.position;
        minimapCamera.transform.position = new Vector3(playerPos.x, minimapHeight, playerPos.z);

        float playerYRotation = playerHealth.transform.eulerAngles.y;
        minimapCamera.transform.rotation = Quaternion.Euler(90f, playerYRotation, 0f);

        if (minimapPlayerMarker != null)
            minimapPlayerMarker.anchoredPosition = Vector2.zero;

        UpdateMinimapMarkers();
    }

    void CreateMinimapMarkers()
    {
        if (CheckpointManager.Instance == null) return;
        if (minimapCheckpointMarkerPrefab == null || minimapCheckpointMarkerParent == null) return;

        foreach (var entry in CheckpointManager.Instance.checkpoints)
        {
            if (entry.checkpoint == null) continue;

            GameObject markerObj = Instantiate(minimapCheckpointMarkerPrefab, minimapCheckpointMarkerParent);

            MinimapMarkerData data = new MinimapMarkerData();
            data.rectTransform = markerObj.GetComponent<RectTransform>();
            data.checkpointIndex = entry.index;
            data.worldPosition = entry.checkpoint.transform.position;

            minimapMarkers.Add(data);
            markerObj.SetActive(false);
        }
    }

    void UpdateMinimapMarkers()
    {
        if (CheckpointManager.Instance == null) return;

        int highestIndex = CheckpointManager.Instance.GetHighestCheckpointIndex();

        foreach (var marker in minimapMarkers)
        {
            bool shouldShow = marker.checkpointIndex <= highestIndex;
            marker.rectTransform.gameObject.SetActive(shouldShow);

            if (shouldShow)
            {
                Vector3 viewportPos = minimapCamera.WorldToViewportPoint(marker.worldPosition);

                bool inView = viewportPos.x > 0f && viewportPos.x < 1f &&
                              viewportPos.y > 0f && viewportPos.y < 1f &&
                              viewportPos.z > 0f;

                marker.rectTransform.gameObject.SetActive(shouldShow && inView);

                if (inView)
                {
                    RectTransform renderRect = minimapRenderImage.rectTransform;
                    float x = (viewportPos.x - 0.5f) * renderRect.rect.width;
                    float y = (viewportPos.y - 0.5f) * renderRect.rect.height;
                    marker.rectTransform.anchoredPosition = new Vector2(x, y);
                }
            }
        }
    }

    // ──────────────────────────────────────
    // Große Map
    // ──────────────────────────────────────

    void PlaySound(AudioClip clip)
    {
        if (clip != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(clip);
    }

    void OpenMap()
    {
        isMapOpen = true;
        mapPanel.SetActive(true);
        mapCamera.gameObject.SetActive(true);

        if (playerHealth != null)
        {
            Vector3 playerPos = playerHealth.transform.position;
            mapCameraPosition = new Vector3(playerPos.x, mapHeight, playerPos.z);
        }

        if (minimapPanel != null)
            minimapPanel.SetActive(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (playerController != null)
            playerController.canMove = false;

        isDragging = false;

        UpdateCheckpointMarkers();
        UpdateChapterList();
        PlaySound(mapOpenSound);
    }

    void CloseMap()
    {
        isMapOpen = false;
        mapPanel.SetActive(false);
        mapCamera.gameObject.SetActive(false);

        if (minimapPanel != null)
            minimapPanel.SetActive(true);

        CloseFastTravelPanel();

        if (playerHealth != null && !playerHealth.isDead)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (playerController != null)
            playerController.canMove = true;

        PlaySound(mapCloseSound);
    }

    void HandleMapDrag()
    {
        bool startDrag = Mouse.current.leftButton.wasPressedThisFrame ||
                         Mouse.current.rightButton.wasPressedThisFrame;
        bool holding = Mouse.current.leftButton.isPressed ||
                       Mouse.current.rightButton.isPressed;

        if (startDrag && !isDragging)
        {
            isDragging = true;
            lastMousePosition = Mouse.current.position.ReadValue();
        }

        if (!holding)
        {
            isDragging = false;
        }

        if (isDragging)
        {
            Vector3 currentMousePosition = Mouse.current.position.ReadValue();
            Vector3 delta = currentMousePosition - lastMousePosition;

            if (delta.magnitude > 0.5f)
            {
                float zoomFactor = mapCamera.orthographicSize / 30f;
                float speed = dragSpeed * zoomFactor;

                mapCameraPosition.x -= delta.x * speed * Time.unscaledDeltaTime;
                mapCameraPosition.z -= delta.y * speed * Time.unscaledDeltaTime;

                // Panel schließen beim Wischen
                if (fastTravelPanel != null && fastTravelPanel.activeSelf)
                    CloseFastTravelPanel();
            }

            lastMousePosition = currentMousePosition;
        }

        float scroll = Mouse.current.scroll.ReadValue().y;
        if (scroll != 0f && !IsMouseOverChapterList())
        {
            mapCamera.orthographicSize -= scroll * zoomSpeed;
            mapCamera.orthographicSize = Mathf.Clamp(mapCamera.orthographicSize, minZoom, maxZoom);
        }
    }

    bool IsMouseOverChapterList()
    {
        if (chapterListPanel == null) return false;
        if (!chapterListPanel.activeSelf) return false;

        RectTransform rect = chapterListPanel.GetComponent<RectTransform>();
        Vector2 mousePos = Mouse.current.position.ReadValue();

        return RectTransformUtility.RectangleContainsScreenPoint(rect, mousePos);
    }

    void UpdatePlayerMarker()
    {
        if (playerMarker == null || playerHealth == null) return;

        Vector3 playerWorldPos = playerHealth.transform.position;
        Vector2 screenPos = WorldToMapScreenPosition(playerWorldPos);

        playerMarker.anchoredPosition = screenPos;

        float pulse = Mathf.Lerp(markerMinSize, markerMaxSize,
            (Mathf.Sin(Time.unscaledTime * markerPulseSpeed) + 1f) * 0.5f);
        playerMarker.sizeDelta = new Vector2(pulse, pulse);
    }

    void CreateCheckpointMarkers()
    {
        if (CheckpointManager.Instance == null) return;
        if (checkpointMarkerPrefab == null || checkpointMarkerParent == null) return;

        foreach (var entry in CheckpointManager.Instance.checkpoints)
        {
            if (entry.checkpoint == null) continue;

            GameObject markerObj = Instantiate(checkpointMarkerPrefab, checkpointMarkerParent);

            CheckpointMarkerData data = new CheckpointMarkerData();
            data.rectTransform = markerObj.GetComponent<RectTransform>();
            data.nameText = markerObj.GetComponentInChildren<TMP_Text>();
            data.button = markerObj.GetComponentInChildren<Button>();
            data.checkpointIndex = entry.index;
            data.worldPosition = entry.checkpoint.transform.position;

            if (data.nameText != null)
                data.nameText.text = entry.areaName;

            int capturedIndex = entry.index;
            string capturedName = entry.areaName;
            if (data.button != null)
                data.button.onClick.AddListener(() => OnCheckpointClicked(capturedIndex, capturedName));

            checkpointMarkers.Add(data);
            markerObj.SetActive(false);
        }
    }

    void UpdateCheckpointMarkers()
    {
        if (CheckpointManager.Instance == null) return;

        int highestIndex = CheckpointManager.Instance.GetHighestCheckpointIndex();

        foreach (var marker in checkpointMarkers)
        {
            bool shouldShow = marker.checkpointIndex <= highestIndex;
            marker.rectTransform.gameObject.SetActive(shouldShow);

            if (shouldShow)
            {
                Vector2 screenPos = WorldToMapScreenPosition(marker.worldPosition);
                marker.rectTransform.anchoredPosition = screenPos;
            }
        }
    }

    Vector2 WorldToMapScreenPosition(Vector3 worldPos)
    {
        Vector3 viewportPos = mapCamera.WorldToViewportPoint(worldPos);

        RectTransform renderRect = mapRenderImage.rectTransform;
        float x = (viewportPos.x - 0.5f) * renderRect.rect.width;
        float y = (viewportPos.y - 0.5f) * renderRect.rect.height;

        return new Vector2(x, y);
    }

    void OnCheckpointClicked(int index, string areaName)
    {
        selectedCheckpointIndex = index;

        if (fastTravelPanel != null)
        {
            foreach (var marker in checkpointMarkers)
            {
                if (marker.checkpointIndex == index)
                {
                    RectTransform panelRect = fastTravelPanel.GetComponent<RectTransform>();
                    Vector2 markerPos = marker.rectTransform.anchoredPosition;

                    panelRect.anchoredPosition = new Vector2(
                        markerPos.x,
                        markerPos.y - 30f
                    );

                    break;
                }
            }

            fastTravelPanel.SetActive(true);

            if (fastTravelAreaName != null)
                fastTravelAreaName.text = areaName;
        }

        PlaySound(checkpointClickSound);
    }

    void OnFastTravel()
    {
        if (CheckpointManager.Instance == null) return;

        foreach (var entry in CheckpointManager.Instance.checkpoints)
        {
            if (entry.index == selectedCheckpointIndex && entry.checkpoint != null)
            {
                if (playerHealth != null)
                {
                    playerHealth.transform.position = entry.checkpoint.GetRespawnPosition();
                    playerHealth.transform.rotation = entry.checkpoint.GetRespawnRotation();
                }
                break;
            }
        }

        PlaySound(fastTravelSound);
        CloseMap();
    }

    void OnCancel()
    {
        PlaySound(cancelSound);
        CloseFastTravelPanel();
    }

    void CloseFastTravelPanel()
    {
        if (fastTravelPanel != null)
            fastTravelPanel.SetActive(false);

        selectedCheckpointIndex = -1;
    }

    void OnDestroy()
    {
        if (mapRenderTexture != null)
            mapRenderTexture.Release();
        if (minimapRenderTexture != null)
            minimapRenderTexture.Release();
    }
}