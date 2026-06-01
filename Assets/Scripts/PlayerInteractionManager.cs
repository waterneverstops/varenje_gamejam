using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PlayerInteractionManager : MonoBehaviour, GameInputs.IPlayerActions
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private GameObject interactionMarker;

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
        SetMarkerVisible(false);
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
        currentInteractable = null;
        SetMarkerVisible(false);
    }

    private void Update()
    {
        if (!PlayerStateManager.Instance.CanProcessPlayerInput)
        {
            currentInteractable = null;
            SetMarkerVisible(false);
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
        currentInteractable = FindViewedInteractable();
        SetMarkerVisible(currentInteractable != null);
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

    private void SetMarkerVisible(bool visible)
    {
        if (interactionMarker != null && interactionMarker.activeSelf != visible)
        {
            interactionMarker.SetActive(visible);
        }
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
