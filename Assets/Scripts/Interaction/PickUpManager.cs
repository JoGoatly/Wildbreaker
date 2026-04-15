using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PickupManager : MonoBehaviour
{
    [System.Serializable]
    public class PickupEntry
    {
        public GameObject item;
        [TextArea]
        public string promptText;
        public Sprite promptImage;
        public AudioClip pickupSound;
        public AudioClip collisionSound;
        public bool showDropLine = true;
    }

    public static List<GameObject> currentlyHeld = new List<GameObject>();

    [Header("Aufhebbare Objekte")]
    public List<PickupEntry> pickupableItems;

    [Header("Tragen Einstellungen")]
    public float holdDistance = 2f;
    public float minHoldDistance = 1f;
    public float maxHoldDistance = 5f;
    public float scrollSpeed = 0.5f;
    public float pickupRange = 3f;
    public float throwForce = 10f;
    public float minImpactForce = 1f;

    [Header("Drop Line")]
    public Color lineColor = Color.white;
    public Color throwColor = Color.red;
    public float ringRadius = 0.3f;
    public int ringSegments = 32;

    [Header("Wurf Vorschau")]
    public int throwPreviewSteps = 60;
    public float throwPreviewTimestep = 0.05f;

    private float currentHoldDistance;
    private GameObject heldObject = null;
    private PickupEntry heldEntry = null;
    private PickupEntry nearbyEntry = null;
    private InteractionManager interactionManager;
    private PlayerController playerController;
    private Camera playerCamera;
    private LineRenderer lineRenderer;
    private LineRenderer ringRenderer;
    private LineRenderer throwLineRenderer;
    private LineRenderer throwRingRenderer;
    private bool isAiming = false;

    void Start()
    {
        interactionManager = GetComponent<InteractionManager>();
        playerController = GetComponent<PlayerController>();
        playerCamera = GetComponentInChildren<Camera>();
        currentHoldDistance = holdDistance;

        // No more local AudioSource — AudioManager handles everything

        lineRenderer = CreateLineRenderer("DropLine", 0.02f, false);
        ringRenderer = CreateLineRenderer("DropRing", 0.03f, true);
        throwLineRenderer = CreateLineRenderer("ThrowLine", 0.03f, false);
        throwRingRenderer = CreateLineRenderer("ThrowRing", 0.04f, true);

        foreach (var entry in pickupableItems)
        {
            if (entry.item == null) continue;
            SetupReporter(entry);
        }
    }

    LineRenderer CreateLineRenderer(string name, float width, bool loop)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(transform);
        LineRenderer lr = obj.AddComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startWidth = width;
        lr.endWidth = width;
        lr.positionCount = 0;
        lr.useWorldSpace = true;
        lr.loop = loop;
        return lr;
    }

    void SetupReporter(PickupEntry entry)
    {
        ItemCollisionReporter reporter = entry.item.GetComponent<ItemCollisionReporter>();
        if (reporter == null)
            reporter = entry.item.AddComponent<ItemCollisionReporter>();

        // Pass the AudioManager's sfx source so volume is always correct
        reporter.Init(AudioManager.Instance.GetSFXSource(), entry.collisionSound, minImpactForce);
    }

    void Update()
    {
        if (heldObject == null)
        {
            CheckNearbyItems();
            ClearAllLines();
        }
        else
        {
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (scroll != 0f)
            {
                currentHoldDistance += scroll * scrollSpeed * Time.deltaTime;
                currentHoldDistance = Mathf.Clamp(currentHoldDistance, minHoldDistance, maxHoldDistance);
            }

            isAiming = Mouse.current.rightButton.isPressed;

            if (isAiming)
            {
                lineRenderer.positionCount = 0;
                ringRenderer.positionCount = 0;
                DrawThrowPreview();
                interactionManager.OverridePrompt("Linksklick - Werfen\nE - Fallen lassen");
            }
            else
            {
                throwLineRenderer.positionCount = 0;
                throwRingRenderer.positionCount = 0;

                if (heldEntry != null && heldEntry.showDropLine)
                    DrawDropLine();
                else
                {
                    lineRenderer.positionCount = 0;
                    ringRenderer.positionCount = 0;
                }

                interactionManager.OverridePrompt("E - Fallen lassen\nRechtsklick halten - Zielen");
            }
        }

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (heldObject != null)
                DropObject();
            else if (nearbyEntry != null)
                PickupObject(nearbyEntry);
        }

        if (Mouse.current.leftButton.wasPressedThisFrame && heldObject != null)
            ThrowObject();
    }

    void DrawThrowPreview()
    {
        Collider col = heldObject.GetComponent<Collider>();
        Vector3 startPos = col != null ? col.bounds.center : heldObject.transform.position;
        Vector3 velocity = playerCamera.transform.forward * throwForce;

        List<Vector3> points = new List<Vector3>();
        Vector3 pos = startPos;
        Vector3 vel = velocity;
        Vector3 landingPoint = Vector3.zero;
        bool landed = false;

        for (int i = 0; i < throwPreviewSteps; i++)
        {
            vel += Physics.gravity * throwPreviewTimestep;
            Vector3 nextPos = pos + vel * throwPreviewTimestep;

            RaycastHit hit;
            if (Physics.Linecast(pos, nextPos, out hit))
            {
                points.Add(hit.point);
                landingPoint = hit.point;
                landed = true;
                break;
            }

            points.Add(nextPos);
            pos = nextPos;
        }

        throwLineRenderer.positionCount = points.Count;
        for (int i = 0; i < points.Count; i++)
            throwLineRenderer.SetPosition(i, points[i]);

        throwLineRenderer.startColor = throwColor;
        throwLineRenderer.endColor = new Color(throwColor.r, throwColor.g, throwColor.b, 0f);

        if (landed)
            DrawRing(throwRingRenderer, landingPoint, throwColor);
        else
            throwRingRenderer.positionCount = 0;
    }

    void DrawDropLine()
    {
        Collider col = heldObject.GetComponent<Collider>();
        Vector3 startPos = col != null ? col.bounds.center : heldObject.transform.position;

        RaycastHit hit;
        bool didHit = Physics.Raycast(startPos, Vector3.down, out hit, 100f);
        Vector3 endPos = didHit ? hit.point : startPos + Vector3.down * 10f;

        int pointCount = 20;
        lineRenderer.positionCount = pointCount;
        for (int i = 0; i < pointCount; i++)
        {
            float t = (float)i / (pointCount - 1);
            lineRenderer.SetPosition(i, Vector3.Lerp(startPos, endPos, t));
        }
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = new Color(lineColor.r, lineColor.g, lineColor.b, 0f);

        if (didHit)
            DrawRing(ringRenderer, hit.point, lineColor);
        else
            ringRenderer.positionCount = 0;
    }

    void DrawRing(LineRenderer lr, Vector3 hitPoint, Color color)
    {
        RaycastHit hit;
        Vector3 normal = Vector3.up;
        if (Physics.Raycast(hitPoint + Vector3.up * 0.1f, Vector3.down, out hit, 0.5f))
            normal = hit.normal;

        Vector3 right = Vector3.Cross(normal, Vector3.forward);
        if (right.magnitude < 0.01f)
            right = Vector3.Cross(normal, Vector3.right);
        right = right.normalized;
        Vector3 forward = Vector3.Cross(normal, right).normalized;

        lr.positionCount = ringSegments;
        lr.startColor = new Color(color.r, color.g, color.b, 0.8f);
        lr.endColor = new Color(color.r, color.g, color.b, 0.8f);

        for (int i = 0; i < ringSegments; i++)
        {
            float angle = (float)i / ringSegments * Mathf.PI * 2f;
            Vector3 point = hitPoint
                + normal * 0.01f
                + right * Mathf.Cos(angle) * ringRadius
                + forward * Mathf.Sin(angle) * ringRadius;
            lr.SetPosition(i, point);
        }
    }

    void ClearAllLines()
    {
        lineRenderer.positionCount = 0;
        ringRenderer.positionCount = 0;
        throwLineRenderer.positionCount = 0;
        throwRingRenderer.positionCount = 0;
    }

    void FixedUpdate()
    {
        if (heldObject != null)
            CarryObject();
    }

    void CheckNearbyItems()
    {
        nearbyEntry = null;

        foreach (var entry in pickupableItems)
        {
            if (entry.item == null) continue;
            if (currentlyHeld.Contains(entry.item)) continue;

            float distance = Vector3.Distance(transform.position, entry.item.transform.position);

            if (distance <= pickupRange)
            {
                nearbyEntry = entry;

                if (entry.promptImage != null)
                    interactionManager.OverridePromptImage(entry.promptImage);
                else
                    interactionManager.OverridePrompt(entry.promptText);

                return;
            }
        }

        interactionManager.ClearOverridePrompt();
    }

    void PickupObject(PickupEntry entry)
    {
        heldObject = entry.item;
        heldEntry = entry;
        currentHoldDistance = holdDistance;
        currentlyHeld.Add(heldObject);

        Rigidbody rb = heldObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Route through AudioManager so SFX volume is respected
        if (entry.pickupSound != null)
            AudioManager.Instance.PlaySFX(entry.pickupSound);

        playerController.SetPickupCameraOffset(true);
        interactionManager.OverridePrompt("E - Fallen lassen\nRechtsklick halten - Zielen");
    }

    void DropObject()
    {
        Rigidbody rb = heldObject.GetComponent<Rigidbody>();
        if (rb != null)
            rb.useGravity = true;

        currentlyHeld.Remove(heldObject);
        ClearAllLines();
        heldObject = null;
        heldEntry = null;
        isAiming = false;
        playerController.SetPickupCameraOffset(false);
        interactionManager.ClearOverridePrompt();
    }

    void ThrowObject()
    {
        Rigidbody rb = heldObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = true;
            rb.AddForce(playerCamera.transform.forward * throwForce, ForceMode.Impulse);
        }

        currentlyHeld.Remove(heldObject);
        ClearAllLines();
        heldObject = null;
        heldEntry = null;
        isAiming = false;
        playerController.SetPickupCameraOffset(false);
        interactionManager.ClearOverridePrompt();
    }

    void CarryObject()
    {
        Vector3 flatForward = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;

        Vector3 targetPosition = transform.position
            + flatForward * currentHoldDistance
            + Vector3.up * 1.2f;

        float minDistance = 0.8f;
        Vector3 directionFromPlayer = targetPosition - transform.position;
        if (directionFromPlayer.magnitude < minDistance)
            targetPosition = transform.position + directionFromPlayer.normalized * minDistance;

        heldObject.transform.position = Vector3.Lerp(heldObject.transform.position, targetPosition, Time.deltaTime * 15f);
    }
}

public class ItemCollisionReporter : MonoBehaviour
{
    private AudioSource audioSource;
    private AudioClip collisionClip;
    private float minImpactForce;

    public void Init(AudioSource source, AudioClip clip, float minForce)
    {
        audioSource = source;
        collisionClip = clip;
        minImpactForce = minForce;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collisionClip == null) return;
        if (audioSource == null) return;

        float impact = collision.relativeVelocity.magnitude;
        if (impact < minImpactForce) return;

        float volume = Mathf.Clamp01(impact / 10f);
        audioSource.PlayOneShot(collisionClip, volume);
    }
}