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

    [Header("Pickup Kamera")]
    public float pickupOffsetX = -0.5f;
    public float cameraShiftSpeed = 5f;

    public Transform groundCheck;
    public float groundDistance = 0.3f;
    public LayerMask groundMask;

    private float verticalRotation = 0f;
    private Camera playerCamera;
    private Rigidbody rb;
    private bool isGrounded;
    private Vector3 defaultCameraLocalPosition;
    private Vector3 targetCameraLocalPosition;

    void Start()
    {
        playerCamera = GetComponentInChildren<Camera>();
        rb = GetComponent<Rigidbody>();

        // Startposition der Kamera merken wie sie im Inspector eingestellt ist
        defaultCameraLocalPosition = playerCamera.transform.localPosition;
        targetCameraLocalPosition = defaultCameraLocalPosition;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleMouseLook();
        HandleJump();

        // Kamera smooth zum Ziel bewegen
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
        HandleMovement();
        CheckGround();
    }

    void CheckGround()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
    }

    void HandleJump()
    {
        if (!canMove) return;

        if (Keyboard.current.spaceKey.isPressed && isGrounded)
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