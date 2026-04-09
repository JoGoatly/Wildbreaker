using UnityEngine;
using UnityEngine.InputSystem;

public class Player2Controller : MonoBehaviour
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

        defaultCameraLocalPosition = playerCamera.transform.localPosition;
        targetCameraLocalPosition = defaultCameraLocalPosition;
    }

    void Update()
    {
        HandleMouseLook();
        HandleJump();

        playerCamera.transform.localPosition = Vector3.Lerp(
            playerCamera.transform.localPosition,
            targetCameraLocalPosition,
            Time.deltaTime * cameraShiftSpeed
        );
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

        if (Keyboard.current.numpad0Key.wasPressedThisFrame && isGrounded)
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
            Keyboard.current.lKey.isPressed ? 1f : Keyboard.current.jKey.isPressed ? -1f : 0f,
            Keyboard.current.iKey.isPressed ? 1f : Keyboard.current.kKey.isPressed ? -1f : 0f
        );

        bool isSprinting = Keyboard.current.rightShiftKey.isPressed;
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

        float arrowX = 0f;
        float arrowY = 0f;

        if (Keyboard.current.leftArrowKey.isPressed) arrowX = -1f;
        if (Keyboard.current.rightArrowKey.isPressed) arrowX = 1f;
        if (Keyboard.current.upArrowKey.isPressed) arrowY = 1f;
        if (Keyboard.current.downArrowKey.isPressed) arrowY = -1f;

        float sensitivity = mouseSensitivity * 100f * Time.deltaTime;

        transform.Rotate(0f, arrowX * sensitivity, 0f);

        verticalRotation -= arrowY * sensitivity;
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