using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PlayerInteractionManager : MonoBehaviour, GameInputs.IPlayerActions
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private GameObject interactionMarker;

    [Header("Hover Hints")]
    [SerializeField] private GameObject albumAndCardsHoverHint;
    [SerializeField] private GameObject lockPuzzleHoverHint;

    [Header("Interaction")]
    [SerializeField, Min(0.1f)] private float interactionDistance = 3f;
    [SerializeField] private LayerMask interactionMask = ~0;
    [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

    private Interactable currentInteractable;
    private bool subscribedToInput;

    public Interactable CurrentInteractable => currentInteractable;
    public bool HasInteractable => currentInteractable != null;

    private void Reset()
    {
        playerCamera = GetComponentInChildren<Camera>();
    }

    private void Awake()
    {
        EnsureCamera();
        SetAllHoverHintsVisible(false);
    }

    private void OnEnable()
    {
        InputService.Instance.SubscribePlayer(this);
        subscribedToInput = true;
    }

    private void OnDisable()
    {
        if (subscribedToInput && InputService.HasInstance)
        {
            InputService.Instance.UnsubscribePlayer(this);
        }

        subscribedToInput = false;
        SetCurrentInteractable(null);
    }

    private void Update()
    {
        if (!PlayerStateManager.Instance.CanProcessPlayerInput)
        {
            SetCurrentInteractable(null);
            return;
        }

        RefreshCurrentInteractable();
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            TryInteract();
        }
    }

    public void TryInteract()
    {
        if (!PlayerStateManager.Instance.CanProcessPlayerInput)
        {
            return;
        }

        RefreshCurrentInteractable();

        if (currentInteractable == null)
        {
            return;
        }

        currentInteractable.Interact(this);
        RefreshCurrentInteractable();
    }

    private void RefreshCurrentInteractable()
    {
        SetCurrentInteractable(FindViewedInteractable());
    }

    private Interactable FindViewedInteractable()
    {
        EnsureCamera();

        if (playerCamera == null)
        {
            return null;
        }

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, interactionDistance, interactionMask, triggerInteraction))
        {
            return null;
        }

        Interactable interactable = hit.collider.GetComponentInParent<Interactable>();
        if (interactable == null || !interactable.CanInteract)
        {
            return null;
        }

        return interactable;
    }

    private void EnsureCamera()
    {
        if (playerCamera != null)
        {
            return;
        }

        playerCamera = GetComponentInChildren<Camera>();

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
    }

    private void SetCurrentInteractable(Interactable interactable)
    {
        if (currentInteractable == interactable)
        {
            return;
        }

        if (currentInteractable != null)
        {
            SetHoverHintVisible(currentInteractable.HoverHintType, false);
        }

        currentInteractable = interactable;

        if (currentInteractable != null)
        {
            SetHoverHintVisible(currentInteractable.HoverHintType, true);
        }
    }

    private void SetHoverHintVisible(InteractableHoverHintType hintType, bool visible)
    {
        GameObject hint = hintType switch
        {
            InteractableHoverHintType.AlbumAndCards => albumAndCardsHoverHint,
            InteractableHoverHintType.LockPuzzle => lockPuzzleHoverHint,
            _ => null
        };

        if (hint != null && hint.activeSelf != visible)
        {
            hint.SetActive(visible);
        }
    }

    private void SetAllHoverHintsVisible(bool visible)
    {
        SetHoverHintVisible(InteractableHoverHintType.AlbumAndCards, visible);
        SetHoverHintVisible(InteractableHoverHintType.LockPuzzle, visible);
    }

    private void OnValidate()
    {
        interactionDistance = Mathf.Max(0.1f, interactionDistance);
    }

    public void OnMove(InputAction.CallbackContext context)
    {
    }

    public void OnLook(InputAction.CallbackContext context)
    {
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
    }

    public void OnCrouch(InputAction.CallbackContext context)
    {
    }

    public void OnJump(InputAction.CallbackContext context)
    {
    }

    public void OnPrevious(InputAction.CallbackContext context)
    {
    }

    public void OnNext(InputAction.CallbackContext context)
    {
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
    }

    public void OnZoom(InputAction.CallbackContext context)
    {
    }

    public void OnLight(InputAction.CallbackContext context)
    {
    }
}
