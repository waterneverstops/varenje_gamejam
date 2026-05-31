using System.Collections.Generic;
using UnityEngine;

public sealed class QuantumObjectManager : MonoBehaviour
{
    [Header("Observer")]
    [SerializeField] private Camera observerCamera;
    [SerializeField, Min(0f)] private float checkInterval = 0.12f;
    [SerializeField, Range(0f, 0.25f)] private float viewportMargin = 0.02f;

    [Header("Darkness")]
    [SerializeField] private LayerMask darknessLightLayers = ~0;
    [SerializeField] private Light[] trackedDarknessLights = new Light[0];

    private static QuantumObjectManager instance;

    private readonly Dictionary<string, QuantumGroup> groups = new Dictionary<string, QuantumGroup>();
    private readonly List<Vector3> pointBuffer = new List<Vector3>(32);
    private readonly List<Light> trackedLightBuffer = new List<Light>(32);
    private readonly List<int> candidateBuffer = new List<int>(16);
    private readonly Plane[] frustumPlanes = new Plane[6];
    private float nextCheckTime;

    public static bool HasInstance => instance != null;

    public static QuantumObjectManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<QuantumObjectManager>();
            }

            if (instance == null)
            {
                GameObject managerObject = new GameObject("Quantum Object Manager");
                instance = managerObject.AddComponent<QuantumObjectManager>();
            }

            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            enabled = false;
            return;
        }

        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void Update()
    {
        if (Time.time < nextCheckTime)
        {
            return;
        }

        nextCheckTime = Time.time + checkInterval;
        TickQuantumGroups();
    }

    public void Register(QuantumObject quantumObject)
    {
        if (quantumObject == null)
        {
            return;
        }

        string groupId = quantumObject.Id;
        if (!groups.TryGetValue(groupId, out QuantumGroup group))
        {
            group = new QuantumGroup();
            groups.Add(groupId, group);
        }

        if (group.Locations.Contains(quantumObject))
        {
            ApplyGroupState(group);
            return;
        }

        group.Locations.Add(quantumObject);
        int newIndex = group.Locations.Count - 1;
        if (group.ActiveIndex < 0 || quantumObject.StartsMaterialized)
        {
            group.ActiveIndex = newIndex;
        }

        ApplyGroupState(group);
    }

    public void Unregister(QuantumObject quantumObject)
    {
        if (quantumObject == null)
        {
            return;
        }

        if (!groups.TryGetValue(quantumObject.Id, out QuantumGroup group))
        {
            return;
        }

        int removedIndex = group.Locations.IndexOf(quantumObject);
        if (removedIndex < 0)
        {
            return;
        }

        group.Locations.RemoveAt(removedIndex);

        if (group.Locations.Count == 0)
        {
            groups.Remove(quantumObject.Id);
            return;
        }

        if (group.ActiveIndex == removedIndex)
        {
            group.ActiveIndex = Mathf.Clamp(removedIndex, 0, group.Locations.Count - 1);
        }
        else if (group.ActiveIndex > removedIndex)
        {
            group.ActiveIndex--;
        }

        ApplyGroupState(group);
    }

    public bool TryJump(string id)
    {
        if (!groups.TryGetValue(id, out QuantumGroup group))
        {
            return false;
        }

        EnsureObserverCamera();
        RefreshTrackedLights();
        NormalizeGroup(group);
        return TryMoveGroup(group);
    }

    private void TickQuantumGroups()
    {
        EnsureObserverCamera();
        RefreshTrackedLights();

        foreach (QuantumGroup group in groups.Values)
        {
            NormalizeGroup(group);

            if (group.Locations.Count <= 1 || group.ActiveIndex < 0)
            {
                ApplyGroupState(group);
                continue;
            }

            QuantumObject activeObject = group.Locations[group.ActiveIndex];
            if (!CanUseAsHiddenQuantumPosition(activeObject))
            {
                continue;
            }

            TryMoveGroup(group);
        }
    }

    private bool TryMoveGroup(QuantumGroup group)
    {
        candidateBuffer.Clear();

        for (int i = 0; i < group.Locations.Count; i++)
        {
            if (i == group.ActiveIndex)
            {
                continue;
            }

            QuantumObject candidate = group.Locations[i];
            if (candidate == null)
            {
                continue;
            }

            if (CanUseAsHiddenQuantumPosition(candidate))
            {
                candidateBuffer.Add(i);
            }
        }

        if (candidateBuffer.Count == 0)
        {
            return false;
        }

        int selectedCandidate = candidateBuffer[Random.Range(0, candidateBuffer.Count)];
        group.ActiveIndex = selectedCandidate;
        ApplyGroupState(group);
        return true;
    }

    private bool CanUseAsHiddenQuantumPosition(QuantumObject quantumObject)
    {
        if (quantumObject == null)
        {
            return false;
        }

        return AreAllTrackedLightsOff() && !IsObjectInCameraFrame(quantumObject);
    }

    private bool IsObjectInCameraFrame(QuantumObject quantumObject)
    {
        if (observerCamera == null || quantumObject == null)
        {
            return false;
        }

        pointBuffer.Clear();
        quantumObject.GetObservationPoints(pointBuffer);

        for (int i = 0; i < pointBuffer.Count; i++)
        {
            if (IsPointInCameraFrame(pointBuffer[i]))
            {
                return true;
            }
        }

        if (quantumObject.TryGetWorldBounds(out Bounds bounds) && bounds.size.sqrMagnitude > Mathf.Epsilon)
        {
            GeometryUtility.CalculateFrustumPlanes(observerCamera, frustumPlanes);
            return GeometryUtility.TestPlanesAABB(frustumPlanes, bounds);
        }

        return false;
    }

    private bool IsPointInCameraFrame(Vector3 point)
    {
        if (observerCamera == null)
        {
            return false;
        }

        Vector3 viewportPoint = observerCamera.WorldToViewportPoint(point);
        if (viewportPoint.z <= observerCamera.nearClipPlane)
        {
            return false;
        }

        if (viewportPoint.x < -viewportMargin || viewportPoint.x > 1f + viewportMargin ||
            viewportPoint.y < -viewportMargin || viewportPoint.y > 1f + viewportMargin)
        {
            return false;
        }

        return true;
    }

    private bool AreAllTrackedLightsOff()
    {
        for (int i = 0; i < trackedDarknessLights.Length; i++)
        {
            Light lightComponent = trackedDarknessLights[i];
            if (lightComponent != null && lightComponent.enabled && lightComponent.gameObject.activeInHierarchy)
            {
                return false;
            }
        }

        return true;
    }

    private void RefreshTrackedLights()
    {
        trackedLightBuffer.Clear();

        Light[] lights = FindObjectsByType<Light>(FindObjectsInactive.Include);
        for (int i = 0; i < lights.Length; i++)
        {
            Light lightComponent = lights[i];
            if (lightComponent != null && IsInDarknessLightLayer(lightComponent.gameObject.layer))
            {
                trackedLightBuffer.Add(lightComponent);
            }
        }

        trackedDarknessLights = trackedLightBuffer.ToArray();
    }

    private bool IsInDarknessLightLayer(int layer)
    {
        return (darknessLightLayers.value & (1 << layer)) != 0;
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

    private void NormalizeGroup(QuantumGroup group)
    {
        for (int i = group.Locations.Count - 1; i >= 0; i--)
        {
            if (group.Locations[i] == null)
            {
                group.Locations.RemoveAt(i);
                if (group.ActiveIndex > i)
                {
                    group.ActiveIndex--;
                }
            }
        }

        if (group.Locations.Count == 0)
        {
            group.ActiveIndex = -1;
            return;
        }

        if (group.ActiveIndex < 0 || group.ActiveIndex >= group.Locations.Count)
        {
            group.ActiveIndex = 0;
        }
    }

    private void ApplyGroupState(QuantumGroup group)
    {
        NormalizeGroup(group);

        for (int i = 0; i < group.Locations.Count; i++)
        {
            QuantumObject quantumObject = group.Locations[i];
            if (quantumObject != null)
            {
                quantumObject.SetMaterialized(i == group.ActiveIndex);
            }
        }
    }

    private sealed class QuantumGroup
    {
        public readonly List<QuantumObject> Locations = new List<QuantumObject>();
        public int ActiveIndex = -1;
    }
}
