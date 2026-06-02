using UnityEngine;

public sealed class AlbumIdComponentToggle : MonoBehaviour
{
    [SerializeField] private string id;
    [SerializeField] private bool activeWhenIdExists = true;

    public string Id => id;

    private void Reset()
    {
        id = gameObject.name;
    }

    private void Awake()
    {
        AlbumManager.Instance.IdCollected += OnIdCollected;
        Refresh();
    }

    private void OnEnable()
    {
        Refresh();
    }

    private void OnDestroy()
    {
        if (AlbumManager.HasInstance)
        {
            AlbumManager.Instance.IdCollected -= OnIdCollected;
        }
    }

    public void Refresh()
    {
        bool hasId = AlbumManager.Instance.HasId(id);
        gameObject.SetActive(hasId == activeWhenIdExists);
    }

    private void OnIdCollected(string collectedId)
    {
        if (collectedId == id)
        {
            Refresh();
        }
    }
}
