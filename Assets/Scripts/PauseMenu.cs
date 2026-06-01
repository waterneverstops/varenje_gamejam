using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public GameObject pauseMenu;
    public MonoBehaviour playerController;

    private bool isPaused = false;

    private void Start()
    {
        pauseMenu.SetActive(false);
    }

    private void OnEnable()
    {
        EscapeButtonManager.Instance.EscapePressedWithoutHandler += PauseGame;
    }

    private void OnDisable()
    {
        if (EscapeButtonManager.HasInstance)
        {
            EscapeButtonManager.Instance.EscapePressedWithoutHandler -= PauseGame;
            EscapeButtonManager.Instance.Unregister(this);
        }

        if (PlayerStateManager.HasInstance)
        {
            PlayerStateManager.Instance.UnblockPlayerInput(this);
        }
    }

    public void PauseGame()
    {
        if (isPaused)
        {
            return;
        }

        isPaused = true;
        EscapeButtonManager.Instance.Register(this, ResumeGame);
        PlayerStateManager.Instance.BlockPlayerInput(this);

        pauseMenu.SetActive(true);
        Time.timeScale = 0f;

        if (playerController != null)
            playerController.enabled = false;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void ResumeGame()
    {
        if (!isPaused)
        {
            return;
        }

        isPaused = false;
        EscapeButtonManager.Instance.Unregister(this);
        PlayerStateManager.Instance.UnblockPlayerInput(this);

        pauseMenu.SetActive(false);
        Time.timeScale = 1f;

        if (playerController != null)
            playerController.enabled = true;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void BackToMenu()
    {
        isPaused = false;
        EscapeButtonManager.Instance.Unregister(this);
        PlayerStateManager.Instance.UnblockPlayerInput(this);
        Time.timeScale = 1f;

        if (playerController != null)
            playerController.enabled = true;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        SceneManager.LoadScene("MainMenu");
    }
}
