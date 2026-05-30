using System;
using UnityEngine;
using UnityEngine.InputSystem;

[Serializable]
public sealed class GrabbedRigidbodySettings
{
    public bool useGravity;
    public float linearDamping = 10f;
    public float angularDamping = 10f;
    public RigidbodyConstraints constraints = RigidbodyConstraints.FreezeRotation;
}

public sealed class PlayerGrabbing : MonoBehaviour, GameInputs.IPlayerActions
{
    [Header("References")]
    [SerializeField] private Transform grabOrigin;
    [SerializeField] private LineRenderer lineRenderer;

    [Header("Grab")]
    [SerializeField, Range(4f, 50f)] private float grabSpeed = 7f;
    [SerializeField, Range(4f, 25f)] private float grabMaxDistance = 10f;
    [SerializeField, Range(10f, 50f)] private float throwImpulse = 25f;
    [SerializeField] private bool enableThrowAction;
    [SerializeField] private LayerMask collisionMask = ~0;
    [SerializeField] private GrabbedRigidbodySettings heldObjectSettings = new GrabbedRigidbodySettings();

    private readonly GrabbedRigidbodySettings cachedSettings = new GrabbedRigidbodySettings();

    private GameObject anchorObject;
    private Rigidbody heldBody;
    private float holdDistance;
    private bool heldBodyHasHinge;
    private bool subscribedToInput;

    public Rigidbody HeldBody => heldBody;
    public bool IsHolding => heldBody != null;

    private void Reset()
    {
        grabOrigin = transform;
        lineRenderer = GetComponent<LineRenderer>();
    }

    private void Awake()
    {
        if (grabOrigin == null)
        {
            grabOrigin = transform;
        }

        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
        }

        anchorObject = new GameObject("Grab Anchor");
        anchorObject.hideFlags = HideFlags.HideInHierarchy;
        SetLineVisible(false);
    }

    private void OnEnable()
    {
        InputService.Instance.SubscribeGrab(this);
        subscribedToInput = true;
    }

    private void OnDisable()
    {
        Drop();

        if (subscribedToInput && InputService.HasInstance)
        {
            InputService.Instance.UnsubscribeGrab(this);
        }

        subscribedToInput = false;
    }

    private void OnDestroy()
    {
        if (anchorObject != null)
        {
            Destroy(anchorObject);
        }
    }

    private void FixedUpdate()
    {
        if (!IsHolding)
        {
            return;
        }

        Vector3 targetPosition = grabOrigin.position + grabOrigin.forward * holdDistance;
        Vector3 anchorPosition = anchorObject.transform.position;
        Vector3 offset = targetPosition - anchorPosition;

        if (heldBodyHasHinge)
        {
            heldBody.AddForceAtPosition(offset * grabSpeed * 100f, anchorPosition, ForceMode.Force);
        }
        else
        {
            heldBody.linearVelocity = offset * grabSpeed;
        }

        DrawGrabLine(targetPosition, anchorPosition);
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.started && !IsHolding)
        {
            TryGrab();
        }
        else if (context.canceled && IsHolding)
        {
            Drop();
        }
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (enableThrowAction && context.started && IsHolding)
        {
            ThrowHeldObject();
        }
    }

    public void OnLight(InputAction.CallbackContext context)
    {
    }

    public void OnMove(InputAction.CallbackContext context)
    {
    }

    public void OnLook(InputAction.CallbackContext context)
    {
    }

    public void OnCrouch(InputAction.CallbackContext context)
    {
    }

    public void OnJump(InputAction.CallbackContext context)
    {
    }

    public void OnPrevious(InputAction.CallbackContext context)
    {
    }

    public void OnNext(InputAction.CallbackContext context)
    {
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
    }

    public void OnZoom(InputAction.CallbackContext context)
    {
    }

    public void Drop()
    {
        if (!IsHolding)
        {
            return;
        }

        RestoreHeldBody();
        heldBody = null;
        heldBodyHasHinge = false;
        anchorObject.transform.SetParent(null);
        SetLineVisible(false);
    }

    public void ThrowHeldObject()
    {
        if (!IsHolding)
        {
            return;
        }

        Rigidbody bodyToThrow = heldBody;
        Drop();
        bodyToThrow.linearVelocity = grabOrigin.forward * throwImpulse;
    }

    private void TryGrab()
    {
        if (!Physics.Raycast(grabOrigin.position, grabOrigin.forward, out RaycastHit hit, grabMaxDistance, collisionMask, QueryTriggerInteraction.Ignore))
        {
            return;
        }

        Rigidbody hitBody = hit.rigidbody;
        if (hitBody == null)
        {
            hitBody = hit.collider.GetComponentInParent<Rigidbody>();
        }

        if (hitBody == null || hitBody.isKinematic)
        {
            return;
        }

        StartHolding(hitBody, hit.distance, hit.point);
    }

    private void StartHolding(Rigidbody target, float distance, Vector3 hitPoint)
    {
        heldBody = target;
        heldBodyHasHinge = heldBody.GetComponent<HingeJoint>() != null;
        holdDistance = distance;

        CacheHeldBodySettings();
        ApplyHeldBodySettings();

        anchorObject.transform.SetParent(heldBody.transform, true);
        anchorObject.transform.position = hitPoint;
        anchorObject.transform.LookAt(grabOrigin);
    }

    private void CacheHeldBodySettings()
    {
        cachedSettings.useGravity = heldBody.useGravity;
        cachedSettings.linearDamping = heldBody.linearDamping;
        cachedSettings.angularDamping = heldBody.angularDamping;
        cachedSettings.constraints = heldBody.constraints;
    }

    private void ApplyHeldBodySettings()
    {
        heldBody.useGravity = heldObjectSettings.useGravity;
        heldBody.linearDamping = heldObjectSettings.linearDamping;
        heldBody.angularDamping = heldObjectSettings.angularDamping;
        heldBody.constraints = heldBodyHasHinge ? RigidbodyConstraints.None : heldObjectSettings.constraints;
    }

    private void RestoreHeldBody()
    {
        heldBody.useGravity = cachedSettings.useGravity;
        heldBody.linearDamping = cachedSettings.linearDamping;
        heldBody.angularDamping = cachedSettings.angularDamping;
        heldBody.constraints = cachedSettings.constraints;
    }

    private void DrawGrabLine(Vector3 targetPosition, Vector3 anchorPosition)
    {
        if (lineRenderer == null)
        {
            return;
        }

        lineRenderer.enabled = true;
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, targetPosition);
        lineRenderer.SetPosition(1, anchorPosition);
    }

    private void SetLineVisible(bool visible)
    {
        if (lineRenderer != null)
        {
            lineRenderer.enabled = visible;
        }
    }

    private void OnValidate()
    {
        grabMaxDistance = Mathf.Max(0.1f, grabMaxDistance);
        grabSpeed = Mathf.Max(0.1f, grabSpeed);
        throwImpulse = Mathf.Max(0f, throwImpulse);
        heldObjectSettings.linearDamping = Mathf.Max(0f, heldObjectSettings.linearDamping);
        heldObjectSettings.angularDamping = Mathf.Max(0f, heldObjectSettings.angularDamping);
    }
}
