using System.Collections.Generic;
using UnityEngine;

public sealed class AlbumManager : MonoBehaviour
{
    private static AlbumManager instance;

    private readonly HashSet<string> _collectedCards = new HashSet<string>();

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

    public IReadOnlyCollection<string> CollectedCards => _collectedCards;
    public int CollectedCount => _collectedCards.Count;

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

    public bool CollectCard(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            Debug.LogWarning("Album card id is empty.", this);
            return false;
        }

        return _collectedCards.Add(id);
    }

    public bool HasCard(string id)
    {
        return !string.IsNullOrWhiteSpace(id) && _collectedCards.Contains(id);
    }
}
