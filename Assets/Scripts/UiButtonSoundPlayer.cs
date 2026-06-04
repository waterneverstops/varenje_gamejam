using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public sealed class UiButtonSoundPlayer : MonoBehaviour
{
    [SerializeField, SoundId(SoundType.Sound)] private string soundId = "UI";

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        if (button != null)
        {
            button.onClick.AddListener(PlaySound);
        }
    }

    private void OnDisable()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(PlaySound);
        }
    }

    private void PlaySound()
    {
        if (!string.IsNullOrEmpty(soundId))
        {
            SoundManager.PlayUISound(soundId);
        }
    }
}
