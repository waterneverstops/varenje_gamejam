using System.Collections.Generic;
using UnityEngine;

public sealed class AlbumManager : MonoBehaviour
{
    private static AlbumManager instance;

    private readonly HashSet<string> _collectedIds = new HashSet<string>();

    public static bool HasInstance => instance != null;

    public static AlbumManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<AlbumManager>();
            }

            if (instance == null)
            {
                GameObject managerObject = new GameObject("Album Manager");
                instance = managerObject.AddComponent<AlbumManager>();
            }

            return instance;
        }
    }

    public IReadOnlyCollection<string> CollectedIds => _collectedIds;
    public int IdsCount => _collectedIds.Count;
    public event System.Action<string> IdCollected;

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

    public bool CollectId(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            Debug.LogWarning("Album id is empty.", this);
            return false;
        }

        bool isAdded = _collectedIds.Add(id);
        if (isAdded)
        {
            IdCollected?.Invoke(id);
        }

        return isAdded;
    }

    public bool HasId(string id)
    {
        return !string.IsNullOrWhiteSpace(id) && _collectedIds.Contains(id);
    }
}
