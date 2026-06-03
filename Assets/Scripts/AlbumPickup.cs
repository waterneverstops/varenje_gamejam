using UnityEngine;

public sealed class AlbumPickup : Interactable
{
    [SerializeField] private bool hideWhenAlreadyCollected = true;

    public override bool CanInteract => base.CanInteract && !PlayerStateManager.Instance.HasAlbum;
    public override InteractableHoverHintType HoverHintType => InteractableHoverHintType.AlbumAndCards;

    private void OnEnable()
    {
        HideIfAlreadyCollected();
    }

    public override void Interact(PlayerInteractionManager interactionManager)
    {
        if (!CanInteract)
        {
            return;
        }

        PlayerStateManager.Instance.CollectAlbum();
        gameObject.SetActive(false);
    }

    private void HideIfAlreadyCollected()
    {
        if (hideWhenAlreadyCollected && PlayerStateManager.Instance.HasAlbum)
        {
            gameObject.SetActive(false);
        }
    }
}
