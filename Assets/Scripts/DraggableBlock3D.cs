using UnityEngine;
using UnityEngine.InputSystem;

// Require these components so the draggable block always has them
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(AudioSource))]
public class DraggableBlock3D : MonoBehaviour
{
    // -------------------------
    // DRAG SETTINGS
    // -------------------------

    [Header("Drag Settings")]

    // Height the block stays at while dragging
    public float dragHeight = 0.0f;

    // Minimum distance allowed between block and ball
    public float minDistanceFromBall = 1.2f;

    // -------------------------
    // COLLISION CHECK
    // -------------------------

    [Header("Collision Check")]

    // Layers checked to prevent overlap placement
    public LayerMask blockedLayers;

    // -------------------------
    // MOVEMENT BOUNDARIES
    // -------------------------

    [Header("Drag Boundaries")]

    // Clamp movement horizontally
    public float minX = -5f;
    public float maxX = 5f;

    // Clamp movement vertically on map plane
    public float minZ = -5f;
    public float maxZ = 5f;

    // -------------------------
    // BALL REFERENCES
    // -------------------------

    [Header("Ball Reference")]

    // Reference to golf ball script
    public BallController3D ball;

    // Ball collider used for collision ignoring
    public Collider ballCollider;

    // -------------------------
    // SOUND EFFECTS
    // -------------------------

    [Header("SFX")]

    public AudioSource audioSource;

    // Sound played when block is picked up
    public AudioClip pickUpSFX;

    // Sound played when block is dropped
    public AudioClip placeSFX;

    // -------------------------
    // INTERNAL REFERENCES
    // -------------------------

    private Camera mainCamera;
    private Collider blockCollider;

    // Is player currently dragging this block?
    private bool isDragging = false;

    // Offset between mouse position and object center
    private Vector3 offset;

    // -------------------------
    // UNITY AWAKE
    // -------------------------

    private void Awake()
    {
        // Cache references
        mainCamera = Camera.main;
        blockCollider = GetComponent<Collider>();

        // Auto-find AudioSource if missing
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    // -------------------------
    // UNITY START
    // -------------------------

    private void Start()
    {
        // Automatically grab collider from ball object
        if (ball != null && ballCollider == null)
            ballCollider = ball.GetComponent<Collider>();
    }

    // -------------------------
    // UNITY UPDATE
    // -------------------------

    private void Update()
    {
        // Safety checks
        if (Mouse.current == null || mainCamera == null)
            return;

        // Prevent dragging before game starts
        if (GameUIManager3D.Instance != null &&
            !GameUIManager3D.Instance.GameStarted)
            return;

        // Stop dragging while ball is moving
        if (ball != null && ball.IsBallMovingOrShot())
        {
            StopDragging();
            return;
        }

        // Start dragging when block clicked
        if (Mouse.current.leftButton.wasPressedThisFrame && ClickedThisBlock())
        {
            StartDragging();
        }

        // Stop dragging when mouse released
        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            StopDragging();
        }

        // -------------------------
        // DRAG MOVEMENT
        // -------------------------

        if (isDragging && TryGetMousePointOnGround(out Vector3 mouseWorldPoint))
        {
            // Move object based on mouse position + original offset
            Vector3 newPos = mouseWorldPoint + offset;

            // Keep object at fixed height
            newPos.y = dragHeight;

            // Clamp movement boundaries
            newPos.x = Mathf.Clamp(newPos.x, minX, maxX);
            newPos.z = Mathf.Clamp(newPos.z, minZ, maxZ);

            // Only move if valid placement
            if (CanPlaceAt(newPos) && !WouldOverlapObject(newPos))
                transform.position = newPos;
        }
    }

    // -------------------------
    // START DRAGGING
    // -------------------------

    private void StartDragging()
    {
        // Get mouse position on world plane
        if (!TryGetMousePointOnGround(out Vector3 mousePoint))
            return;

        isDragging = true;

        // Store offset so object doesn't snap to mouse center
        offset = transform.position - mousePoint;

        // Ignore collisions between ball and block while dragging
        IgnoreBallCollision(true);

        PlaySFX(pickUpSFX);
    }

    // -------------------------
    // STOP DRAGGING
    // -------------------------

    private void StopDragging()
    {
        if (!isDragging)
            return;

        isDragging = false;

        // Re-enable collision with ball
        IgnoreBallCollision(false);

        PlaySFX(placeSFX);
    }

    // -------------------------
    // IGNORE BALL COLLISION
    // -------------------------

    private void IgnoreBallCollision(bool ignore)
    {
        if (blockCollider != null && ballCollider != null)
            Physics.IgnoreCollision(blockCollider, ballCollider, ignore);
    }

    // -------------------------
    // VALIDATE PLACEMENT DISTANCE
    // -------------------------

    private bool CanPlaceAt(Vector3 newPos)
    {
        // No ball reference = always valid
        if (ball == null)
            return true;

        // Closest point from block collider to ball
        Vector3 closestPoint = blockCollider.ClosestPoint(ball.transform.position);

        // Simulate moved position
        Vector3 moveOffset = newPos - transform.position;
        closestPoint += moveOffset;

        // Flatten both positions onto XZ plane
        Vector3 ballFlat = ball.transform.position;
        ballFlat.y = 0f;

        Vector3 closestFlat = closestPoint;
        closestFlat.y = 0f;

        // Measure distance
        float distance = Vector3.Distance(closestFlat, ballFlat);

        // Must stay outside minimum distance
        return distance >= minDistanceFromBall;
    }

    // -------------------------
    // CLICK DETECTION
    // -------------------------

    private bool ClickedThisBlock()
    {
        // Create ray from mouse position
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        // Check if ray hit THIS block
        if (Physics.Raycast(ray, out RaycastHit hit))
            return hit.collider == blockCollider;

        return false;
    }

    // -------------------------
    // GET MOUSE WORLD POSITION
    // -------------------------

    private bool TryGetMousePointOnGround(out Vector3 point)
    {
        // Create ray from mouse
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        // Infinite horizontal plane
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        // Check if ray intersects plane
        if (groundPlane.Raycast(ray, out float enter))
        {
            point = ray.GetPoint(enter);

            return true;
        }

        point = Vector3.zero;

        return false;
    }

    // -------------------------
    // OVERLAP CHECK
    // -------------------------

    private bool WouldOverlapObject(Vector3 newPos)
    {
        // Slightly shrink overlap box to reduce edge false positives
        Vector3 halfExtents = blockCollider.bounds.extents * 0.9f;

        // Check overlapping colliders
        Collider[] hits = Physics.OverlapBox(
            newPos,
            halfExtents,
            transform.rotation,
            blockedLayers,
            QueryTriggerInteraction.Ignore
        );

        foreach (Collider hit in hits)
        {
            // Ignore self overlap
            if (hit == blockCollider)
                continue;

            // Found overlap
            return true;
        }

        return false;
    }

    // -------------------------
    // PLAY AUDIO
    // -------------------------

    private void PlaySFX(AudioClip clip)
    {
        if (clip == null || audioSource == null)
            return;

        audioSource.PlayOneShot(clip);
    }
}