using System.Collections;
using UnityEngine;
using UnityEngine.Formats.Alembic.Importer;

public sealed class ToysManager : MonoBehaviour
{
    [SerializeField] private ToyPlace[] toyPlaces = new ToyPlace[3];

    [SerializeField] private AlembicStreamPlayer solvedAnimationPlayer;
    [SerializeField] private bool activateAnimationObjectOnSolved = true;
    [SerializeField] private bool rewindAnimationOnSolved = true;
    [SerializeField, Min(0.01f)] private float animationPlaybackSpeed = 1f;

    [Header("Sound")]
    [SerializeField, SoundId(SoundType.Sound)] private string solvedSoundId = "Cho-choo";

    private bool solved;
    private Coroutine solvedAnimationRoutine;

    private void OnEnable()
    {
        SubscribePlaces();
        CheckSolved();
    }

    private void OnDisable()
    {
        UnsubscribePlaces();

        if (solvedAnimationRoutine != null)
        {
            StopCoroutine(solvedAnimationRoutine);
            solvedAnimationRoutine = null;
        }
    }

    private void OnPlaceStateChanged(ToyPlace place)
    {
        CheckSolved();
    }

    private void CheckSolved()
    {
        if (solved || !AreAllPlacesSolved())
        {
            return;
        }

        solved = true;

        if (!string.IsNullOrEmpty(solvedSoundId))
        {
            SoundManager.PlaySound(solvedSoundId);
        }

        PlaySolvedAnimation();
    }

    private void PlaySolvedAnimation()
    {
        if (solvedAnimationPlayer == null)
        {
            return;
        }

        if (solvedAnimationRoutine != null)
        {
            StopCoroutine(solvedAnimationRoutine);
        }

        if (activateAnimationObjectOnSolved)
        {
            solvedAnimationPlayer.gameObject.SetActive(true);
        }

        solvedAnimationRoutine = StartCoroutine(PlaySolvedAnimationRoutine());
    }

    private IEnumerator PlaySolvedAnimationRoutine()
    {
        float time = rewindAnimationOnSolved ? 0f : solvedAnimationPlayer.CurrentTime;
        float duration = solvedAnimationPlayer.Duration;

        if (duration <= 0f || float.IsNaN(duration) || float.IsInfinity(duration))
        {
            solvedAnimationPlayer.UpdateImmediately(time);
            solvedAnimationRoutine = null;
            yield break;
        }

        while (time < duration)
        {
            solvedAnimationPlayer.UpdateImmediately(time);
            time += Time.deltaTime * animationPlaybackSpeed;
            yield return null;
        }

        solvedAnimationPlayer.UpdateImmediately(duration);
        solvedAnimationRoutine = null;
    }

    private void OnValidate()
    {
        animationPlaybackSpeed = Mathf.Max(0.01f, animationPlaybackSpeed);
    }

    private bool AreAllPlacesSolved()
    {
        if (toyPlaces == null || toyPlaces.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < toyPlaces.Length; i++)
        {
            ToyPlace place = toyPlaces[i];
            if (place == null || !place.HasMatchingToy)
            {
                return false;
            }
        }

        return true;
    }

    private void SubscribePlaces()
    {
        if (toyPlaces == null)
        {
            return;
        }

        for (int i = 0; i < toyPlaces.Length; i++)
        {
            ToyPlace place = toyPlaces[i];
            if (place != null)
            {
                place.StateChanged += OnPlaceStateChanged;
            }
        }
    }

    private void UnsubscribePlaces()
    {
        if (toyPlaces == null)
        {
            return;
        }

        for (int i = 0; i < toyPlaces.Length; i++)
        {
            ToyPlace place = toyPlaces[i];
            if (place != null)
            {
                place.StateChanged -= OnPlaceStateChanged;
            }
        }
    }
}
