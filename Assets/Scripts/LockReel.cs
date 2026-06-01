using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class LockReel : MonoBehaviour
{
    [SerializeField] private Button upButton;
    [SerializeField] private Button downButton;
    [SerializeField] private TMP_Text digitText;
    [SerializeField, Range(0, 9)] private int currentDigit;

    public int CurrentDigit => currentDigit;

    private void OnEnable()
    {
        if (upButton != null)
        {
            upButton.onClick.AddListener(Increment);
        }

        if (downButton != null)
        {
            downButton.onClick.AddListener(Decrement);
        }

        RefreshText();
    }

    private void OnDisable()
    {
        if (upButton != null)
        {
            upButton.onClick.RemoveListener(Increment);
        }

        if (downButton != null)
        {
            downButton.onClick.RemoveListener(Decrement);
        }
    }

    public void SetDigit(int digit)
    {
        currentDigit = WrapDigit(digit);
        RefreshText();
    }

    private void Increment()
    {
        SetDigit(currentDigit + 1);
    }

    private void Decrement()
    {
        SetDigit(currentDigit - 1);
    }

    private void RefreshText()
    {
        if (digitText != null)
        {
            digitText.text = currentDigit.ToString();
        }
    }

    private static int WrapDigit(int digit)
    {
        digit %= 10;
        return digit < 0 ? digit + 10 : digit;
    }

    private void OnValidate()
    {
        currentDigit = Mathf.Clamp(currentDigit, 0, 9);
        RefreshText();
    }
}
