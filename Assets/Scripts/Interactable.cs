using UnityEngine;

public enum InteractableHoverHintType
{
    None,
    AlbumAndCards,
    LockPuzzle
}

public abstract class Interactable : MonoBehaviour
{
    public virtual bool CanInteract => isActiveAndEnabled;
    public virtual InteractableHoverHintType HoverHintType => InteractableHoverHintType.None;

    public abstract void Interact(PlayerInteractionManager interactionManager);
}
