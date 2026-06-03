using UnityEngine;

public sealed class AlbumCard : Interactable
{
    [SerializeField] private string id = "Card";

    public string Id => id;
    public override InteractableHoverHintType HoverHintType => InteractableHoverHintType.AlbumAndCards;

    private void Reset()
    {
        id = gameObject.name;
    }

    public override void Interact(PlayerInteractionManager interactionManager)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return;
        }

        AlbumManager.Instance.CollectId(id);
        gameObject.SetActive(false);
    }

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            id = gameObject.name;
        }
    }
}
