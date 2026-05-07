using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
public class DraggableBlock3D : MonoBehaviour
{
    [Header("Drag Settings")]
    public float dragHeight = 0.5f;
    public float minDistanceFromBall = 1.2f;

    [Header("Ball Reference")]
    public BallController3D ball;
    public Collider ballCollider;

    private Camera mainCamera;
    private Collider blockCollider;

    private bool isDragging = false;
    private Vector3 offset;

    private void Awake()
    {
        mainCamera = Camera.main;
        blockCollider = GetComponent<Collider>();
    }

    private void Start()
    {
        if (ball != null && ballCollider == null)
            ballCollider = ball.GetComponent<Collider>();
    }

    private void Update()
    {
        if (Mouse.current == null || mainCamera == null)
            return;

        if (GameUIManager3D.Instance != null &&
            !GameUIManager3D.Instance.GameStarted)
            return;

        // Disable dragging once the ball is moving.
        if (ball != null && ball.IsBallMovingOrShot())
        {
            StopDragging();
            return;
        }

        if (Mouse.current.leftButton.wasPressedThisFrame && ClickedThisBlock())
        {
            StartDragging();
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            StopDragging();
        }

        if (isDragging && TryGetMousePointOnGround(out Vector3 mouseWorldPoint))
        {
            Vector3 newPos = mouseWorldPoint + offset;
            newPos.y = dragHeight;

            // Prevent placing directly on the ball.
            if (CanPlaceAt(newPos))
                transform.position = newPos;
        }
    }

    private void StartDragging()
    {
        if (!TryGetMousePointOnGround(out Vector3 mousePoint))
            return;

        isDragging = true;
        offset = transform.position - mousePoint;

        // Prevent the dragged block from pushing the ball.
        IgnoreBallCollision(true);
    }

    private void StopDragging()
    {
        if (!isDragging)
            return;

        isDragging = false;

        // Re-enable collision after placement.
        IgnoreBallCollision(false);
    }

    private void IgnoreBallCollision(bool ignore)
    {
        if (blockCollider != null && ballCollider != null)
            Physics.IgnoreCollision(blockCollider, ballCollider, ignore);
    }

    private bool CanPlaceAt(Vector3 newPos)
    {
        if (ball == null)
            return true;

        Vector3 blockFlat = newPos;
        blockFlat.y = 0f;

        Vector3 ballFlat = ball.transform.position;
        ballFlat.y = 0f;

        float distance = Vector3.Distance(blockFlat, ballFlat);

        return distance >= minDistanceFromBall;
    }

    private bool ClickedThisBlock()
    {
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit))
            return hit.collider == blockCollider;

        return false;
    }

    private bool TryGetMousePointOnGround(out Vector3 point)
    {
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        if (groundPlane.Raycast(ray, out float enter))
        {
            point = ray.GetPoint(enter);
            return true;
        }

        point = Vector3.zero;
        return false;
    }
}