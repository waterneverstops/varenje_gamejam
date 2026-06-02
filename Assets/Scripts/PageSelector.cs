using UnityEngine;
using UnityEngine.InputSystem;

public class PageSelector : MonoBehaviour
{
    public GameObject album;
    public GameObject[] pages = new GameObject[4];
    public GameObject activePage = null;
    private bool isOpen = false;

    private void Update()
    {
        if (Keyboard.current.tabKey.wasPressedThisFrame && !isOpen)
        {
            OpenAlbum();
        }
        else if (Keyboard.current.tabKey.wasPressedThisFrame && isOpen)
        {
            CloseAlbum();
        }
    }

    public void OpenAlbum()
    {
        isOpen = true;
        activePage = pages[0];
        album.SetActive(true);
        activePage.SetActive(true);

        // Включаем паузу: блокируем ввод игрока, останавливаем время и показываем курсор
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
        isOpen = false;
        album.SetActive(false);
        activePage.SetActive(false);
        activePage = null;

        // Выключаем паузу: разблокируем ввод игрока, восстанавливаем время и скрываем курсор
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
        
        if (pageIndex >= 0 && pageIndex < pages.Length)
        {
            activePage.SetActive(false);
            activePage = pages[pageIndex];
            activePage.SetActive(true);
        }
    }
}
