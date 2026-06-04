using UnityEngine;
using UnityEngine.Events;

public sealed class PerspectivePuzzle : MonoBehaviour
{
    [Header("Observer")]
    [SerializeField] private Camera observerCamera;
    [SerializeField] private Transform requiredViewPosition;
    [SerializeField] private Transform lookTarget;
    [SerializeField] private Transform viewDirection;

    [Header("Player Light")]
    [SerializeField] private PlayerLight playerLight;

    [Header("Position")]
    [SerializeField, Min(0.05f)] private float enterPositionRadius = 0.8f;
    [SerializeField, Min(0.05f)] private float exitPositionRadius = 1.05f;
    [SerializeField] private bool ignoreHeight = true;

    [Header("Look")]
    [SerializeField, Range(1f, 90f)] private float enterAngleTolerance = 18f;
    [SerializeField, Range(1f, 90f)] private float exitAngleTolerance = 26f;

    [Header("Timing")]
    [SerializeField, Min(0f)] private float holdDuration = 0.45f;
    [SerializeField, Min(0f)] private float progressDrainSpeed = 1.6f;
    [SerializeField] private bool solveOnce = true;
    [SerializeField] private bool requirePlayerInputAllowed = true;

    [Header("Reward")]
    [SerializeField] private GameObject[] activateOnSolved;
    [SerializeField] private GameObject[] deactivateOnSolved;
    [SerializeField] private UnityEvent onSolved;

    private bool solved;
    private bool positionMatched;
    private bool angleMatched;
    private float holdProgress;

    public bool IsSolved => solved;
    public bool IsPositionMatched => positionMatched;
    public bool IsAngleMatched => angleMatched;
    public float HoldProgress => holdDuration <= 0f ? 1f : Mathf.Clamp01(holdProgress / holdDuration);

    private Transform RequiredViewPosition => requiredViewPosition != null ? requiredViewPosition : transform;

    private void Reset()
    {
        requiredViewPosition = transform;
        observerCamera = Camera.main;
        exitPositionRadius = Mathf.Max(exitPositionRadius, enterPositionRadius);
        exitAngleTolerance = Mathf.Max(exitAngleTolerance, enterAngleTolerance);
    }

    private void Awake()
    {
        NormalizeSettings();
        EnsureObserverCamera();
    }

    private void Update()
    {
        if (solved && solveOnce)
        {
            return;
        }

        if (requirePlayerInputAllowed && PlayerStateManager.HasInstance &&
            !PlayerStateManager.Instance.CanProcessPlayerInput)
        {
            DrainProgress(Time.deltaTime);
            return;
        }

        if (!IsPlayerLightOff())
        {
            positionMatched = false;
            angleMatched = false;
            DrainProgress(Time.deltaTime);
            return;
        }

        EnsureObserverCamera();
        if (observerCamera == null)
        {
            DrainProgress(Time.deltaTime);
            return;
        }

        positionMatched = CheckPosition();
        angleMatched = CheckAngle();

        if (positionMatched && angleMatched)
        {
            holdProgress += Time.deltaTime;
            if (holdProgress >= holdDuration)
            {
                Solve();
            }

            return;
        }

        DrainProgress(Time.deltaTime);
    }

    public void Solve()
    {
        if (solved && solveOnce)
        {
            return;
        }

        solved = true;
        holdProgress = holdDuration;
        SetObjectsActive(activateOnSolved, true);
        SetObjectsActive(deactivateOnSolved, false);
        onSolved?.Invoke();
    }

    public void ResetPuzzleProgress()
    {
        if (solveOnce && solved)
        {
            return;
        }

        holdProgress = 0f;
        positionMatched = false;
        angleMatched = false;
    }

    private bool CheckPosition()
    {
        Vector3 observerPosition = observerCamera.transform.position;
        Vector3 requiredPosition = RequiredViewPosition.position;

        if (ignoreHeight)
        {
            observerPosition.y = 0f;
            requiredPosition.y = 0f;
        }

        float radius = positionMatched ? exitPositionRadius : enterPositionRadius;
        return Vector3.Distance(observerPosition, requiredPosition) <= radius;
    }

    private bool CheckAngle()
    {
        if (!TryGetRequiredViewDirection(out Vector3 requiredDirection))
        {
            return false;
        }

        float tolerance = angleMatched ? exitAngleTolerance : enterAngleTolerance;
        float angle = Vector3.Angle(observerCamera.transform.forward, requiredDirection);
        return angle <= tolerance;
    }

    private bool TryGetRequiredViewDirection(out Vector3 direction)
    {
        if (lookTarget != null)
        {
            direction = lookTarget.position - observerCamera.transform.position;
            return direction.sqrMagnitude > Mathf.Epsilon;
        }

        Transform directionSource = viewDirection != null ? viewDirection : transform;
        direction = directionSource.forward;
        return direction.sqrMagnitude > Mathf.Epsilon;
    }

    private void DrainProgress(float deltaTime)
    {
        holdProgress = Mathf.Max(0f, holdProgress - progressDrainSpeed * deltaTime);
    }

    private bool IsPlayerLightOff()
    {
        return playerLight != null && !playerLight.IsLightOn;
    }

    private static void SetObjectsActive(GameObject[] targets, bool active)
    {
        if (targets == null)
        {
            return;
        }

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] != null)
            {
                targets[i].SetActive(active);
            }
        }
    }

    private void EnsureObserverCamera()
    {
        if (observerCamera != null)
        {
            return;
        }

        observerCamera = Camera.main;
        if (observerCamera == null)
        {
            observerCamera = FindAnyObjectByType<Camera>();
        }
    }

    private void NormalizeSettings()
    {
        enterPositionRadius = Mathf.Max(0.05f, enterPositionRadius);
        exitPositionRadius = Mathf.Max(enterPositionRadius, exitPositionRadius);
        enterAngleTolerance = Mathf.Clamp(enterAngleTolerance, 1f, 90f);
        exitAngleTolerance = Mathf.Clamp(Mathf.Max(enterAngleTolerance, exitAngleTolerance), 1f, 90f);
        holdDuration = Mathf.Max(0f, holdDuration);
        progressDrainSpeed = Mathf.Max(0f, progressDrainSpeed);
    }

    private void OnValidate()
    {
        NormalizeSettings();
    }

    private void OnDrawGizmosSelected()
    {
        Transform positionSource = RequiredViewPosition;
        Vector3 requiredPosition = positionSource.position;

        Gizmos.color = new Color(0.2f, 0.75f, 1f, 0.35f);
        Gizmos.DrawWireSphere(requiredPosition, enterPositionRadius);

        Gizmos.color = new Color(0.2f, 0.75f, 1f, 0.18f);
        Gizmos.DrawWireSphere(requiredPosition, exitPositionRadius);

        if (!TryGetGizmoViewDirection(requiredPosition, out Vector3 direction))
        {
            return;
        }

        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.85f);
        Gizmos.DrawRay(requiredPosition, direction.normalized * 1.5f);
    }

    private bool TryGetGizmoViewDirection(Vector3 requiredPosition, out Vector3 direction)
    {
        if (lookTarget != null)
        {
            direction = lookTarget.position - requiredPosition;
            return direction.sqrMagnitude > Mathf.Epsilon;
        }

        Transform directionSource = viewDirection != null ? viewDirection : transform;
        direction = directionSource.forward;
        return direction.sqrMagnitude > Mathf.Epsilon;
    }
}
