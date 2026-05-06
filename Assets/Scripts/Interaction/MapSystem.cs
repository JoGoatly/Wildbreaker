using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class MapSystem : MonoBehaviour
{
    public static MapSystem Instance { get; private set; }

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
        UpdatePlayerMarker();
        UpdateCheckpointMarkers();

        // Kamera Position setzen
        mapCamera.transform.position = new Vector3(
            mapCameraPosition.x,
            mapHeight,
            mapCameraPosition.z
        );

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
            CloseMap();
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

        // Kamera startet über Spieler
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
            }

            lastMousePosition = currentMousePosition;
        }

        // Scroll = Zoom
        float scroll = Mouse.current.scroll.ReadValue().y;
        if (scroll != 0f)
        {
            mapCamera.orthographicSize -= scroll * zoomSpeed;
            mapCamera.orthographicSize = Mathf.Clamp(mapCamera.orthographicSize, minZoom, maxZoom);
        }
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
                        markerPos.y - 80f
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