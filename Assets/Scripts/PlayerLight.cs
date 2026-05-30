using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PlayerLight : MonoBehaviour, GameInputs.IPlayerActions
{
    [SerializeField] private Light targetLight;
    [SerializeField] private bool startEnabled;

    [Header("Charges")]
    [SerializeField, Min(0)] private int maxCharges = 5;

    [Header("Charge Color")]
    [SerializeField] private Color fullChargeColor = Color.white;
    [SerializeField] private Color emptyChargeColor = new Color(1f, 0.45f, 0f);

    [Header("Startup Delay")]
    [SerializeField, Min(0f)] private float minStartupDelay = 0.05f;
    [SerializeField, Min(0f)] private float maxStartupDelay = 0.8f;
    [SerializeField, Range(0f, 1f)] private float startupFlickerIntensityMin = 0.08f;
    [SerializeField, Range(0f, 1f)] private float startupFlickerIntensityMax = 0.35f;
    [SerializeField, Min(0.01f)] private float startupFlickerIntervalMin = 0.03f;
    [SerializeField, Min(0.01f)] private float startupFlickerIntervalMax = 0.12f;

    [Header("Flicker")]
    [SerializeField, Range(0f, 1f)] private float flickerIntensityVariance = 0.12f;
    [SerializeField, Min(0.01f)] private float flickerIntervalMin = 0.04f;
    [SerializeField, Min(0.01f)] private float flickerIntervalMax = 0.12f;

    private int chargesRemaining;
    private float baseIntensity;
    private Coroutine startupRoutine;
    private Coroutine flickerRoutine;
    private bool subscribedToInput;

    public int ChargesRemaining => chargesRemaining;
    public int MaxCharges => maxCharges;
    public bool IsLightOn => targetLight != null && targetLight.enabled;

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

        chargesRemaining = maxCharges;
        baseIntensity = targetLight != null ? targetLight.intensity : 0f;

        ApplyChargeColor();
        SetLightEnabled(startEnabled && chargesRemaining > 0);
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
        StopLightRoutines();
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

        if (targetLight.enabled || startupRoutine != null)
        {
            StopStartupRoutine();
            SetLightEnabled(false);
            return;
        }

        TryStartLight();
    }

    private void TryStartLight()
    {
        if (chargesRemaining <= 0)
        {
            SetLightEnabled(false);
            return;
        }

        int chargeIndex = maxCharges - chargesRemaining;
        float startupDelay = GetStartupDelay(chargeIndex);

        chargesRemaining--;
        ApplyChargeColor();

        startupRoutine = StartCoroutine(StartLightAfterFlicker(startupDelay));
    }

    private void SetLightEnabled(bool value)
    {
        if (targetLight != null)
        {
            targetLight.enabled = value;

            if (value)
            {
                StartFlicker();
            }
            else
            {
                StopFlicker();
                ResetLightIntensity();
            }
        }
    }

    private IEnumerator StartLightAfterFlicker(float startupDelay)
    {
        StopFlicker();
        targetLight.enabled = true;

        float elapsed = 0f;
        while (elapsed < startupDelay)
        {
            targetLight.intensity = GetRandomStartupFlickerIntensity();

            float interval = Random.Range(startupFlickerIntervalMin, startupFlickerIntervalMax);
            yield return new WaitForSeconds(interval);

            elapsed += interval;
        }

        startupRoutine = null;
        SetLightEnabled(true);
    }

    private void StartFlicker()
    {
        if (flickerRoutine != null || flickerIntensityVariance <= 0f)
        {
            return;
        }

        flickerRoutine = StartCoroutine(Flicker());
    }

    private IEnumerator Flicker()
    {
        while (targetLight != null && targetLight.enabled)
        {
            targetLight.intensity = GetRandomFlickerIntensity();
            yield return new WaitForSeconds(Random.Range(flickerIntervalMin, flickerIntervalMax));
        }

        flickerRoutine = null;
    }

    private float GetRandomFlickerIntensity()
    {
        float variance = baseIntensity * flickerIntensityVariance;
        return Random.Range(baseIntensity - variance, baseIntensity + variance);
    }

    private float GetRandomStartupFlickerIntensity()
    {
        float minMultiplier = Mathf.Min(startupFlickerIntensityMin, startupFlickerIntensityMax);
        float maxMultiplier = Mathf.Max(startupFlickerIntensityMin, startupFlickerIntensityMax);
        return baseIntensity * Random.Range(minMultiplier, maxMultiplier);
    }

    private float GetStartupDelay(int chargeIndex)
    {
        int chargeCount = Mathf.Max(1, maxCharges);
        float minDelay = Mathf.Min(minStartupDelay, maxStartupDelay);
        float maxDelay = Mathf.Max(minStartupDelay, maxStartupDelay);
        float step = (maxDelay - minDelay) / chargeCount;
        int clampedChargeIndex = Mathf.Clamp(chargeIndex, 0, chargeCount - 1);
        float delayMin = minDelay + step * clampedChargeIndex;
        float delayMax = clampedChargeIndex == chargeCount - 1 ? maxDelay : delayMin + step;

        return Random.Range(delayMin, delayMax);
    }

    private void ApplyChargeColor()
    {
        if (targetLight == null)
        {
            return;
        }

        float chargeRatio = maxCharges > 0 ? chargesRemaining / (float)maxCharges : 0f;
        targetLight.color = Color.Lerp(emptyChargeColor, fullChargeColor, chargeRatio);
    }

    private void StopLightRoutines()
    {
        StopStartupRoutine();
        StopFlicker();
        ResetLightIntensity();
    }

    private void StopStartupRoutine()
    {
        if (startupRoutine == null)
        {
            return;
        }

        StopCoroutine(startupRoutine);
        startupRoutine = null;
    }

    private void StopFlicker()
    {
        if (flickerRoutine == null)
        {
            return;
        }

        StopCoroutine(flickerRoutine);
        flickerRoutine = null;
    }

    private void ResetLightIntensity()
    {
        if (targetLight != null)
        {
            targetLight.intensity = baseIntensity;
        }
    }

    private void OnValidate()
    {
        if (maxStartupDelay < minStartupDelay)
        {
            maxStartupDelay = minStartupDelay;
        }

        if (startupFlickerIntervalMax < startupFlickerIntervalMin)
        {
            startupFlickerIntervalMax = startupFlickerIntervalMin;
        }

        if (startupFlickerIntensityMax < startupFlickerIntensityMin)
        {
            startupFlickerIntensityMax = startupFlickerIntensityMin;
        }

        if (flickerIntervalMax < flickerIntervalMin)
        {
            flickerIntervalMax = flickerIntervalMin;
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
