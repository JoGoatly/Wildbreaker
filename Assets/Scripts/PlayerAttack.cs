using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    [Header("Angriff")]
    public float damage = 34f;
    public float range = 5f;
    public LayerMask ignoreLayers;

    private Camera playerCamera;

    void Start()
    {
        playerCamera = GetComponentInChildren<Camera>();
    }

    void Update()
    {
        if (Keyboard.current.xKey.wasPressedThisFrame)
            TryHit();
    }

    void TryHit()
    {
        Vector3 origin = playerCamera.transform.position;
        Vector3 direction = playerCamera.transform.forward;

        int layerMask = ~ignoreLayers;

        RaycastHit[] hits = Physics.RaycastAll(origin, direction, range, layerMask);

        foreach (var hit in hits)
        {
            Destructible destructible = hit.collider.GetComponent<Destructible>();
            if (destructible != null)
            {
                destructible.TakeDamage(damage);
                break;
            }
        }
    }
}