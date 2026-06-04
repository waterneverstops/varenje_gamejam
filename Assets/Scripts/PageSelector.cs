using UnityEngine;
using UnityEngine.InputSystem;

public class PageSelector : MonoBehaviour
{
    public GameObject album;
    public GameObject[] albumHints;
    public GameObject[] pages = new GameObject[4];
    public GameObject activePage = null;
    private bool isOpen = false;

    [Header("Sound")]
    [SerializeField, SoundId(SoundType.Sound)] private string openSoundId = "Turning_Page_Open_Book";
    [SerializeField, SoundId(SoundType.Sound)] private string pageTurnSoundId = "Turning_Page_Open_Book";

    private void OnEnable()
    {
        PlayerStateManager.Instance.AlbumCollected += UpdateAlbumHintVisibility;
        UpdateAlbumHintVisibility();
    }

    private void Update()
    {
        if (!PlayerStateManager.Instance.HasAlbum)
        {
            return;
        }

        if (Keyboard.current == null || !Keyboard.current.tabKey.wasPressedThisFrame)
        {
            return;
        }

        if (isOpen)
        {
            CloseAlbum();
            return;
        }

        if (EscapeButtonManager.Instance.HasOpenWindow)
        {
            return;
        }

        OpenAlbum();
    }

    private void OnDisable()
    {
        if (PlayerStateManager.HasInstance)
        {
            PlayerStateManager.Instance.AlbumCollected -= UpdateAlbumHintVisibility;
        }

        if (isOpen)
        {
            CloseAlbum();
        }

        if (EscapeButtonManager.HasInstance)
        {
            EscapeButtonManager.Instance.UnregisterWindow(this);
        }
    }

    private void UpdateAlbumHintVisibility()
    {
        bool shouldShowAlbumHint = PlayerStateManager.Instance.HasAlbum;

        if (albumHints == null)
        {
            return;
        }

        foreach (GameObject albumHint in albumHints)
        {
            if (albumHint != null)
            {
                albumHint.SetActive(shouldShowAlbumHint);
            }
        }
    }

    public void OpenAlbum()
    {
        if (!PlayerStateManager.Instance.HasAlbum)
        {
            return;
        }

        if (isOpen || album == null || pages == null || pages.Length == 0 || pages[0] == null)
        {
            return;
        }

        isOpen = true;
        activePage = pages[0];
        EscapeButtonManager.Instance.RegisterWindow(this, CloseAlbum);

        album.SetActive(true);
        activePage.SetActive(true);

        if (!string.IsNullOrEmpty(openSoundId))
        {
            SoundManager.PlaySound(openSoundId);
        }

        if (PlayerStateManager.HasInstance)
        {
            PlayerStateManager.Instance.BlockPlayerInput(this);
        }

        Time.timeScale = 0f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void CloseAlbum()
    {
        if (!isOpen)
        {
            return;
        }

        isOpen = false;
        if (EscapeButtonManager.HasInstance)
        {
            EscapeButtonManager.Instance.UnregisterWindow(this);
        }

        album.SetActive(false);
        if (activePage != null)
        {
            activePage.SetActive(false);
        }

        activePage = null;

        if (PlayerStateManager.HasInstance)
        {
            PlayerStateManager.Instance.UnblockPlayerInput(this);
        }

        Time.timeScale = 1f;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void GoToPage(int pageIndex)
    {
        if (!isOpen || activePage == null)
        {
            return;
        }

        if (pages != null && pageIndex >= 0 && pageIndex < pages.Length && pages[pageIndex] != null && pages[pageIndex] != activePage)
        {
            activePage.SetActive(false);
            activePage = pages[pageIndex];
            activePage.SetActive(true);

            if (!string.IsNullOrEmpty(pageTurnSoundId))
            {
                SoundManager.PlaySound(pageTurnSoundId);
            }
        }
    }
}
