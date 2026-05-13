using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(AudioSource))]
public class DraggableBlock3D : MonoBehaviour
{
    [Header("Drag Settings")]
    public float dragHeight = 0.5f;
    public float minDistanceFromBall = 1.2f;

    [Header("Collision Check")]
    public LayerMask blockedLayers;

    [Header("Ball Reference")]
    public BallController3D ball;
    public Collider ballCollider;

    [Header("SFX")]
    public AudioSource audioSource;
    public AudioClip pickUpSFX;
    public AudioClip placeSFX;

    private Camera mainCamera;
    private Collider blockCollider;

    private bool isDragging = false;
    private Vector3 offset;

    private void Awake()
    {
        mainCamera = Camera.main;
        blockCollider = GetComponent<Collider>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
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

            if (CanPlaceAt(newPos) && !WouldOverlapObject(newPos))
                transform.position = newPos;
        }
    }

    private void StartDragging()
    {
        if (!TryGetMousePointOnGround(out Vector3 mousePoint))
            return;

        isDragging = true;
        offset = transform.position - mousePoint;

        IgnoreBallCollision(true);
        PlaySFX(pickUpSFX);
    }

    private void StopDragging()
    {
        if (!isDragging)
            return;

        isDragging = false;

        IgnoreBallCollision(false);
        PlaySFX(placeSFX);
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

        Vector3 closestPoint = blockCollider.ClosestPoint(ball.transform.position);

        Vector3 moveOffset = newPos - transform.position;
        closestPoint += moveOffset;

        Vector3 ballFlat = ball.transform.position;
        ballFlat.y = 0f;

        Vector3 closestFlat = closestPoint;
        closestFlat.y = 0f;

        float distance = Vector3.Distance(closestFlat, ballFlat);

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

    private bool WouldOverlapObject(Vector3 newPos)
    {
        Vector3 halfExtents = blockCollider.bounds.extents * 0.9f;

        Collider[] hits = Physics.OverlapBox(
            newPos,
            halfExtents,
            transform.rotation,
            blockedLayers,
            QueryTriggerInteraction.Ignore
        );

        foreach (Collider hit in hits)
        {
            if (hit == blockCollider)
                continue;

            return true;
        }

        return false;
    }

    private void PlaySFX(AudioClip clip)
    {
        if (clip == null || audioSource == null)
            return;

        audioSource.PlayOneShot(clip);
    }
}