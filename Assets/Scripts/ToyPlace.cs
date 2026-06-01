using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class ToyPlace : MonoBehaviour
{
    [SerializeField] private string id = "Toy";
    [SerializeField] private bool logStateChanges;

    private readonly Dictionary<Toy, int> toyOverlapCounts = new Dictionary<Toy, int>();

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
        Collider triggerCollider = GetComponent<Collider>();
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
    }
}
