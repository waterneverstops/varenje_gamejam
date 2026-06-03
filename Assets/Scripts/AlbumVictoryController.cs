using UnityEngine;

public sealed class AlbumVictoryController : MonoBehaviour
{
    [SerializeField] private string[] requiredAlbumIds = { "first_puzzle", "second_puzzle", "third_puzzle" };
    [SerializeField] private GameObject victoryWindow;
    [SerializeField] private bool pauseTimeOnVictory = true;
    [SerializeField] private bool showCursorOnVictory = true;

    private bool victoryShown;

    private void Awake()
    {
        if (victoryWindow != null)
        {
            victoryWindow.SetActive(false);
        }
    }

    private void OnEnable()
    {
        AlbumManager.Instance.IdCollected += OnAlbumIdCollected;
        CheckVictory();
    }

    private void OnDisable()
    {
        if (AlbumManager.HasInstance)
        {
            AlbumManager.Instance.IdCollected -= OnAlbumIdCollected;
        }

        if (!victoryShown && EscapeButtonManager.HasInstance)
        {
            EscapeButtonManager.Instance.UnregisterWindow(this);
        }

        if (!victoryShown && PlayerStateManager.HasInstance)
        {
            PlayerStateManager.Instance.UnblockPlayerInput(this);
        }
    }

    private void Update()
    {
        if (!victoryShown)
        {
            return;
        }

        KeepVictoryLocked();
    }

    private void OnAlbumIdCollected(string id)
    {
        CheckVictory();
    }

    private void CheckVictory()
    {
        if (victoryShown || !HasAllRequiredAlbumIds())
        {
            return;
        }

        ShowVictory();
    }

    private bool HasAllRequiredAlbumIds()
    {
        if (requiredAlbumIds == null || requiredAlbumIds.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < requiredAlbumIds.Length; i++)
        {
            if (!AlbumManager.Instance.HasId(requiredAlbumIds[i]))
            {
                return false;
            }
        }

        return true;
    }

    private void ShowVictory()
    {
        victoryShown = true;
        KeepVictoryLocked();
    }

    private void KeepVictoryLocked()
    {
        if (victoryWindow != null && !victoryWindow.activeSelf)
        {
            victoryWindow.SetActive(true);
        }

        if (PlayerStateManager.HasInstance)
        {
            PlayerStateManager.Instance.BlockPlayerInput(this);
        }

        if (EscapeButtonManager.HasInstance && !EscapeButtonManager.Instance.IsWindowRegistered(this))
        {
            EscapeButtonManager.Instance.RegisterWindow(this, KeepVictoryLocked);
        }

        if (pauseTimeOnVictory)
        {
            Time.timeScale = 0f;
        }

        if (showCursorOnVictory)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    private void OnValidate()
    {
        if (requiredAlbumIds == null || requiredAlbumIds.Length == 0)
        {
            requiredAlbumIds = new[] { "first_puzzle", "second_puzzle", "third_puzzle" };
        }
    }
}
