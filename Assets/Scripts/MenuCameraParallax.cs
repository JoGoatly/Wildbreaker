using UnityEngine;
using UnityEngine.InputSystem;

public class MenuCameraParallax : MonoBehaviour
{
    [Header("Bewegungsstärke")]
    public float maxAngleX = 5f;
    public float maxAngleY = 8f;

    [Header("Geschwindigkeit")]
    public float smoothSpeed = 3f;

    private Quaternion targetRotation;
    private Quaternion initialRotation;

    void Start()
    {
        initialRotation = transform.rotation;
        targetRotation = initialRotation;
    }

    void Update()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();

        float mouseX = (mousePos.x / Screen.width) * 2f - 1f;
        float mouseY = (mousePos.y / Screen.height) * 2f - 1f;

        float rotX = -mouseY * maxAngleX;
        float rotY = mouseX * maxAngleY;

        targetRotation = initialRotation * Quaternion.Euler(rotX, rotY, 0f);

        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            targetRotation,
            Time.deltaTime * smoothSpeed
        );
    }
}