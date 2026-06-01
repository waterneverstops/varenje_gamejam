using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class LockWindowController : MonoBehaviour
{
    [Header("Code")]
    [SerializeField, Range(0, 9)] private int firstDigit;
    [SerializeField, Range(0, 9)] private int secondDigit;
    [SerializeField, Range(0, 9)] private int thirdDigit;

    [Header("Reels")]
    [SerializeField] private LockReel firstReel;
    [SerializeField] private LockReel secondReel;
    [SerializeField] private LockReel thirdReel;

    [Header("Buttons")]
    [SerializeField] private Button enterButton;
    [SerializeField] private Button exitButton;

    private Action solvedCallback;
    private Action closedCallback;
    private bool isOpen;

    public bool IsOpen => isOpen;

    private void OnEnable()
    {
        if (enterButton != null)
        {
            enterButton.onClick.AddListener(TryEnterCode);
        }

        if (exitButton != null)
        {
            exitButton.onClick.AddListener(Close);
        }
    }

    private void OnDisable()
    {
        if (enterButton != null)
        {
            enterButton.onClick.RemoveListener(TryEnterCode);
        }

        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(Close);
        }

        if (isOpen)
        {
            CloseWindow(false);
        }
    }

    public void Open(Action onSolved, Action onClosed)
    {
        solvedCallback = onSolved;
        closedCallback = onClosed;

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        if (!isOpen)
        {
            isOpen = true;
            EscapeButtonManager.Instance.Register(this, Close);
        }

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void Close()
    {
        CloseWindow(false);
    }

    private void TryEnterCode()
    {
        if (firstReel == null || secondReel == null || thirdReel == null)
        {
            return;
        }

        if (firstReel.CurrentDigit != firstDigit ||
            secondReel.CurrentDigit != secondDigit ||
            thirdReel.CurrentDigit != thirdDigit)
        {
            return;
        }

        CloseWindow(true);
    }

    private void CloseWindow(bool solved)
    {
        Action callback = solved ? solvedCallback : closedCallback;

        solvedCallback = null;
        closedCallback = null;

        if (isOpen)
        {
            isOpen = false;
            EscapeButtonManager.Instance.Unregister(this);
        }

        if (gameObject.activeSelf)
        {
            gameObject.SetActive(false);
        }

        callback?.Invoke();
    }

    private void OnValidate()
    {
        firstDigit = Mathf.Clamp(firstDigit, 0, 9);
        secondDigit = Mathf.Clamp(secondDigit, 0, 9);
        thirdDigit = Mathf.Clamp(thirdDigit, 0, 9);
    }
}
