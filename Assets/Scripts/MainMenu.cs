using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Sound")]
    [SerializeField, SoundId(SoundType.Music)] private string menuMusicId = "MainTheme";

    private void Start()
    {
        Time.timeScale = 1f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (!string.IsNullOrEmpty(menuMusicId))
        {
            SoundManager.PlayMusic(menuMusicId);
        }
    }
    public void PlayGame()
    {
        Time.timeScale = 1f;
        SoundManager.StopMusic();
        SceneManager.LoadScene("Location_1_Bedroom");
    }
    public void ExitGame()
    {
        Debug.Log("Вы вышли из игры");
        Application.Quit();
    }
}
