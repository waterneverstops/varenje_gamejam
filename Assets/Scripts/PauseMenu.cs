using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public GameObject pauseMenu;
    public MonoBehaviour playerController;

    private bool isPaused = false;

    private void Awake()
    {
        ResetAlbumProgress();
    }

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
            EscapeButtonManager.Instance.UnregisterWindow(this);
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
        EscapeButtonManager.Instance.RegisterWindow(this, ResumeGame);
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
        EscapeButtonManager.Instance.UnregisterWindow(this);
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
        ResetPauseState(true);
        SceneManager.LoadScene("MainMenu");
    }

    public void RestartLevel()
    {
        ResetPauseState(false);
        SceneManager.sceneLoaded -= ResetInputStateAfterSceneLoad;
        SceneManager.sceneLoaded += ResetInputStateAfterSceneLoad;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void ResetPauseState(bool showCursor)
    {
        isPaused = false;

        if (EscapeButtonManager.HasInstance)
        {
            EscapeButtonManager.Instance.ClearWindows();
        }

        if (PlayerStateManager.HasInstance)
        {
            PlayerStateManager.Instance.ClearInputBlockers();
        }

        if (InputService.HasInstance)
        {
            InputService.Instance.RefreshPlayerInputState();
        }

        pauseMenu.SetActive(false);
        Time.timeScale = 1f;

        if (playerController != null)
        {
            playerController.enabled = true;
        }

        Cursor.visible = showCursor;
        Cursor.lockState = showCursor ? CursorLockMode.None : CursorLockMode.Locked;
    }

    private static void ResetInputStateAfterSceneLoad(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= ResetInputStateAfterSceneLoad;
        Time.timeScale = 1f;

        if (EscapeButtonManager.HasInstance)
        {
            EscapeButtonManager.Instance.ClearWindows();
        }

        if (PlayerStateManager.HasInstance)
        {
            PlayerStateManager.Instance.ClearInputBlockers();
        }

        if (InputService.HasInstance)
        {
            InputService.Instance.RefreshPlayerInputState();
        }
    }

    private static void ResetAlbumProgress()
    {
        if (PlayerStateManager.HasInstance)
        {
            PlayerStateManager.Instance.ResetAlbum();
        }

        if (AlbumManager.HasInstance)
        {
            AlbumManager.Instance.ClearCollectedIds();
        }
    }
}
