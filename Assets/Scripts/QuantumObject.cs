using System.Collections.Generic;
using UnityEngine;

public sealed class QuantumObject : MonoBehaviour
{
    [Header("Quantum")]
    [SerializeField] private string id = "Default";
    [SerializeField] private bool startsMaterialized;

    [Header("Sampling")]
    [SerializeField] private Transform sampleRoot;
    [SerializeField] private Transform[] extraSamplePoints;
    [SerializeField] private bool sampleRenderers = true;
    [SerializeField] private bool sampleColliders = true;

    [Header("Controlled Components")]
    [SerializeField] private bool controlRenderers = true;
    [SerializeField] private bool controlColliders = true;
    [SerializeField] private bool controlRigidbodies = true;
    [SerializeField] private Renderer[] controlledRenderers;
    [SerializeField] private Collider[] controlledColliders;
    [SerializeField] private Rigidbody[] controlledRigidbodies;

    private readonly List<Vector3> localObservationPoints = new List<Vector3>();
    private RigidbodyState[] rigidbodyStates;
    private Vector3 initialLocalPosition;
    private Quaternion initialLocalRotation;
    private bool pointsDirty = true;
    private bool componentsCached;
    private bool materialized = true;
    private bool initialPositionStored;

    public string Id => id;
    public bool StartsMaterialized => startsMaterialized;
    public bool IsMaterialized => materialized;

    private Transform SampleRoot => sampleRoot != null ? sampleRoot : transform;

    private void Reset()
    {
        id = gameObject.name;
        sampleRoot = transform;
        CacheControlledComponents();
        pointsDirty = true;
    }

    private void Awake()
    {
        CacheControlledComponents();
        StoreInitialPosition();
        RebuildObservationPoints();
    }

    private void OnEnable()
    {
        QuantumObjectManager.Instance.Register(this);
    }

    private void OnDisable()
    {
        if (QuantumObjectManager.HasInstance)
        {
            QuantumObjectManager.Instance.Unregister(this);
        }
    }

    public void SetMaterialized(bool value)
    {
        CacheControlledComponents();
        bool materializedChanged = materialized != value;
        materialized = value;

        if (materializedChanged)
        {
            ResetToInitialPositionAndStopInertia();
        }

        if (controlRenderers)
        {
            for (int i = 0; i < controlledRenderers.Length; i++)
            {
                if (controlledRenderers[i] != null)
                {
                    controlledRenderers[i].enabled = value;
                }
            }
        }

        if (controlColliders)
        {
            for (int i = 0; i < controlledColliders.Length; i++)
            {
                if (controlledColliders[i] != null)
                {
                    controlledColliders[i].enabled = value;
                }
            }
        }

        if (controlRigidbodies)
        {
            for (int i = 0; i < controlledRigidbodies.Length; i++)
            {
                Rigidbody body = controlledRigidbodies[i];
                if (body == null)
                {
                    continue;
                }

                if (value)
                {
                    rigidbodyStates[i].ApplyTo(body);
                }
                else
                {
                    rigidbodyStates[i] = RigidbodyState.From(body);
                    body.linearVelocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                    body.useGravity = false;
                    body.isKinematic = true;
                    body.detectCollisions = false;
                }
            }
        }
    }

    public void GetObservationPoints(List<Vector3> points)
    {
        if (pointsDirty || localObservationPoints.Count == 0)
        {
            RebuildObservationPoints();
        }

        Transform root = SampleRoot;
        for (int i = 0; i < localObservationPoints.Count; i++)
        {
            points.Add(root.TransformPoint(localObservationPoints[i]));
        }

        if (extraSamplePoints == null)
        {
            return;
        }

        for (int i = 0; i < extraSamplePoints.Length; i++)
        {
            if (extraSamplePoints[i] != null)
            {
                points.Add(extraSamplePoints[i].position);
            }
        }
    }

    public bool OwnsCollider(Collider target)
    {
        if (target == null)
        {
            return false;
        }

        Transform targetTransform = target.transform;
        return targetTransform == transform || targetTransform.IsChildOf(transform);
    }

    public bool TryGetWorldBounds(out Bounds bounds)
    {
        CacheControlledComponents();

        bounds = default;
        bool hasBounds = false;

        for (int i = 0; i < controlledRenderers.Length; i++)
        {
            Renderer rendererComponent = controlledRenderers[i];
            if (rendererComponent == null)
            {
                continue;
            }

            if (hasBounds)
            {
                bounds.Encapsulate(rendererComponent.bounds);
            }
            else
            {
                bounds = rendererComponent.bounds;
                hasBounds = true;
            }
        }

        for (int i = 0; i < controlledColliders.Length; i++)
        {
            Collider colliderComponent = controlledColliders[i];
            if (colliderComponent == null)
            {
                continue;
            }

            if (hasBounds)
            {
                bounds.Encapsulate(colliderComponent.bounds);
            }
            else
            {
                bounds = colliderComponent.bounds;
                hasBounds = true;
            }
        }

        if (!hasBounds)
        {
            bounds = new Bounds(transform.position, Vector3.one * 0.25f);
        }

        return true;
    }

