using UnityEngine;
using UnityEngine.UI;

public sealed class FirstPersonControllerUi : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] private FirstPersonController controller;

    [Header("Crosshair")]
    [SerializeField] private Image crosshairImage;
    [SerializeField] private bool showCrosshair = true;
    [SerializeField] private Sprite crosshairSprite;
    [SerializeField] private Color crosshairColor = Color.white;

    [Header("Stamina")]
    [SerializeField] private Image staminaBar;
    [SerializeField] private CanvasGroup staminaBarGroup;
    [SerializeField] private bool hideWhenFull = true;
    [SerializeField] private float fadeSpeed = 6f;

    private void Reset()
    {
        controller = GetComponentInParent<FirstPersonController>();
        staminaBarGroup = GetComponentInChildren<CanvasGroup>();
    }

    private void Awake()
    {
        if (controller == null)
        {
            controller = GetComponentInParent<FirstPersonController>();
        }

        SetupCrosshair();
        UpdateStamina(true);
    }

    private void LateUpdate()
    {
        UpdateStamina(false);
    }

    private void SetupCrosshair()
    {
        if (crosshairImage == null)
        {
            return;
        }

        crosshairImage.gameObject.SetActive(showCrosshair);
        crosshairImage.color = crosshairColor;

        if (crosshairSprite != null)
        {
            crosshairImage.sprite = crosshairSprite;
        }
    }

    private void UpdateStamina(bool instant)
    {
        if (controller == null)
        {
            return;
        }

        if (staminaBar != null)
        {
            staminaBar.fillAmount = controller.StaminaNormalized;
        }

        if (staminaBarGroup == null)
        {
            return;
        }

        float targetAlpha = hideWhenFull && controller.IsStaminaFull ? 0f : 1f;
        staminaBarGroup.alpha = instant ? targetAlpha : Mathf.MoveTowards(staminaBarGroup.alpha, targetAlpha, fadeSpeed * Time.deltaTime);
    }

    private void OnValidate()
    {
        fadeSpeed = Mathf.Max(0f, fadeSpeed);
    }
}
