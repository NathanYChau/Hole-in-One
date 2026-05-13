using UnityEngine;
using UnityEngine.InputSystem;

// Handles switching between follow camera mode and freecam mode
public class CameraModeSwitcher : MonoBehaviour
{
    // -------------------------
    // TARGET SETTINGS
    // -------------------------

    [Header("Target")]

    // The object the camera follows (usually the ball)
    public Transform target;

    // -------------------------
    // CAMERA MODE TOGGLE
    // -------------------------

    [Header("Mode Toggle")]

    // Press this key to swap between follow mode and freecam
    public Key toggleKey = Key.F;

    // true = follow ball
    // false = freecam
    public bool followMode = true;

    // -------------------------
    // FREE CAMERA MOVEMENT
    // -------------------------

    [Header("Free Camera Movement")]

    // WASD movement speed
    public float moveSpeed = 10f;

    // Q/E vertical movement speed
    public float verticalSpeed = 8f;

    // -------------------------
    // FREE CAMERA LOOK
    // -------------------------

    [Header("Free Camera Look")]

    // Mouse sensitivity when rotating camera
    public float lookSensitivity = 0.15f;

    // Lowest vertical angle allowed
    public float minPitch = -30f;

    // Highest vertical angle allowed
    public float maxPitch = 80f;

    // -------------------------
    // FOLLOW CAMERA SETTINGS
    // -------------------------

    [Header("Follow Camera")]

    // Camera offset from the target
    // Example:
    // (0, 8, -8) = above and behind the ball
    public Vector3 followOffset = new Vector3(0f, 8f, -8f);

    // How smoothly the camera follows
    public float followSpeed = 6f;

    // Should the camera constantly face the target?
    public bool lookAtTarget = true;

    // -------------------------
    // INTERNAL ROTATION TRACKING
    // -------------------------

    // Left/right camera rotation
    private float yaw;

    // Up/down camera rotation
    private float pitch;

    // -------------------------
    // UNITY START
    // -------------------------

    private void Start()
    {
        // Store initial camera rotation
        // so freecam starts facing the correct direction
        Vector3 startRotation = transform.eulerAngles;

        yaw = startRotation.y;
        pitch = startRotation.x;
    }

    // -------------------------
    // UNITY UPDATE
    // -------------------------

    private void Update()
    {
        // Safety check
        if (Keyboard.current == null)
            return;

        // Disable camera controls before game starts
        if (GameUIManager3D.Instance != null &&
            !GameUIManager3D.Instance.GameStarted)
            return;

        // Toggle between follow mode and freecam
        if (Keyboard.current[toggleKey].wasPressedThisFrame)
        {
            followMode = !followMode;
        }

        // Run correct camera mode
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

    // -------------------------
    // FREECAM MOVEMENT
    // -------------------------

    private void FreeMove()
    {
        Vector3 move = Vector3.zero;

        // Move forward relative to camera direction
        if (Keyboard.current.wKey.isPressed)
            move += transform.forward;

        // Move backward
        if (Keyboard.current.sKey.isPressed)
            move -= transform.forward;

        // Move left
        if (Keyboard.current.aKey.isPressed)
            move -= transform.right;

        // Move right
        if (Keyboard.current.dKey.isPressed)
            move += transform.right;

        // Prevent diagonal movement from being faster
        move = move.normalized;

        // Apply movement
        transform.position += move * moveSpeed * Time.deltaTime;

        // Move upward
        if (Keyboard.current.qKey.isPressed)
            transform.position += Vector3.up * verticalSpeed * Time.deltaTime;

        // Move downward
        if (Keyboard.current.eKey.isPressed)
            transform.position += Vector3.down * verticalSpeed * Time.deltaTime;
    }

    // -------------------------
    // FREECAM LOOK ROTATION
    // -------------------------

    private void FreeLook()
    {
        if (Mouse.current == null)
            return;

        // Only rotate camera while right mouse button is held
        if (Mouse.current.rightButton.isPressed)
        {
            // Mouse movement since last frame
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();

            // Horizontal turning
            yaw += mouseDelta.x * lookSensitivity;

            // Vertical looking
            pitch -= mouseDelta.y * lookSensitivity;

            // Prevent flipping upside down
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

            // Apply rotation
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }
    }

    // -------------------------
    // FOLLOW CAMERA MODE
    // -------------------------

    private void FollowBall()
    {
        // Safety check
        if (target == null)
            return;

        // Desired position = target position + offset
        Vector3 desiredPosition = target.position + followOffset;

        // Smoothly move camera toward target
        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            followSpeed * Time.deltaTime
        );

        // Make camera face the target
        if (lookAtTarget)
        {
            transform.LookAt(target.position);
        }
    }
}