    private void CacheControlledComponents()
    {
        if (componentsCached)
        {
            return;
        }

        if (controlledRenderers == null || controlledRenderers.Length == 0)
        {
            controlledRenderers = GetComponentsInChildren<Renderer>(true);
        }

        if (controlledColliders == null || controlledColliders.Length == 0)
        {
            controlledColliders = GetComponentsInChildren<Collider>(true);
        }

        if (controlledRigidbodies == null || controlledRigidbodies.Length == 0)
        {
            controlledRigidbodies = GetComponentsInChildren<Rigidbody>(true);
        }

        rigidbodyStates = new RigidbodyState[controlledRigidbodies.Length];
        for (int i = 0; i < controlledRigidbodies.Length; i++)
        {
            rigidbodyStates[i] = controlledRigidbodies[i] != null
                ? RigidbodyState.From(controlledRigidbodies[i])
                : default;
        }

        componentsCached = true;
    }

    private void StoreInitialPosition()
    {
        initialLocalPosition = transform.localPosition;
        initialLocalRotation = transform.localRotation;
        initialPositionStored = true;
    }

    private void ResetToInitialPositionAndStopInertia()
    {
        if (!initialPositionStored)
        {
            StoreInitialPosition();
        }

        transform.SetLocalPositionAndRotation(initialLocalPosition, initialLocalRotation);

        for (int i = 0; i < controlledRigidbodies.Length; i++)
        {
            Rigidbody body = controlledRigidbodies[i];
            if (body == null)
            {
                continue;
            }

            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            rigidbodyStates[i].ResetTransform(body);
            body.Sleep();
        }
    }

    private void RebuildObservationPoints()
    {
        CacheControlledComponents();
        localObservationPoints.Clear();

        Transform root = SampleRoot;

        if (sampleRenderers)
        {
            for (int i = 0; i < controlledRenderers.Length; i++)
            {
                Renderer rendererComponent = controlledRenderers[i];
                if (rendererComponent != null)
                {
                    AddBoundsPoints(rendererComponent.bounds, root);
                }
            }
        }

        if (sampleColliders)
        {
            for (int i = 0; i < controlledColliders.Length; i++)
            {
                Collider colliderComponent = controlledColliders[i];
                if (colliderComponent != null && colliderComponent.enabled)
                {
                    AddBoundsPoints(colliderComponent.bounds, root);
                }
            }
        }

        if (localObservationPoints.Count == 0)
        {
            localObservationPoints.Add(root.InverseTransformPoint(transform.position));
        }

        pointsDirty = false;
    }

    private void AddBoundsPoints(Bounds bounds, Transform root)
    {
        if (bounds.size.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        Vector3 min = bounds.min;
        Vector3 max = bounds.max;
        Vector3 center = bounds.center;

        AddWorldPoint(center, root);

        AddWorldPoint(new Vector3(min.x, min.y, min.z), root);
        AddWorldPoint(new Vector3(min.x, min.y, max.z), root);
        AddWorldPoint(new Vector3(min.x, max.y, min.z), root);
        AddWorldPoint(new Vector3(min.x, max.y, max.z), root);
        AddWorldPoint(new Vector3(max.x, min.y, min.z), root);
        AddWorldPoint(new Vector3(max.x, min.y, max.z), root);
        AddWorldPoint(new Vector3(max.x, max.y, min.z), root);
        AddWorldPoint(new Vector3(max.x, max.y, max.z), root);

        AddWorldPoint(new Vector3(center.x, center.y, min.z), root);
        AddWorldPoint(new Vector3(center.x, center.y, max.z), root);
        AddWorldPoint(new Vector3(center.x, min.y, center.z), root);
        AddWorldPoint(new Vector3(center.x, max.y, center.z), root);
        AddWorldPoint(new Vector3(min.x, center.y, center.z), root);
        AddWorldPoint(new Vector3(max.x, center.y, center.z), root);
    }

    private void AddWorldPoint(Vector3 point, Transform root)
    {
        localObservationPoints.Add(root.InverseTransformPoint(point));
    }

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            id = "Default";
        }

        pointsDirty = true;
        componentsCached = false;
    }

    private void OnDrawGizmosSelected()
    {
        List<Vector3> points = new List<Vector3>();
        GetObservationPoints(points);

        Gizmos.color = Color.cyan;
        for (int i = 0; i < points.Count; i++)
        {
            Gizmos.DrawSphere(points[i], 0.04f);
        }
    }

    private struct RigidbodyState
    {
        private bool isKinematic;
        private bool useGravity;
        private bool detectCollisions;
        private Vector3 localPosition;
        private Quaternion localRotation;

        public static RigidbodyState From(Rigidbody body)
        {
            return new RigidbodyState
            {
                isKinematic = body.isKinematic,
                useGravity = body.useGravity,
                detectCollisions = body.detectCollisions,
                localPosition = body.transform.localPosition,
                localRotation = body.transform.localRotation,
            };
        }

        public void ApplyTo(Rigidbody body)
        {
            body.isKinematic = isKinematic;
            body.useGravity = useGravity;
            body.detectCollisions = detectCollisions;
        }

        public void ResetTransform(Rigidbody body)
        {
            body.transform.SetLocalPositionAndRotation(localPosition, localRotation);
        }
    }
}
