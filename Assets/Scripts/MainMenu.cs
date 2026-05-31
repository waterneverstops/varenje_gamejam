using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void PlayGame()
    {
        SceneManager.LoadScene("Location_1_Bedroom");
    }
    public void ExitGame()
    {
        Debug.Log("Вы вышли из игры");
        Application.Quit();
    }
}
