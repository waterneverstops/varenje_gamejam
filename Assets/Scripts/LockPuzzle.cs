using DG.Tweening;
using UnityEngine;

public sealed class LockPuzzle : Interactable
{
    [Header("Window")]
    [SerializeField] private GameObject lockWindow;
    [SerializeField] private LockWindowController lockWindowController;

    [Header("Door")]
    [SerializeField] private GameObject door;
    [SerializeField] private Transform openedDoorTransform;
    [Min(0f)]
    [SerializeField] private float doorOpenDuration = 1f;
    [SerializeField] private Ease doorOpenEase = Ease.InOutSine;

    private bool isOpen;
    private bool isSolved;
    private Tween doorOpenTween;

    public override bool CanInteract => base.CanInteract && !isOpen && !isSolved;

    private void Reset()
    {
        FindWindowController();
    }

    private void Awake()
    {
        FindWindowController();

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
        enabled = false;
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

    private void OnDestroy()
    {
        doorOpenTween?.Kill();
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
        if (door == null || openedDoorTransform == null)
        {
            Debug.LogWarning($"{nameof(LockPuzzle)} on {name} has no door or opened door transform assigned.", this);
            return;
        }

        Transform doorTransform = door.transform;
        doorOpenTween?.Kill();
        doorOpenTween = DOTween.Sequence()
            .SetEase(doorOpenEase)
            .SetUpdate(true)
            .Join(doorTransform.DOMove(openedDoorTransform.position, doorOpenDuration))
            .Join(doorTransform.DORotateQuaternion(openedDoorTransform.rotation, doorOpenDuration));
    }
}
