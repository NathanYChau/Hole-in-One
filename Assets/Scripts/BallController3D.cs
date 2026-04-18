using UnityEngine;
using UnityEngine.InputSystem;

// Ensure the object always has these components
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class BallController3D : MonoBehaviour
{
    [Header("Shot Settings")]
    public float maxDragDistance = 3f;        // Max distance player can drag (limits shot power)
    public float shotForceMultiplier = 8f;    // Multiplies drag into actual force
    public float minSpeedToBeMoving = 0.15f;  // Threshold to consider ball "still moving"

    [Header("Aim Line")]
    public LineRenderer aimLine;              // Visual line showing shot direction/power

    // Internal references
    private Rigidbody rb;
    private Collider ballCollider;
    private Camera mainCamera;

    // Drag state
    private bool isDragging = false;
    private Vector3 dragStartPoint;
    private Vector3 dragCurrentPoint;

    private void Awake()
    {
        // Cache components for performance
        rb = GetComponent<Rigidbody>();
        ballCollider = GetComponent<Collider>();
        mainCamera = Camera.main;
    }

    private void Start()
    {
        // Initialize the aim line
        if (aimLine != null)
        {
            aimLine.positionCount = 2; // start + end point
            aimLine.enabled = false;   // hidden until dragging
        }
    }

    private void Update()
    {
        // Make sure input and camera exist
        if (Mouse.current == null || mainCamera == null)
            return;

        // Prevent shooting while ball is still moving
        if (IsMoving())
            return;

        // Convert mouse position to a point on the ground plane
        if (!TryGetMousePointOnGround(out Vector3 mouseWorldPoint))
            return;

        // Start dragging only if we clicked the ball
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (DidClickBall())
            {
                isDragging = true;
                dragStartPoint = mouseWorldPoint;
                dragCurrentPoint = mouseWorldPoint;
            }
        }

        // While dragging, update direction and aim line
        if (isDragging)
        {
            dragCurrentPoint = mouseWorldPoint;
            UpdateAimLine();
        }

        // On release, shoot the ball
        if (Mouse.current.leftButton.wasReleasedThisFrame && isDragging)
        {
            isDragging = false;

            if (aimLine != null)
                aimLine.enabled = false;

            ShootBall();
        }
    }

    // Converts mouse position into a world point on a flat ground plane
    private bool TryGetMousePointOnGround(out Vector3 worldPoint)
    {
        // Create a ray from the camera through the mouse
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        // Define a flat horizontal plane at y = 0
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        // Check if ray hits the plane
        if (groundPlane.Raycast(ray, out float enter))
        {
            worldPoint = ray.GetPoint(enter); // Get exact hit point
            return true;
        }

        worldPoint = Vector3.zero;
        return false;
    }

    // (Not used anymore, but checks if mouse is close to ball on ground)
    private bool IsMouseNearBall(Vector3 mouseWorldPoint)
    {
        Vector3 flattenedBallPos = transform.position;
        flattenedBallPos.y = 0f;

        Vector3 flattenedMousePos = mouseWorldPoint;
        flattenedMousePos.y = 0f;

        return Vector3.Distance(flattenedBallPos, flattenedMousePos) < 1.0f;
    }

    // Updates the visual aim line while dragging
    private void UpdateAimLine()
    {
        if (aimLine == null)
            return;

        // Calculate drag direction
        Vector3 dragVector = dragCurrentPoint - dragStartPoint;

        // Remove vertical component so shot stays horizontal
        dragVector.y = 0f;

        // Limit max drag distance (caps power)
        dragVector = Vector3.ClampMagnitude(dragVector, maxDragDistance);

        // Line starts at ball
        Vector3 start = transform.position;

        // Line ends in opposite direction (like pulling back a slingshot)
        Vector3 end = start - dragVector;

        // Enable and update line
        aimLine.enabled = true;
        aimLine.SetPosition(0, start);
        aimLine.SetPosition(1, end);
    }

    // Applies force to the ball based on drag
    private void ShootBall()
    {
        // Calculate drag direction
        Vector3 dragVector = dragCurrentPoint - dragStartPoint;

        // Keep movement horizontal only
        dragVector.y = 0f;

        // Limit drag distance
        dragVector = Vector3.ClampMagnitude(dragVector, maxDragDistance);

        // Ignore tiny drags
        if (dragVector.sqrMagnitude < 0.0001f)
            return;

        // Reverse direction (pull back -> shoot forward)
        Vector3 force = -dragVector * shotForceMultiplier;

        // Extra safety: no vertical force
        force.y = 0f;

        // Reset motion before applying new force
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Apply impulse force
        rb.AddForce(force, ForceMode.Impulse);
    }

    // Checks if ball is still moving (horizontal speed only)
    private bool IsMoving()
    {
        Vector3 horizontalVelocity = rb.linearVelocity;
        horizontalVelocity.y = 0f;

        return horizontalVelocity.magnitude > minSpeedToBeMoving;
    }

    // Detects if the mouse click hit the ball
    private bool DidClickBall()
    {
        // Raycast from camera through mouse position
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // Return true only if we hit THIS ball's collider
            return hit.collider == ballCollider;
        }

        return false;
    }
}