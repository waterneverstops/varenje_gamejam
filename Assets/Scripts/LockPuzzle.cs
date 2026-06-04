using UnityEngine;

public sealed class LockPuzzle : Interactable
{
    [Header("Window")]
    [SerializeField] private GameObject lockWindow;
    [SerializeField] private LockWindowController lockWindowController;

    [Header("Door")]
    [SerializeField] private DoorOpener doorOpener;

    private bool isOpen;
    private bool isSolved;

    public override bool CanInteract => base.CanInteract && !isOpen && !isSolved;
    public override InteractableHoverHintType HoverHintType => InteractableHoverHintType.LockPuzzle;
    public bool IsSolved => isSolved;

    private void Reset()
    {
        FindWindowController();
        FindDoorOpener();
    }

    private void Awake()
    {
        FindWindowController();
        FindDoorOpener();

        if (lockWindow != null)
        {
            lockWindow.SetActive(false);
        }
    }

    public override void Interact(PlayerInteractionManager interactionManager)
    {
        if (!CanInteract)
        {
            return;
        }

        FindWindowController();

        if (lockWindowController == null)
        {
            Debug.LogWarning($"{nameof(LockPuzzle)} on {name} has no lock window controller.", this);
            return;
        }

        isOpen = true;
        PlayerStateManager.Instance.BlockPlayerInput(this);
        lockWindowController.Open(OnSolved, OnClosed);
    }

    private void OnClosed()
    {
        isOpen = false;
        PlayerStateManager.Instance.UnblockPlayerInput(this);
        RestoreGameCursorIfInputAllowed();
    }

    private void OnSolved()
    {
        isSolved = true;
        isOpen = false;
        PlayerStateManager.Instance.UnblockPlayerInput(this);
        RestoreGameCursorIfInputAllowed();
        OpenDoor();
        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        if (!isOpen)
        {
            return;
        }

        isOpen = false;
        if (PlayerStateManager.HasInstance)
        {
            PlayerStateManager.Instance.UnblockPlayerInput(this);
        }

        if (lockWindow != null)
        {
            lockWindow.SetActive(false);
        }
    }

    private void FindWindowController()
    {
        if (lockWindowController != null)
        {
            return;
        }

        if (lockWindow == null)
        {
            return;
        }

        lockWindowController = lockWindow.GetComponentInChildren<LockWindowController>(true);
    }

    private void RestoreGameCursorIfInputAllowed()
    {
        if (!PlayerStateManager.Instance.CanProcessPlayerInput)
        {
            return;
        }

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void OpenDoor()
    {
        FindDoorOpener();

        if (doorOpener == null)
        {
            Debug.LogWarning($"{nameof(LockPuzzle)} on {name} has no door opener assigned.", this);
            return;
        }

        doorOpener.Open();
    }

    private void FindDoorOpener()
    {
        if (doorOpener != null)
        {
            return;
        }

        doorOpener = GetComponentInChildren<DoorOpener>(true);
    }
}
