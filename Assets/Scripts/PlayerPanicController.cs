using UnityEngine;
using UnityEngine.UI;

public sealed class PlayerPanicController : MonoBehaviour
{
    [SerializeField] private PlayerLight playerLight;
    [SerializeField] private Image panicEffectImage;
    [SerializeField] private GameObject loseWindow;
    [SerializeField] private bool autoFindPlayerLight = true;

    [Header("Panic")]
    [SerializeField, Min(0.01f)] private float timeToFullPanic = 20f;
    [SerializeField, Min(0.01f)] private float recoveryTime = 8f;

    [Header("Album IDs")]
    [SerializeField] private string panicAlbumId = "panic";
    [SerializeField, Range(0f, 1f)] private float panicAlbumThreshold = 0.2f;

    [Header("Loss")]
    [SerializeField] private bool pauseTimeOnLoss = true;
    [SerializeField] private bool showCursorOnLoss = true;

    private float panic;
    private bool lossShown;
    private bool panicAlbumIdCollected;

    public float Panic => panic;
    public bool IsLossShown => lossShown;

    private void Reset()
    {
        playerLight = FindAnyObjectByType<PlayerLight>();
        ResolveUiReferences();
    }

    private void Awake()
    {
        ResolvePlayerLight();
        ResolveUiReferences();

        if (panicEffectImage != null)
        {
            panicEffectImage.raycastTarget = false;
        }

        if (loseWindow != null)
        {
            loseWindow.SetActive(false);
        }

        ApplyPanicEffect();
    }

    private void OnEnable()
    {
        ResolvePlayerLight();
        ApplyPanicEffect();
    }

    private void OnDisable()
    {
        if (!lossShown && EscapeButtonManager.HasInstance)
        {
            EscapeButtonManager.Instance.UnregisterWindow(this);
        }

        if (!lossShown && PlayerStateManager.HasInstance)
        {
            PlayerStateManager.Instance.UnblockPlayerInput(this);
        }
    }

    private void Update()
    {
        if (lossShown)
        {
            KeepLossLocked();
            return;
        }

        ResolvePlayerLight();
        TickPanic(Time.deltaTime);
        TryCollectPanicAlbumId();
        ApplyPanicEffect();

        if (panic >= 1f)
        {
            ShowLoss();
        }
    }

    private void TickPanic(float deltaTime)
    {
        bool lightIsOn = playerLight != null && playerLight.IsLightOn;
        float rate = lightIsOn ? -1f / recoveryTime : 1f / timeToFullPanic;
        panic = Mathf.Clamp01(panic + rate * deltaTime);
    }

    private void ApplyPanicEffect()
    {
        if (panicEffectImage == null)
        {
            return;
        }

        float intensity = Mathf.SmoothStep(0f, 1f, panic);
        Color color = Color.white;
        color.a *= intensity;
        panicEffectImage.color = color;

        GameObject effectObject = panicEffectImage.gameObject;
        bool shouldBeActive = lossShown || intensity > 0.001f;
        if (effectObject.activeSelf != shouldBeActive)
        {
            effectObject.SetActive(shouldBeActive);
        }
    }

    private void TryCollectPanicAlbumId()
    {
        if (panicAlbumIdCollected || panic < panicAlbumThreshold)
        {
            return;
        }

        panicAlbumIdCollected = true;
        AlbumManager.Instance.CollectId(panicAlbumId);
    }

    private void ShowLoss()
    {
        if (lossShown)
        {
            return;
        }

        lossShown = true;
        panic = 1f;
        ApplyPanicEffect();
        KeepLossLocked();
    }

    private void KeepLossLocked()
    {
        if (loseWindow != null && !loseWindow.activeSelf)
        {
            loseWindow.SetActive(true);
        }

        if (PlayerStateManager.HasInstance)
        {
            PlayerStateManager.Instance.BlockPlayerInput(this);
        }

        if (EscapeButtonManager.HasInstance && !EscapeButtonManager.Instance.IsWindowRegistered(this))
        {
            EscapeButtonManager.Instance.RegisterWindow(this, KeepLossLocked);
        }

        if (pauseTimeOnLoss)
        {
            Time.timeScale = 0f;
        }

        if (showCursorOnLoss)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    private void ResolvePlayerLight()
    {
        if (playerLight != null || !autoFindPlayerLight)
        {
            return;
        }

        playerLight = FindAnyObjectByType<PlayerLight>();
    }

    private void ResolveUiReferences()
    {
        if (panicEffectImage == null)
        {
            Transform effectTransform = transform.Find("BlackLayer/BlackLayerWithoutLight");
            if (effectTransform == null)
            {
                effectTransform = FindChildByName(transform, "BlackLayerWithoutLight");
            }

            if (effectTransform != null)
            {
                panicEffectImage = effectTransform.GetComponent<Image>();
            }
        }

        if (loseWindow == null)
        {
            Transform loseTransform = FindChildByName(transform, "LoseMenu");
            if (loseTransform != null)
            {
                loseWindow = loseTransform.gameObject;
            }
        }
    }

    private Transform FindChildByName(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == childName)
            {
                return child;
            }

            Transform result = FindChildByName(child, childName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    private void OnValidate()
    {
        timeToFullPanic = Mathf.Max(0.01f, timeToFullPanic);
        recoveryTime = Mathf.Max(0.01f, recoveryTime);
        panicAlbumThreshold = Mathf.Clamp01(panicAlbumThreshold);
    }
}
