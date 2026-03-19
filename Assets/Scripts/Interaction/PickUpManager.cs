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
    }

    [Header("Aufhebbare Objekte")]
    public List<PickupEntry> pickupableItems;

    [Header("Tragen Einstellungen")]
    public float holdDistance = 2f;
    public float pickupRange = 3f;
    public float throwForce = 10f;

    private GameObject heldObject = null;
    private PickupEntry nearbyEntry = null;
    private InteractionManager interactionManager;
    private PlayerController playerController;
    private Camera playerCamera;

    void Start()
    {
        interactionManager = GetComponent<InteractionManager>();
        playerController = GetComponent<PlayerController>();
        playerCamera = GetComponentInChildren<Camera>();
    }

    void Update()
    {
        if (heldObject == null)
            CheckNearbyItems();

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (heldObject != null)
                DropObject();
            else if (nearbyEntry != null)
                PickupObject(nearbyEntry.item);
        }

        if (Mouse.current.leftButton.wasPressedThisFrame && heldObject != null)
            ThrowObject();
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

            float distance = Vector3.Distance(transform.position, entry.item.transform.position);

            if (distance <= pickupRange)
            {
                nearbyEntry = entry;

                // Bild oder Text anzeigen je nach Einstellung
                if (entry.promptImage != null)
                    interactionManager.OverridePromptImage(entry.promptImage);
                else
                    interactionManager.OverridePrompt(entry.promptText);

                return;
            }
        }

        interactionManager.ClearOverridePrompt();
    }

    void PickupObject(GameObject obj)
    {
        heldObject = obj;

        Rigidbody rb = heldObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        playerController.SetPickupCameraOffset(true);
        interactionManager.OverridePrompt("E - Fallen lassen\nLinksklick - Werfen");
    }

    void DropObject()
    {
        Rigidbody rb = heldObject.GetComponent<Rigidbody>();
        if (rb != null)
            rb.useGravity = true;

        heldObject = null;
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

        heldObject = null;
        playerController.SetPickupCameraOffset(false);
        interactionManager.ClearOverridePrompt();
    }

    void CarryObject()
    {
        Vector3 flatForward = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;

        Vector3 targetPosition = transform.position
            + flatForward * holdDistance
            + Vector3.up * 1.2f;

        float minDistance = 0.8f;
        Vector3 directionFromPlayer = targetPosition - transform.position;
        if (directionFromPlayer.magnitude < minDistance)
            targetPosition = transform.position + directionFromPlayer.normalized * minDistance;

        heldObject.transform.position = Vector3.Lerp(heldObject.transform.position, targetPosition, Time.deltaTime * 15f);
    }
}