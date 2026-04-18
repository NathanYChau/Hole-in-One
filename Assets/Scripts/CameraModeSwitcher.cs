using UnityEngine;
using UnityEngine.InputSystem;

public class CameraModeSwitcher : MonoBehaviour
{
    [Header("Target")]
    public Transform target; // The ball the camera will follow

    [Header("Mode Toggle")]
    public Key toggleKey = Key.F; // Press F to switch modes
    public bool followMode = false; // false = freecam, true = follow ball

    [Header("Free Camera Movement")]
    public float moveSpeed = 10f;     // WASD movement speed
    public float verticalSpeed = 8f;  // Q/E up/down speed

    [Header("Free Camera Look")]
    public float lookSensitivity = 0.15f; // Mouse sensitivity
    public float minPitch = -30f;         // Limit looking down
    public float maxPitch = 80f;          // Limit looking up

    [Header("Follow Camera")]
    public Vector3 followOffset = new Vector3(0f, 8f, -8f); // Offset from ball
    public float followSpeed = 6f; // How smoothly camera follows
    public bool lookAtTarget = true; // Should camera face the ball

    // Internal rotation tracking
    private float yaw;   // left/right rotation
    private float pitch; // up/down rotation

    private void Start()
    {
        // Initialize yaw/pitch based on current camera rotation
        Vector3 startRotation = transform.eulerAngles;
        yaw = startRotation.y;
        pitch = startRotation.x;
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        // Toggle between freecam and follow mode
        if (Keyboard.current[toggleKey].wasPressedThisFrame)
        {
            followMode = !followMode;
        }

        // Run correct mode
        if (followMode)
        {
            FollowBall();
        }
        else
        {
            FreeLook();
            FreeMove();
        }
    }

    private void FreeMove()
    {
        Vector3 move = Vector3.zero;

        // Move in full camera direction (including vertical)
        if (Keyboard.current.wKey.isPressed)
            move += transform.forward;

        if (Keyboard.current.sKey.isPressed)
            move -= transform.forward;

        if (Keyboard.current.aKey.isPressed)
            move -= transform.right;

        if (Keyboard.current.dKey.isPressed)
            move += transform.right;

        // Normalize so diagonal isn't faster
        move = move.normalized;

        // Apply movement
        transform.position += move * moveSpeed * Time.deltaTime;

        // Optional extra vertical controls (can keep or remove)
        if (Keyboard.current.qKey.isPressed)
            transform.position += Vector3.up * verticalSpeed * Time.deltaTime;

        if (Keyboard.current.eKey.isPressed)
            transform.position += Vector3.down * verticalSpeed * Time.deltaTime;
    }

    private void FreeLook()
    {
        if (Mouse.current == null)
            return;

        // Hold right mouse button to rotate camera
        if (Mouse.current.rightButton.isPressed)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();

            // Drag right -> camera turns right
            yaw += mouseDelta.x * lookSensitivity;

            // Drag up -> camera looks up
            pitch -= mouseDelta.y * lookSensitivity;

            // Clamp vertical rotation so you can't flip upside down
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

            // Apply rotation
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }
    }

    private void FollowBall()
    {
        if (target == null)
            return;

        // Desired position = ball position + offset
        Vector3 desiredPosition = target.position + followOffset;

        // Smoothly move camera toward that position
        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            followSpeed * Time.deltaTime
        );

        // Optionally rotate camera to always look at ball
        if (lookAtTarget)
        {
            transform.LookAt(target.position);
        }
    }
}