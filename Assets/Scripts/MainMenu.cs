using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    private void Start()
    {
        Time.timeScale = 1f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
    public void PlayGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Location_1_Bedroom");
    }
    public void ExitGame()
    {
        Debug.Log("Вы вышли из игры");
        Application.Quit();
    }
}
