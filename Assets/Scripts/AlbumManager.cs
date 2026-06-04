using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public sealed class AlbumManager : MonoBehaviour
{
    private static AlbumManager instance;

    [Header("Collection Flash")]
    [SerializeField] private CanvasGroup collectionFlashGroup;
    [Min(0f)] [SerializeField] private float flashFadeInDuration = 0.15f;
    [Min(0f)] [SerializeField] private float flashHoldDuration = 0.1f;
    [Min(0f)] [SerializeField] private float flashFadeOutDuration = 0.6f;
    [SerializeField] private Ease flashFadeInEase = Ease.OutSine;
    [SerializeField] private Ease flashFadeOutEase = Ease.InSine;
    [SerializeField] private bool useUnscaledFlashTime = true;

    [Header("Sound")]
    [SerializeField, SoundId(SoundType.Sound)] private string collectSoundId = "Pen_Writing_Book";

    private readonly HashSet<string> _collectedIds = new HashSet<string>();
    private Tween collectionFlashTween;

    public static bool HasInstance => instance != null;

    public static AlbumManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<AlbumManager>();
            }

            if (instance == null)
            {
                GameObject managerObject = new GameObject("Album Manager");
                instance = managerObject.AddComponent<AlbumManager>();
            }

            return instance;
        }
    }

    public IReadOnlyCollection<string> CollectedIds => _collectedIds;
    public int IdsCount => _collectedIds.Count;
    public event System.Action<string> IdCollected;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            enabled = false;
            return;
        }

        instance = this;

        if (collectionFlashGroup != null)
        {
            collectionFlashGroup.alpha = 0f;
        }
    }

    private void OnDestroy()
    {
        collectionFlashTween?.Kill();

        if (instance == this)
        {
            instance = null;
        }
    }

    public bool CollectId(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            Debug.LogWarning("Album id is empty.", this);
            return false;
        }

        bool isAdded = _collectedIds.Add(id);
        if (isAdded)
        {
            PlayCollectionFlash();
            if (!string.IsNullOrEmpty(collectSoundId))
            {
                SoundManager.PlaySound(collectSoundId);
            }
            IdCollected?.Invoke(id);
        }

        return isAdded;
    }

    public bool HasId(string id)
    {
        return !string.IsNullOrWhiteSpace(id) && _collectedIds.Contains(id);
    }

    public void ClearCollectedIds()
    {
        _collectedIds.Clear();
        collectionFlashTween?.Kill();

        if (collectionFlashGroup != null)
        {
            collectionFlashGroup.alpha = 0f;
        }
    }

    private void PlayCollectionFlash()
    {
        if (collectionFlashGroup == null)
        {
            return;
        }

        collectionFlashTween?.Kill();
        collectionFlashGroup.alpha = 0f;

        collectionFlashTween = DOTween.Sequence()
            .SetUpdate(useUnscaledFlashTime)
            .Append(collectionFlashGroup.DOFade(1f, flashFadeInDuration).SetEase(flashFadeInEase))
            .AppendInterval(flashHoldDuration)
            .Append(collectionFlashGroup.DOFade(0f, flashFadeOutDuration).SetEase(flashFadeOutEase));
    }

    private void OnValidate()
    {
        flashFadeInDuration = Mathf.Max(0f, flashFadeInDuration);
        flashHoldDuration = Mathf.Max(0f, flashHoldDuration);
        flashFadeOutDuration = Mathf.Max(0f, flashFadeOutDuration);
    }
}
