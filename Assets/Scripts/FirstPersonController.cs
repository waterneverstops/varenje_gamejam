using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public sealed class FirstPersonController : MonoBehaviour, GameInputs.IPlayerActions
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform cameraRoot;

    [Header("Look")]
    [SerializeField] private bool lockCursor = true;
    [SerializeField] private bool canLook = true;
    [SerializeField] private bool invertY;
    [SerializeField] private float mouseSensitivity = 0.12f;
    [SerializeField] private float gamepadSensitivity = 140f;
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;
    [SerializeField] private float defaultFov = 60f;
    [SerializeField] private bool allowZoom = true;
    [SerializeField] private bool holdZoom = true;
    [SerializeField] private float zoomFov = 35f;
    [SerializeField] private float fovLerpSpeed = 12f;

    [Header("Movement")]
    [SerializeField] private bool canMove = true;
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float crouchSpeedMultiplier = 0.45f;
    [SerializeField] private float maxVelocityChange = 12f;
    [SerializeField] private float airControlMultiplier = 0.35f;
    [SerializeField] private bool slideAlongWalls = true;
    [SerializeField, Range(0f, 89f)] private float minWallSlideAngle = 35f;
    [SerializeField, Range(0f, 1f)] private float wallSlideStrength = 0.35f;

    [Header("Sprint")]
    [SerializeField] private bool canSprint = true;
    [SerializeField] private bool unlimitedSprint;
    [SerializeField] private float sprintDuration = 5f;
    [SerializeField] private float sprintRecoverySpeed = 1.25f;
    [SerializeField] private float sprintCooldown = 0.5f;
    [SerializeField] private float sprintFov = 72f;

    [Header("Jump")]
    [SerializeField] private bool canJump = true;
    [SerializeField] private float jumpImpulse = 5f;
    [SerializeField] private float jumpBufferTime = 0.12f;
    [SerializeField] private float coyoteTime = 0.08f;
    [SerializeField] private float groundCheckLockAfterJump = 0.08f;

    [Header("Crouch")]
    [SerializeField] private bool canCrouch = true;
    [SerializeField] private bool holdCrouch = true;
    [SerializeField] private float crouchHeight = 0.75f;

    [Header("Ground Check")]
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float groundCheckDistance = 0.12f;
    [SerializeField] private float groundProbeRadius = 0.18f;
    [SerializeField, Range(0f, 1f)] private float minGroundNormalY = 0.65f;
    [SerializeField] private float groundContactGraceTime = 0.08f;

    [Header("Head Bob")]
    [SerializeField] private bool useHeadBob = true;
    [SerializeField] private Transform headBobTarget;
    [SerializeField] private float headBobSpeed = 10f;
    [SerializeField] private Vector3 headBobAmount = new Vector3(0.08f, 0.04f, 0f);

    private Rigidbody body;
    private CapsuleCollider capsule;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private Vector3 originalScale;
    private Vector3 headBobStartPosition;
    private float stamina;
    private float sprintCooldownTimer;
    private float pitch;
    private float headBobTimer;
    private float lastGroundedTime = -999f;
    private float jumpPressedTime = -1f;
    private float groundCheckLockTimer;
    private readonly Vector3[] wallNormals = new Vector3[8];
    private int wallNormalCount;
    private bool grounded;
    private bool sprintHeld;
    private bool sprinting;
    private bool crouched;
    private bool zoomed;
    private bool zoomHeld;
    private bool lookInputIsPointer;
    private bool jumpRequested;
    private bool subscribedToInput;

    public float Stamina => stamina;
    public float MaxStamina => sprintDuration;
    public float StaminaNormalized => sprintDuration <= 0f ? 1f : Mathf.Clamp01(stamina / sprintDuration);
    public bool HasUnlimitedSprint => unlimitedSprint;
    public bool IsStaminaFull => unlimitedSprint || Mathf.Approximately(stamina, sprintDuration);
    public bool IsSprinting => sprinting;
    public bool IsCrouched => crouched;
    public bool IsGrounded => grounded;

    private void Reset()
    {
        playerCamera = GetComponentInChildren<Camera>();
        cameraRoot = playerCamera != null ? playerCamera.transform : transform;
        headBobTarget = cameraRoot;
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();

        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
        }

        if (cameraRoot == null && playerCamera != null)
        {
            cameraRoot = playerCamera.transform;
        }

        if (headBobTarget == null)
        {
            headBobTarget = cameraRoot;
        }

        originalScale = transform.localScale;
        headBobStartPosition = headBobTarget != null ? headBobTarget.localPosition : Vector3.zero;
        stamina = sprintDuration;
        body.freezeRotation = true;

        if (playerCamera != null)
        {
            playerCamera.fieldOfView = defaultFov;
        }

    }

    private void OnEnable()
    {
        InputService.Instance.SubscribeMovement(this);
        subscribedToInput = true;

        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void OnDisable()
    {
        if (subscribedToInput && InputService.HasInstance)
        {
            InputService.Instance.UnsubscribeMovement(this);
        }

        subscribedToInput = false;
        moveInput = Vector2.zero;
        lookInput = Vector2.zero;
        sprintHeld = false;
        zoomHeld = false;
        zoomed = false;
        jumpRequested = false;
        jumpPressedTime = -1f;
        groundCheckLockTimer = 0f;
        wallNormalCount = 0;

        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void Update()
    {
        TickGrounded(Time.deltaTime);

        ReadLook();
        TickSprint(Time.deltaTime);
        TickCameraFov(Time.deltaTime);
        TickHeadBob(Time.deltaTime);
    }

    private void FixedUpdate()
    {
        ReadJump();

        if (!canMove)
        {
            sprinting = false;
            return;
        }

        Vector3 localDirection = Vector3.ClampMagnitude(new Vector3(moveInput.x, 0f, moveInput.y), 1f);
        Vector3 worldDirection = transform.TransformDirection(localDirection);
        worldDirection = ProjectMovementAlongWalls(worldDirection);

        float speed = GetCurrentSpeed(moveInput);
        Vector3 targetVelocity = worldDirection * speed;
        Vector3 currentVelocity = body.linearVelocity;
        Vector3 velocityChange = targetVelocity - new Vector3(currentVelocity.x, 0f, currentVelocity.z);
        float control = grounded ? 1f : airControlMultiplier;

        velocityChange.x = Mathf.Clamp(velocityChange.x, -maxVelocityChange, maxVelocityChange) * control;
        velocityChange.z = Mathf.Clamp(velocityChange.z, -maxVelocityChange, maxVelocityChange) * control;
        velocityChange.y = 0f;

        body.AddForce(velocityChange, ForceMode.VelocityChange);
        wallNormalCount = 0;
    }

    private void OnCollisionStay(Collision collision)
    {
        if (!IsInGroundMask(collision.collider.gameObject.layer))
        {
            return;
        }

        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector3 normal = collision.GetContact(i).normal;
            if (normal.y >= minGroundNormalY)
            {
                if (groundCheckLockTimer <= 0f)
                {
                    grounded = true;
                    lastGroundedTime = Time.time;
                }

                continue;
            }

            RegisterWallNormal(normal);
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
        lookInputIsPointer = context.control != null && context.control.device is Pointer;
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            jumpRequested = true;
            jumpPressedTime = Time.time;
        }
    }

    public void OnCrouch(InputAction.CallbackContext context)
    {
        if (!canCrouch)
        {
            return;
        }

        if (holdCrouch)
        {
            if (context.started)
            {
                SetCrouched(true);
            }
            else if (context.canceled)
            {
                SetCrouched(false);
            }
        }
        else if (context.started)
        {
            SetCrouched(!crouched);
        }
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        sprintHeld = context.ReadValueAsButton();
    }

    public void OnZoom(InputAction.CallbackContext context)
    {
        if (!allowZoom)
        {
            return;
        }

        if (holdZoom)
        {
            zoomHeld = context.ReadValueAsButton();
            return;
        }

        if (context.started && !sprinting)
        {
            zoomed = !zoomed;
        }
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
    }

    public void OnPrevious(InputAction.CallbackContext context)
    {
    }

    public void OnNext(InputAction.CallbackContext context)
    {
    }

    private void ReadLook()
    {
        if (!canLook || lookInput.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        float horizontalScale = lookInputIsPointer ? mouseSensitivity : gamepadSensitivity * Time.deltaTime;
        float verticalScale = invertY ? horizontalScale : -horizontalScale;

        transform.Rotate(Vector3.up, lookInput.x * horizontalScale, Space.Self);
        pitch = Mathf.Clamp(pitch + lookInput.y * verticalScale, minPitch, maxPitch);

        if (cameraRoot != null)
        {
            cameraRoot.localEulerAngles = new Vector3(pitch, 0f, 0f);
        }
    }

    private void ReadJump()
    {
        if (!jumpRequested)
        {
            return;
        }

        if (Time.time - jumpPressedTime > jumpBufferTime)
        {
            jumpRequested = false;
            return;
        }

        bool canUseGround = grounded || Time.time - lastGroundedTime <= coyoteTime;
        if (!canJump || !canUseGround)
        {
            return;
        }

        jumpRequested = false;
        jumpPressedTime = -1f;

        if (crouched && !holdCrouch)
        {
            SetCrouched(false);
        }

        Vector3 velocity = body.linearVelocity;
        if (velocity.y < 0f)
        {
            body.linearVelocity = new Vector3(velocity.x, 0f, velocity.z);
        }

        body.AddForce(Vector3.up * jumpImpulse, ForceMode.Impulse);
        grounded = false;
        groundCheckLockTimer = groundCheckLockAfterJump;
    }

    private void SetCrouched(bool value)
    {
        if (crouched == value)
        {
            return;
        }

        crouched = value;
        float targetHeight = crouched ? crouchHeight : originalScale.y;
        transform.localScale = new Vector3(originalScale.x, targetHeight, originalScale.z);
    }

    private void TickSprint(float deltaTime)
    {
        sprintCooldownTimer = Mathf.Max(0f, sprintCooldownTimer - deltaTime);

        if (unlimitedSprint)
        {
            stamina = sprintDuration;
        }

        bool wantsSprint = canSprint && !crouched && sprintHeld;
        bool hasMoveInput = moveInput.sqrMagnitude > 0.01f;
        bool hasStamina = unlimitedSprint || stamina > 0f;

        sprinting = wantsSprint && hasMoveInput && hasStamina && sprintCooldownTimer <= 0f;

        if (sprinting && !unlimitedSprint)
        {
            stamina = Mathf.Max(0f, stamina - deltaTime);

            if (stamina <= 0f)
            {
                sprinting = false;
                sprintCooldownTimer = sprintCooldown;
            }
        }
        else if (!unlimitedSprint)
        {
            stamina = Mathf.Min(sprintDuration, stamina + sprintRecoverySpeed * deltaTime);
        }
    }

    private float GetCurrentSpeed(Vector2 input)
    {
        if (input.sqrMagnitude <= 0.01f)
        {
            sprinting = false;
            return 0f;
        }

        if (sprinting)
        {
            return sprintSpeed;
        }

        return crouched ? walkSpeed * crouchSpeedMultiplier : walkSpeed;
    }

    private Vector3 ProjectMovementAlongWalls(Vector3 direction)
    {
        if (!slideAlongWalls || direction.sqrMagnitude <= Mathf.Epsilon)
        {
            return direction;
        }

        float inputMagnitude = Mathf.Clamp01(direction.magnitude);
        float minSlideDot = Mathf.Cos(minWallSlideAngle * Mathf.Deg2Rad);

        for (int i = 0; i < wallNormalCount; i++)
        {
            Vector3 normal = wallNormals[i];
            Vector3 moveDirection = direction.normalized;
            float signedIntoWall = Vector3.Dot(moveDirection, normal);
            if (signedIntoWall < 0f)
            {
                normal = -normal;
                signedIntoWall = -signedIntoWall;
            }

            if (signedIntoWall <= 0f)
            {
                continue;
            }

            Vector3 slideDirection = Vector3.ProjectOnPlane(direction, normal);
            if (signedIntoWall >= minSlideDot)
            {
                direction = Vector3.zero;
                continue;
            }

            float slideFactor = Mathf.InverseLerp(minSlideDot, 0f, signedIntoWall);
            float slideScale = Mathf.Lerp(0f, wallSlideStrength, slideFactor);
            direction = slideDirection * slideScale;
        }

        if (direction.sqrMagnitude <= Mathf.Epsilon)
        {
            return Vector3.zero;
        }

        return Vector3.ClampMagnitude(direction.normalized * inputMagnitude, 1f);
    }

    private void RegisterWallNormal(Vector3 normal)
    {
        normal.y = 0f;

        if (normal.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        normal.Normalize();

        for (int i = 0; i < wallNormalCount; i++)
        {
            if (Vector3.Dot(wallNormals[i], normal) > 0.95f)
            {
                return;
            }
        }

        if (wallNormalCount >= wallNormals.Length)
        {
            return;
        }

        wallNormals[wallNormalCount] = normal;
        wallNormalCount++;
    }

    private void TickCameraFov(float deltaTime)
    {
        if (playerCamera == null)
        {
            return;
        }

        if (allowZoom && holdZoom)
        {
            zoomed = zoomHeld && !sprinting;
        }

        if (sprinting)
        {
            zoomed = false;
        }

        float targetFov = sprinting ? sprintFov : zoomed ? zoomFov : defaultFov;
        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFov, fovLerpSpeed * deltaTime);
    }

    private bool CheckGrounded()
    {
        Vector3 center = transform.position;
        float radius = groundProbeRadius;
        float distance = groundCheckDistance + 0.08f;

        if (capsule != null)
        {
            center = transform.TransformPoint(capsule.center);
            float height = capsule.height * Mathf.Abs(transform.localScale.y);
            float bottom = center.y - height * 0.5f;
            center.y = bottom + 0.08f;
            radius = Mathf.Min(radius, capsule.radius * Mathf.Max(transform.localScale.x, transform.localScale.z) * 0.7f);
        }

        radius = Mathf.Max(0f, radius);

        if (HasGroundBelow(center, distance))
        {
            return true;
        }

        Vector3 right = transform.right * radius;
        Vector3 forward = transform.forward * radius;

        return HasGroundBelow(center + right, distance)
               || HasGroundBelow(center - right, distance)
               || HasGroundBelow(center + forward, distance)
               || HasGroundBelow(center - forward, distance);
    }

    private void TickGrounded(float deltaTime)
    {
        groundCheckLockTimer = Mathf.Max(0f, groundCheckLockTimer - deltaTime);
        bool hasGroundBelow = CheckGrounded();
        bool hasRecentGroundContact = Time.time - lastGroundedTime <= groundContactGraceTime;
        grounded = groundCheckLockTimer <= 0f && (hasGroundBelow || hasRecentGroundContact);

        if (groundCheckLockTimer <= 0f && hasGroundBelow)
        {
            lastGroundedTime = Time.time;
        }
    }

    private bool HasGroundBelow(Vector3 origin, float distance)
    {
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, distance, groundMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.collider == null || IsOwnCollider(hit.collider) || hit.normal.y < minGroundNormalY)
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private bool IsOwnCollider(Collider hitCollider)
    {
        return hitCollider.transform == transform || hitCollider.transform.IsChildOf(transform);
    }

    private bool IsInGroundMask(int layer)
    {
        return (groundMask.value & (1 << layer)) != 0;
    }

    private void TickHeadBob(float deltaTime)
    {
        if (!useHeadBob || headBobTarget == null)
        {
            return;
        }

        bool walking = grounded && moveInput.sqrMagnitude > 0.01f;

        if (!walking)
        {
            headBobTimer = 0f;
            headBobTarget.localPosition = Vector3.Lerp(headBobTarget.localPosition, headBobStartPosition, headBobSpeed * deltaTime);
            return;
        }

        float speedMultiplier = sprinting ? 1.45f : crouched ? crouchSpeedMultiplier : 1f;
        headBobTimer += deltaTime * headBobSpeed * speedMultiplier;

        Vector3 offset = new Vector3(
            Mathf.Sin(headBobTimer) * headBobAmount.x,
            Mathf.Abs(Mathf.Sin(headBobTimer)) * headBobAmount.y,
            Mathf.Sin(headBobTimer) * headBobAmount.z);

        headBobTarget.localPosition = headBobStartPosition + offset;
    }

    private void OnValidate()
    {
        sprintDuration = Mathf.Max(0.1f, sprintDuration);
        sprintRecoverySpeed = Mathf.Max(0.01f, sprintRecoverySpeed);
        sprintCooldown = Mathf.Max(0f, sprintCooldown);
        jumpBufferTime = Mathf.Max(0f, jumpBufferTime);
        coyoteTime = Mathf.Max(0f, coyoteTime);
        groundCheckLockAfterJump = Mathf.Max(0f, groundCheckLockAfterJump);
        groundCheckDistance = Mathf.Max(0.01f, groundCheckDistance);
        groundProbeRadius = Mathf.Max(0f, groundProbeRadius);
        groundContactGraceTime = Mathf.Max(0f, groundContactGraceTime);
        minWallSlideAngle = Mathf.Clamp(minWallSlideAngle, 0f, 89f);
        wallSlideStrength = Mathf.Clamp01(wallSlideStrength);
        walkSpeed = Mathf.Max(0f, walkSpeed);
        sprintSpeed = Mathf.Max(walkSpeed, sprintSpeed);
        crouchSpeedMultiplier = Mathf.Clamp01(crouchSpeedMultiplier);
        crouchHeight = Mathf.Max(0.1f, crouchHeight);
        minPitch = Mathf.Min(minPitch, maxPitch);
        maxPitch = Mathf.Max(minPitch, maxPitch);
        defaultFov = Mathf.Clamp(defaultFov, 1f, 179f);
        zoomFov = Mathf.Clamp(zoomFov, 1f, defaultFov);
        sprintFov = Mathf.Clamp(sprintFov, defaultFov, 179f);
    }
}
