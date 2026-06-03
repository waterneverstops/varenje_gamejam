using UnityEngine;

public sealed class PlayerLightObjectToggle : MonoBehaviour
{
    [SerializeField] private PlayerLight playerLight;
    [SerializeField] private GameObject targetObject;
    [SerializeField] private bool activeWhenLightOn = true;
    [SerializeField] private bool autoFindPlayerLight = true;

    private bool warnedAboutMissingTarget;
    private bool warnedAboutSelfTarget;

    private void Reset()
    {
        playerLight = FindAnyObjectByType<PlayerLight>();
    }

    private void Awake()
    {
        ResolvePlayerLight();
        Refresh();
    }

    private void OnEnable()
    {
        ResolvePlayerLight();
        Refresh();
    }

    private void Update()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (targetObject == null)
        {
            WarnAboutMissingTarget();
            return;
        }

        ResolvePlayerLight();
        if (playerLight == null)
        {
            return;
        }

        bool desiredActive = playerLight.IsLightOn == activeWhenLightOn;
        if (targetObject == gameObject && !desiredActive)
        {
            WarnAboutSelfTarget();
            return;
        }

        if (targetObject.activeSelf != desiredActive)
        {
            targetObject.SetActive(desiredActive);
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

    private void WarnAboutMissingTarget()
    {
        if (warnedAboutMissingTarget)
        {
            return;
        }

        warnedAboutMissingTarget = true;
        Debug.LogWarning($"{nameof(PlayerLightObjectToggle)} needs a target object.", this);
    }

    private void WarnAboutSelfTarget()
    {
        if (warnedAboutSelfTarget)
        {
            return;
        }

        warnedAboutSelfTarget = true;
        Debug.LogWarning(
            $"{nameof(PlayerLightObjectToggle)} cannot deactivate its own GameObject and reactivate it later. Put this component on a parent or manager object, then assign the object to toggle as Target Object.",
            this);
    }
}
