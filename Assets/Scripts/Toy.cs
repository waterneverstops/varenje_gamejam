using UnityEngine;

public sealed class Toy : MonoBehaviour
{
    [SerializeField] private string id = "Toy";

    public string Id => id;

    private void Reset()
    {
        id = gameObject.name;
    }

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            id = gameObject.name;
        }
    }
}
