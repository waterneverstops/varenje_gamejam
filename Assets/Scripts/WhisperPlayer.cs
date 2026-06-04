using System.Collections;
using UnityEngine;

public sealed class WhisperPlayer : MonoBehaviour
{
    [SerializeField] private PlayerLight playerLight;
    [SerializeField] private bool autoFindPlayerLight = true;

    [Header("Interval (seconds)")]
    [SerializeField, Min(0f)] private float minInterval = 5f;
    [SerializeField, Min(0f)] private float maxInterval = 10f;

    [Header("Sounds")]
    [SerializeField, SoundId(SoundType.Sound)] private string[] whisperIds =
    {
        "Random_Wisper_1",
        "Random_Wisper_2",
        "Whisper_Many_Voices"
    };

    private Coroutine routine;

    private void OnEnable()
    {
        ResolvePlayerLight();
        routine = StartCoroutine(WhisperLoop());
    }

    private void OnDisable()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
    }

    private IEnumerator WhisperLoop()
    {
        while (true)
        {
            float wait = Random.Range(minInterval, maxInterval);
            yield return new WaitForSeconds(wait);

            if (CanPlayWhisper())
            {
                PlayRandomWhisper();
            }
        }
    }

    private bool CanPlayWhisper()
    {
        if (whisperIds == null || whisperIds.Length == 0)
        {
            return false;
        }

        if (playerLight == null)
        {
            return false;
        }

        return !playerLight.IsLightOn;
    }

    private void PlayRandomWhisper()
    {
        string id = whisperIds[Random.Range(0, whisperIds.Length)];
        if (!string.IsNullOrEmpty(id))
        {
            SoundManager.PlaySound(id);
        }
    }

    private void ResolvePlayerLight()
    {
        if (playerLight != null || !autoFindPlayerLight)
        {
            return;
        }

        playerLight = FindAnyObjectByType<PlayerLight>();
    }

    private void OnValidate()
    {
        if (maxInterval < minInterval)
        {
            maxInterval = minInterval;
        }
    }
}
