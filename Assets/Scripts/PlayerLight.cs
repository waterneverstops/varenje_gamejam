using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PlayerLight : MonoBehaviour, GameInputs.IPlayerActions
{
    [SerializeField] private Light targetLight;
    [SerializeField] private bool startEnabled;

    private bool subscribedToInput;

    private void Reset()
    {
        targetLight = GetComponentInChildren<Light>(true);
    }

    private void Awake()
    {
        if (targetLight == null)
        {
            targetLight = GetComponentInChildren<Light>(true);
        }

        SetLightEnabled(startEnabled);
    }

    private void OnEnable()
    {
        InputService.Instance.SubscribePlayer(this);
        subscribedToInput = true;
    }

    private void OnDisable()
    {
        if (subscribedToInput && InputService.HasInstance)
        {
            InputService.Instance.UnsubscribePlayer(this);
        }

        subscribedToInput = false;
    }

    public void OnLight(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            ToggleLight();
        }
    }

    public void ToggleLight()
    {
        if (targetLight == null)
        {
            return;
        }

        SetLightEnabled(!targetLight.enabled);
    }

    private void SetLightEnabled(bool value)
    {
        if (targetLight != null)
        {
            targetLight.enabled = value;
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
    }

    public void OnLook(InputAction.CallbackContext context)
    {
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
    }

    public void OnCrouch(InputAction.CallbackContext context)
    {
    }

    public void OnJump(InputAction.CallbackContext context)
    {
    }

    public void OnPrevious(InputAction.CallbackContext context)
    {
    }

    public void OnNext(InputAction.CallbackContext context)
    {
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
    }

    public void OnZoom(InputAction.CallbackContext context)
    {
    }
}
