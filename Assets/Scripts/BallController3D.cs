using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(AudioSource))]
public class BallController3D : MonoBehaviour
{
    [Header("Shot Settings")]
    public float maxDragDistance = 3f;
    public float shotForceMultiplier = 8f;
    public float minSpeedToBeMoving = 0.15f;

    [Header("Projected Path")]
    public LineRenderer aimLine;
    public int maxBounces = 3;
    public float pathLengthMultiplier = 1.5f;
    public float floorLineHeight = 0.03f;

    [Header("Selection Outline")]
    public GameObject selectionOutline;
    public float outlineYOffset = 0.03f;
    public Vector3 outlineBaseScale = new Vector3(0.6f, 0.6f, 0.6f);
    public float outlinePulseSpeed = 6f;
    public float outlinePulseAmount = 0.03f;

    [Header("SFX")]
    public AudioSource audioSource;
    public AudioClip selectSFX;
    public AudioClip cancelSFX;
    public AudioClip shootSFX;
    public AudioClip bounceSFX;
    public float minBounceSpeedForSFX = 1.5f;

    private Rigidbody rb;
    private Collider ballCollider;
    private Camera mainCamera;

    private bool isSelected = false;
    private bool isDragging = false;

    private Vector3 dragStartPoint;
    private Vector3 dragCurrentPoint;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        ballCollider = GetComponent<Collider>();
        mainCamera = Camera.main;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        if (aimLine != null)
        {
            aimLine.useWorldSpace = true;
            aimLine.positionCount = 0;
            aimLine.enabled = false;
            aimLine.alignment = LineAlignment.View;
        }

        if (selectionOutline != null)
            selectionOutline.SetActive(false);
    }

    private void Update()
    {
        if (Mouse.current == null || mainCamera == null)
            return;

        // Block all ball input before Start is pressed
        if (GameUIManager3D.Instance != null &&
            !GameUIManager3D.Instance.GameStarted)
        {
            ClearSelection();
            return;
        }

        UpdateSelectionOutlinePosition();

        if (IsMoving())
        {
            ClearSelection();
            return;
        }

        if (!TryGetMousePointOnGround(out Vector3 mouseWorldPoint))
            return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (DidClickBall())
            {
                if (!isSelected)
                {
                    SelectBall();
                    return;
                }

                isDragging = true;
                dragStartPoint = mouseWorldPoint;
                dragCurrentPoint = mouseWorldPoint;
            }
            else
            {
                ClearSelection();
            }
        }

        if (isDragging)
        {
            dragCurrentPoint = mouseWorldPoint;
            UpdateProjectedPath();

            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                CancelDrag();
                return;
            }
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame && isDragging)
        {
            isDragging = false;
            HideAimLine();

            ShootBall();
            ClearSelection();
        }
    }

    private void SelectBall()
    {
        isSelected = true;
        PlaySFX(selectSFX);

        if (selectionOutline != null)
        {
            selectionOutline.SetActive(true);
            UpdateSelectionOutlinePosition();
        }
    }

    private void ClearSelection()
    {
        isSelected = false;
        isDragging = false;

        HideAimLine();

        if (selectionOutline != null)
            selectionOutline.SetActive(false);
    }

    private void CancelDrag()
    {
        isDragging = false;

        PlaySFX(cancelSFX);

        ClearSelection();
    }

    private void UpdateSelectionOutlinePosition()
    {
        if (selectionOutline == null || !selectionOutline.activeSelf)
            return;

        Vector3 pos = transform.position;
        pos.y = outlineYOffset;

        selectionOutline.transform.position = pos;
        selectionOutline.transform.rotation = Quaternion.identity;

        float pulse =
            1f + Mathf.Sin(Time.time * outlinePulseSpeed) * outlinePulseAmount;

        selectionOutline.transform.localScale =
            new Vector3(
                outlineBaseScale.x * pulse,
                outlineBaseScale.y,
                outlineBaseScale.z * pulse
            );
    }

    private bool TryGetMousePointOnGround(out Vector3 worldPoint)
    {
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        if (groundPlane.Raycast(ray, out float enter))
        {
            worldPoint = ray.GetPoint(enter);
            return true;
        }

        worldPoint = Vector3.zero;
        return false;
    }

    private Vector3 GetDragVector()
    {
        Vector3 dragVector = dragCurrentPoint - dragStartPoint;
        dragVector.y = 0f;

        return Vector3.ClampMagnitude(dragVector, maxDragDistance);
    }

    private void UpdateProjectedPath()
    {
        if (aimLine == null)
            return;

        Vector3 dragVector = GetDragVector();

        if (dragVector.sqrMagnitude < 0.0001f)
        {
            HideAimLine();
            return;
        }

        Vector3 direction = -dragVector.normalized;

        float shotPower = dragVector.magnitude / maxDragDistance;

        float remainingLength =
            dragVector.magnitude *
            shotForceMultiplier *
            pathLengthMultiplier *
            Mathf.Lerp(0.45f, 1f, shotPower);

        Vector3 currentPoint = transform.position;
        currentPoint.y = floorLineHeight;

        aimLine.enabled = true;
        aimLine.positionCount = 1;
        aimLine.SetPosition(0, currentPoint);

        int pointIndex = 1;

        for (int bounce = 0; bounce <= maxBounces; bounce++)
        {
            Vector3 rayStart = currentPoint + Vector3.up * 0.25f;

            if (Physics.Raycast(rayStart, direction, out RaycastHit hit, remainingLength))
            {
                if (hit.collider == ballCollider)
                    break;

                Vector3 hitPoint = hit.point;
                hitPoint.y = floorLineHeight;

                aimLine.positionCount = pointIndex + 1;
                aimLine.SetPosition(pointIndex, hitPoint);
                pointIndex++;

                remainingLength -= Vector3.Distance(currentPoint, hitPoint);

                direction = Vector3.Reflect(direction, hit.normal);
                direction.y = 0f;
                direction.Normalize();

                currentPoint = hitPoint + direction * 0.05f;
                currentPoint.y = floorLineHeight;
            }
            else
            {
                Vector3 endPoint = currentPoint + direction * remainingLength;
                endPoint.y = floorLineHeight;

                aimLine.positionCount = pointIndex + 1;
                aimLine.SetPosition(pointIndex, endPoint);
                break;
            }
        }
    }

    private void HideAimLine()
    {
        if (aimLine == null)
            return;

        aimLine.enabled = false;
        aimLine.positionCount = 0;
    }

    private void ShootBall()
    {
        Vector3 dragVector = GetDragVector();

        if (dragVector.sqrMagnitude < 0.0001f)
            return;

        Vector3 force = -dragVector * shotForceMultiplier;
        force.y = 0f;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        PlaySFX(shootSFX);

        rb.AddForce(force, ForceMode.Impulse);
    }

    private bool IsMoving()
    {
        Vector3 horizontalVelocity = rb.linearVelocity;
        horizontalVelocity.y = 0f;

        return horizontalVelocity.magnitude > minSpeedToBeMoving;
    }

    private bool DidClickBall()
    {
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit))
            return hit.collider == ballCollider;

        return false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (rb == null)
            return;

        if (collision.relativeVelocity.magnitude >= minBounceSpeedForSFX)
            PlaySFX(bounceSFX);
    }

    private void PlaySFX(AudioClip clip)
    {
        if (clip == null)
            return;

        if (audioSource != null)
            audioSource.PlayOneShot(clip);
    }

    public bool IsBallMovingOrShot()
    {
        return IsMoving();
    }

}