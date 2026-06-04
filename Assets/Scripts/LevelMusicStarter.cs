using UnityEngine;

public sealed class LevelMusicStarter : MonoBehaviour
{
    [SerializeField, SoundId(SoundType.Music)] private string musicId = "Rain";

    private void Start()
    {
        if (!string.IsNullOrEmpty(musicId))
        {
            SoundManager.PlayMusic(musicId);
        }
    }
}
