using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    public virtual bool CanInteract => isActiveAndEnabled;

    public abstract void Interact(PlayerInteractionManager interactionManager);
}
