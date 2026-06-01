using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class ToyPlace : MonoBehaviour
{
    [SerializeField] private string id = "Toy";
    [SerializeField] private bool logStateChanges;
    [SerializeField, Min(0.02f)] private float validationInterval = 0.1f;

    private readonly Dictionary<Toy, int> toyOverlapCounts = new Dictionary<Toy, int>();
    private readonly List<Toy> staleToys = new List<Toy>();
    private Collider triggerCollider;
    private float nextValidationTime;

    public event Action<ToyPlace> StateChanged;

    public string Id => id;

    public Toy MatchingToy
    {
        get
        {
            foreach (Toy toy in toyOverlapCounts.Keys)
            {
                if (toy != null && toy.isActiveAndEnabled && toy.gameObject.activeInHierarchy && toy.Id == id)
                {
                    return toy;
                }
            }

            return null;
        }
    }

    public bool HasMatchingToy => MatchingToy != null;

    private void Reset()
    {
        id = gameObject.name;
        EnsureTriggerCollider();
    }

    private void Awake()
    {
        EnsureTriggerCollider();
    }

    private void Update()
    {
        if (Time.time < nextValidationTime)
        {
            return;
        }

        nextValidationTime = Time.time + validationInterval;
        ValidateTrackedToys();
    }

    private void OnTriggerEnter(Collider other)
    {
        Toy toy = other.GetComponentInParent<Toy>();
        if (toy == null)
        {
            return;
        }

        toyOverlapCounts.TryGetValue(toy, out int overlapCount);
        toyOverlapCounts[toy] = overlapCount + 1;

        if (overlapCount == 0)
        {
            LogStateChange($"Toy entered: {toy.Id}");
            StateChanged?.Invoke(this);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Toy toy = other.GetComponentInParent<Toy>();
        if (toy == null || !toyOverlapCounts.TryGetValue(toy, out int overlapCount))
        {
            return;
        }

        overlapCount--;
        if (overlapCount > 0)
        {
            toyOverlapCounts[toy] = overlapCount;
            return;
        }

        toyOverlapCounts.Remove(toy);
        LogStateChange($"Toy exited: {toy.Id}");
        StateChanged?.Invoke(this);
    }

    private void OnDisable()
    {
        if (toyOverlapCounts.Count == 0)
        {
            return;
        }

        toyOverlapCounts.Clear();
        LogStateChange("State cleared");
        StateChanged?.Invoke(this);
    }

    private void ValidateTrackedToys()
    {
        if (toyOverlapCounts.Count == 0)
        {
            return;
        }

        staleToys.Clear();

        foreach (Toy toy in toyOverlapCounts.Keys)
        {
            if (!IsToyActuallyInside(toy))
            {
                staleToys.Add(toy);
            }
        }

        if (staleToys.Count == 0)
        {
            return;
        }

        for (int i = 0; i < staleToys.Count; i++)
        {
            Toy staleToy = staleToys[i];
            toyOverlapCounts.Remove(staleToy);
            LogStateChange($"Toy removed by validation: {(staleToy != null ? staleToy.Id : "null")}");
        }

        StateChanged?.Invoke(this);
    }

    private bool IsToyActuallyInside(Toy toy)
    {
        if (toy == null || !toy.isActiveAndEnabled || !toy.gameObject.activeInHierarchy)
        {
            return false;
        }

        EnsureTriggerCollider();
        if (triggerCollider == null || !triggerCollider.enabled || !triggerCollider.gameObject.activeInHierarchy)
        {
            return false;
        }

        Collider[] toyColliders = toy.GetComponentsInChildren<Collider>();
        for (int i = 0; i < toyColliders.Length; i++)
        {
            Collider toyCollider = toyColliders[i];
            if (toyCollider == null || !toyCollider.enabled || !toyCollider.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (triggerCollider.bounds.Intersects(toyCollider.bounds))
            {
                return true;
            }
        }

        return false;
    }

    private void LogStateChange(string message)
    {
        if (!logStateChanges)
        {
            return;
        }

        Debug.Log($"ToyPlace '{name}' ({id}): {message}. Matching toy: {MatchingToy?.Id ?? "none"}.", this);
    }

    private void EnsureTriggerCollider()
    {
        if (triggerCollider == null)
        {
            triggerCollider = GetComponent<Collider>();
        }

        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
    }

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            id = gameObject.name;
        }

        EnsureTriggerCollider();
        validationInterval = Mathf.Max(0.02f, validationInterval);
    }
}
