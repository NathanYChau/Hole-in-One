using UnityEngine;
using UnityEngine.InputSystem;

// Require these components so the script cannot run without them
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(AudioSource))]
public class BallController3D : MonoBehaviour
{
    // -------------------------
    // SHOT SETTINGS
    // -------------------------

    [Header("Shot Settings")]

    // Maximum distance the player can drag when aiming
    public float maxDragDistance = 3f;

    // Multiplier applied to shot force
    public float shotForceMultiplier = 8f;

    // Speed threshold before the ball counts as "moving"
    public float minSpeedToBeMoving = 0.15f;

    // -------------------------
    // ONE SHOT RESET
    // -------------------------

    [Header("One Shot Reset")]

    // Automatically restart level after one shot finishes
    public bool resetAfterOneShot = true;

    // Delay before level reset begins
    public float stoppedResetDelay = 0.75f;

    // How long the ball must stay still before reset triggers
    public float stoppedConfirmTime = 1.0f;

    // Internal tracking
    private bool shotTaken = false;
    private bool resetStarted = false;
    private float stoppedTimer = 0f;

    // -------------------------
    // AIM / PROJECTED PATH
    // -------------------------

    [Header("Projected Path")]

    // Line renderer used for aim prediction
    public LineRenderer aimLine;

    // Number of wall bounces shown in prediction
    public int maxBounces = 1;

    // Scales projected line length
    public float pathLengthMultiplier = 0.06f;

    // Height offset above the ball for the aim line
    public float lineHeightOffset = 0.05f;

    // Maximum possible projected line length
    public float maxAimLineLength = 6f;

    // -------------------------
    // SELECTION OUTLINE
    // -------------------------

    [Header("Selection Outline")]

    // Glowing/pulsing outline object
    public GameObject selectionOutline;

    // Vertical offset for outline positioning
    public float outlineYOffset = 0.03f;

    // Base scale of outline before pulsing
    public Vector3 outlineBaseScale = new Vector3(0.6f, 0.6f, 0.6f);

    // Pulse animation speed
    public float outlinePulseSpeed = 6f;

    // Pulse size intensity
    public float outlinePulseAmount = 0.03f;

    // -------------------------
    // SOUND EFFECTS
    // -------------------------

    [Header("SFX")]

    public AudioSource audioSource;

    // Plays when ball is selected
    public AudioClip selectSFX;

    // Plays when drag is canceled
    public AudioClip cancelSFX;

    // Plays when shooting
    public AudioClip shootSFX;

    // Plays on bounce
    public AudioClip bounceSFX;

    // Minimum collision speed required for bounce SFX
    public float minBounceSpeedForSFX = 1.5f;

    // -------------------------
    // INTERNAL REFERENCES
    // -------------------------

    private Rigidbody rb;
    private Collider ballCollider;
    private Camera mainCamera;

    // -------------------------
    // INPUT / STATE TRACKING
    // -------------------------

    private bool isSelected = false;
    private bool isDragging = false;

    // Mouse drag positions
    private Vector3 dragStartPoint;
    private Vector3 dragCurrentPoint;

    // -------------------------
    // UNITY FUNCTIONS
    // -------------------------

    private void Awake()
    {
        // Cache references
        rb = GetComponent<Rigidbody>();
        ballCollider = GetComponent<Collider>();
        mainCamera = Camera.main;

        // Auto-find audio source if missing
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        // Configure line renderer
        if (aimLine != null)
        {
            aimLine.useWorldSpace = true;
            aimLine.positionCount = 0;
            aimLine.enabled = false;

            // Makes line always face camera
            aimLine.alignment = LineAlignment.View;

            // Create fade-out gradient
            Gradient gradient = new Gradient();

            gradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                }
            );

