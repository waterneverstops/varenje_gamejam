using DG.Tweening;
using UnityEngine;

public sealed class DoorOpener : MonoBehaviour
{
    [SerializeField] private Transform door;
    [SerializeField] private Transform openedTransform;
    [Min(0f)] [SerializeField] private float openDuration = 1f;
    [SerializeField] private Ease openEase = Ease.InOutSine;
    [SerializeField] private bool useUnscaledTime = true;

    private Tween openTween;
    private bool isOpen;

    public bool IsOpen => isOpen;

    public void Open()
    {
        if (door == null)
        {
            Debug.LogWarning($"{nameof(DoorOpener)} on {name} has no door assigned.", this);
            return;
        }

        if (openedTransform == null)
        {
            Debug.LogWarning($"{nameof(DoorOpener)} on {name} has no opened transform assigned.", this);
            return;
        }

        isOpen = true;
        openTween?.Kill();
        openTween = DOTween.Sequence()
            .SetEase(openEase)
            .Join(door.DOMove(openedTransform.position, openDuration));
    }

    private void OnDestroy()
    {
        openTween?.Kill();
    }
}