using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float sprintSpeed = 10f;
    public float mouseSensitivity = 2f;
    public float jumpForce = 5f;
    public float airSpeedMultiplier = 1.5f;
    public bool canMove = true;

    [Header("Dodge / Ausweichen")]
    public float dodgeForce = 12f;
    public float dodgeDuration = 0.4f;
    public float dodgeCooldown = 0.8f;
    public float dodgeInvincibilityDuration = 0.3f;

    [Header("Pickup Kamera")]
    public float pickupOffsetX = -0.5f;
    public float cameraShiftSpeed = 5f;

    [Header("Respawn")]
    public float fallThreshold = -20f;
    public float savePositionInterval = 0.5f;

    public Transform groundCheck;
    public float groundDistance = 0.3f;
    public LayerMask groundMask;

    private float verticalRotation = 0f;
    private Camera playerCamera;
    private Rigidbody rb;
    private bool isGrounded;
    private Vector3 defaultCameraLocalPosition;
    private Vector3 targetCameraLocalPosition;

    // Respawn
    private Vector3 lastSafePosition;
    private Quaternion lastSafeRotation;
    private float saveTimer;

    // Dodge
    private bool isDodging = false;
    private bool isInvincible = false;
    private float dodgeTimer = 0f;
    private float dodgeCooldownTimer = 0f;
    private float invincibilityTimer = 0f;
    private Vector3 dodgeDirection;

    // Public property damit EnemyMelee prüfen kann ob Spieler unverwundbar ist
    public bool IsInvincible => isInvincible;

    void Start()
    {
        playerCamera = GetComponentInChildren<Camera>();
        rb = GetComponent<Rigidbody>();

        defaultCameraLocalPosition = playerCamera.transform.localPosition;
        targetCameraLocalPosition = defaultCameraLocalPosition;

        lastSafePosition = transform.position;
        lastSafeRotation = transform.rotation;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleMouseLook();
        HandleJump();
        HandleDodge();
        UpdateDodgeTimers();
        CheckFallRespawn();
        SaveSafePosition();

        playerCamera.transform.localPosition = Vector3.Lerp(
            playerCamera.transform.localPosition,
            targetCameraLocalPosition,
            Time.deltaTime * cameraShiftSpeed
        );

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void FixedUpdate()
    {
        if (isDodging)
        {
            rb.linearVelocity = new Vector3(
                dodgeDirection.x * dodgeForce,
                rb.linearVelocity.y,
                dodgeDirection.z * dodgeForce
            );
        }
        else
        {
            HandleMovement();
        }

        CheckGround();
    }

    void CheckGround()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
    }

    void HandleDodge()
    {
        if (!canMove) return;
        if (!isGrounded) return;
        if (isDodging) return;
        if (dodgeCooldownTimer > 0f) return;

        // Rechte Maustaste für Dodge
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            StartDodge();
        }
    }

    void StartDodge()
    {
        // Bewegungsrichtung ermitteln
        Vector2 moveInput = new Vector2(
            Keyboard.current.dKey.isPressed ? 1f : Keyboard.current.aKey.isPressed ? -1f : 0f,
            Keyboard.current.wKey.isPressed ? 1f : Keyboard.current.sKey.isPressed ? -1f : 0f
        );

        // Wenn keine Richtung gedrückt → nach hinten ausweichen
        if (moveInput.sqrMagnitude < 0.01f)
        {
            dodgeDirection = -transform.forward;
        }
        else
        {
            dodgeDirection = (transform.right * moveInput.x + transform.forward * moveInput.y).normalized;
        }

        isDodging = true;
        isInvincible = true;
        dodgeTimer = dodgeDuration;
        dodgeCooldownTimer = dodgeCooldown;
        invincibilityTimer = dodgeInvincibilityDuration;
    }

    void UpdateDodgeTimers()
    {
        // Dodge-Duration
        if (isDodging)
        {
            dodgeTimer -= Time.deltaTime;
            if (dodgeTimer <= 0f)
            {
                isDodging = false;
            }
        }

        // Invincibility
        if (isInvincible)
        {
            invincibilityTimer -= Time.deltaTime;
            if (invincibilityTimer <= 0f)
            {
                isInvincible = false;
            }
        }

        // Cooldown
        if (dodgeCooldownTimer > 0f)
        {
            dodgeCooldownTimer -= Time.deltaTime;
        }
    }

    void SaveSafePosition()
    {
        saveTimer += Time.deltaTime;

        if (isGrounded && saveTimer >= savePositionInterval)
        {
            lastSafePosition = transform.position;
            lastSafeRotation = transform.rotation;
            saveTimer = 0f;
        }
    }

    void CheckFallRespawn()
    {
        if (transform.position.y < fallThreshold)
        {
            Respawn();
        }
    }

    void Respawn()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        transform.position = lastSafePosition;
        transform.rotation = lastSafeRotation;
    }

    void HandleJump()
    {
        if (!canMove) return;
        if (isDodging) return;

        if (Keyboard.current.spaceKey.wasPressedThisFrame && isGrounded)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    void HandleMovement()
    {
        if (!canMove)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            return;
        }

        Vector2 moveInput = new Vector2(
            Keyboard.current.dKey.isPressed ? 1f : Keyboard.current.aKey.isPressed ? -1f : 0f,
            Keyboard.current.wKey.isPressed ? 1f : Keyboard.current.sKey.isPressed ? -1f : 0f
        );

        bool isSprinting = Keyboard.current.leftShiftKey.isPressed;
        float currentSpeed = isSprinting ? sprintSpeed : moveSpeed;

        if (!isGrounded)
            currentSpeed *= airSpeedMultiplier;

        Vector3 moveDirection = transform.right * moveInput.x + transform.forward * moveInput.y;
        rb.linearVelocity = new Vector3(
            moveDirection.x * currentSpeed,
            rb.linearVelocity.y,
            moveDirection.z * currentSpeed
        );
    }

    void HandleMouseLook()
    {
        if (!canMove) return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue() * mouseSensitivity * 0.1f;

        transform.Rotate(0f, mouseDelta.x, 0f);

        verticalRotation -= mouseDelta.y;
        verticalRotation = Mathf.Clamp(verticalRotation, -80f, 80f);
        playerCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
    }

    public void SetPickupCameraOffset(bool holding)
    {
        if (holding)
            targetCameraLocalPosition = defaultCameraLocalPosition + new Vector3(pickupOffsetX, 0f, 0f);
        else
            targetCameraLocalPosition = defaultCameraLocalPosition;
    }
}