            aimLine.colorGradient = gradient;
        }

        // Hide selection outline at start
        if (selectionOutline != null)
            selectionOutline.SetActive(false);
    }

    private void Update()
    {
        // Safety checks
        if (Mouse.current == null || mainCamera == null)
            return;

        // Prevent input before game starts
        if (GameUIManager3D.Instance != null &&
            !GameUIManager3D.Instance.GameStarted)
        {
            ClearSelection();
            return;
        }

        // -------------------------
        // AUTO RESET LOGIC
        // -------------------------

        if (resetAfterOneShot && shotTaken && !resetStarted)
        {
            if (IsMoving())
            {
                // Reset timer if ball moves again
                stoppedTimer = 0f;
            }
            else
            {
                stoppedTimer += Time.deltaTime;

                // Begin reset after staying still long enough
                if (stoppedTimer >= stoppedConfirmTime)
                {
                    StartCoroutine(ResetAfterDelay());
                    return;
                }
            }
        }

        // Animate selection glow
        UpdateSelectionOutlinePosition();

        // Disable interaction while moving
        if (IsMoving())
        {
            ClearSelection();
            return;
        }

        // Get mouse position on world plane
        if (!TryGetMousePointOnGround(out Vector3 mouseWorldPoint))
            return;

        // -------------------------
        // LEFT CLICK INPUT
        // -------------------------

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            // Clicked the ball
            if (DidClickBall())
            {
                // First click selects ball
                if (!isSelected)
                {
                    SelectBall();
                    return;
                }

                // Second click begins drag aiming
                isDragging = true;

                dragStartPoint = mouseWorldPoint;
                dragCurrentPoint = mouseWorldPoint;
            }
            else
            {
                // Clicking elsewhere clears selection
                ClearSelection();
            }
        }

        // -------------------------
        // DRAGGING / AIMING
        // -------------------------

        if (isDragging)
        {
            dragCurrentPoint = mouseWorldPoint;

            // Update projected aim path
            UpdateProjectedPath();

            // Right click cancels shot
            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                CancelDrag();
                return;
            }
        }

        // -------------------------
        // RELEASE TO SHOOT
        // -------------------------

        if (Mouse.current.leftButton.wasReleasedThisFrame && isDragging)
        {
            isDragging = false;

            HideAimLine();

            ShootBall();

            ClearSelection();
        }
    }

    // -------------------------
    // BALL SELECTION
    // -------------------------

    private void SelectBall()
    {
        isSelected = true;

        PlaySFX(selectSFX);

        // Enable glow outline
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

        // Disable glow outline
        if (selectionOutline != null)
            selectionOutline.SetActive(false);
    }

    private void CancelDrag()
    {
        isDragging = false;

        PlaySFX(cancelSFX);

        ClearSelection();
    }

    // -------------------------
    // OUTLINE PULSE ANIMATION
    // -------------------------

    private void UpdateSelectionOutlinePosition()
    {
        if (selectionOutline == null || !selectionOutline.activeSelf)
            return;

        // Position outline on ball
        Vector3 pos = transform.position;

        pos.y = transform.position.y + outlineYOffset;

        selectionOutline.transform.position = pos;

        // Keep upright
        selectionOutline.transform.rotation = Quaternion.identity;

        // Pulse scale animation
        float pulse =
            1f + Mathf.Sin(Time.time * outlinePulseSpeed) * outlinePulseAmount;

        selectionOutline.transform.localScale =
            new Vector3(
                outlineBaseScale.x * pulse,
                outlineBaseScale.y,
                outlineBaseScale.z * pulse
            );
    }

    // -------------------------
    // MOUSE WORLD POSITION
    // -------------------------

    private bool TryGetMousePointOnGround(out Vector3 worldPoint)
    {
        // Convert screen mouse position into world ray
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        // Infinite horizontal plane
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        if (groundPlane.Raycast(ray, out float enter))
        {
            worldPoint = ray.GetPoint(enter);

            return true;
        }

        worldPoint = Vector3.zero;

        return false;
    }

    // -------------------------
    // AIM DRAG VECTOR
    // -------------------------

    private Vector3 GetDragVector()
    {
        Vector3 dragVector = dragCurrentPoint - dragStartPoint;

        // Ignore vertical movement
        dragVector.y = 0f;

        // Clamp max power
        return Vector3.ClampMagnitude(dragVector, maxDragDistance);
    }

    // -------------------------
    // PROJECTED PATH LINE
    // -------------------------

    private void UpdateProjectedPath()
    {
        if (aimLine == null)
            return;

        Vector3 dragVector = GetDragVector();

        // Hide if drag too small
        if (dragVector.sqrMagnitude < 0.0001f)
        {
            HideAimLine();
            return;
        }

        // Shot direction is opposite drag direction
        Vector3 direction = -dragVector.normalized;

        // Normalize shot power
        float shotPower = dragVector.magnitude / maxDragDistance;

        // Compute projected line length
        float remainingLength = Mathf.Min(
            dragVector.magnitude *
            shotForceMultiplier *
            pathLengthMultiplier *
            Mathf.Lerp(0.45f, 1f, shotPower),
            maxAimLineLength
        );

        // Keep line slightly above ball height
        float aimLineY = transform.position.y + lineHeightOffset;

        Vector3 currentPoint = transform.position;

        currentPoint.y = aimLineY;

        aimLine.enabled = true;

        aimLine.positionCount = 1;

        aimLine.SetPosition(0, currentPoint);

        int pointIndex = 1;

        // Bounce simulation loop
        for (int bounce = 0; bounce <= maxBounces; bounce++)
        {
            Vector3 rayStart = currentPoint + Vector3.up * 0.25f;

            if (Physics.Raycast(rayStart, direction, out RaycastHit hit, remainingLength))
            {
                // Ignore self-hit
                if (hit.collider == ballCollider)
                    break;

                Vector3 hitPoint = hit.point;

                hitPoint.y = aimLineY;

                aimLine.positionCount = pointIndex + 1;

                aimLine.SetPosition(pointIndex, hitPoint);

                pointIndex++;

                remainingLength -= Vector3.Distance(currentPoint, hitPoint);

                // Reflect direction for bounce prediction
                direction = Vector3.Reflect(direction, hit.normal);

                direction.y = 0f;

                direction.Normalize();

                currentPoint = hitPoint + direction * 0.05f;

                currentPoint.y = aimLineY;
            }
            else
            {
                // No collision -> final endpoint
                Vector3 endPoint = currentPoint + direction * remainingLength;

                endPoint.y = aimLineY;

                aimLine.positionCount = pointIndex + 1;

                aimLine.SetPosition(pointIndex, endPoint);

                break;
            }
        }
    }

    // Hide projected path
    private void HideAimLine()
    {
        if (aimLine == null)
            return;

        aimLine.enabled = false;
        aimLine.positionCount = 0;
    }

    // -------------------------
    // SHOOT BALL
    // -------------------------

    private void ShootBall()
    {
        Vector3 dragVector = GetDragVector();

        // Ignore tiny drags
        if (dragVector.sqrMagnitude < 0.0001f)
            return;

        // Reverse drag direction for shot force
        Vector3 force = -dragVector * shotForceMultiplier;

        force.y = 0f;

        // Stop existing movement
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        PlaySFX(shootSFX);

        // Apply impulse force
        rb.AddForce(force, ForceMode.Impulse);

        shotTaken = true;
    }

    // -------------------------
    // MOVEMENT CHECK
    // -------------------------

    private bool IsMoving()
    {
        Vector3 horizontalVelocity = rb.linearVelocity;

        horizontalVelocity.y = 0f;

        return horizontalVelocity.magnitude > minSpeedToBeMoving;
    }

    // -------------------------
    // CLICK DETECTION
    // -------------------------

    private bool DidClickBall()
    {
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit))
            return hit.collider == ballCollider;

        return false;
    }

    // -------------------------
    // COLLISION SFX
    // -------------------------

    private void OnCollisionEnter(Collision collision)
    {
        if (rb == null)
            return;

        // Play bounce sound only if collision strong enough
        if (collision.relativeVelocity.magnitude >= minBounceSpeedForSFX)
            PlaySFX(bounceSFX);
    }

    // -------------------------
    // PLAY AUDIO
    // -------------------------

    private void PlaySFX(AudioClip clip)
    {
        if (clip == null)
            return;

        if (audioSource != null)
            audioSource.PlayOneShot(clip);
    }

    // -------------------------
    // LEVEL RESET
    // -------------------------

    private System.Collections.IEnumerator ResetAfterDelay()
    {
        resetStarted = true;

        yield return new WaitForSeconds(stoppedResetDelay);

        Time.timeScale = 1f;

        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
        );
    }

    // Used by draggable blocks to prevent movement during shots
    public bool IsBallMovingOrShot()
    {
        return IsMoving();
    }
}