using UnityEngine;

public sealed class PlayerLightChargesView : MonoBehaviour
{
    [SerializeField] private PlayerLight playerLight;
    [SerializeField] private Transform lightsRoot;
    [SerializeField] private GameObject[] chargeLights;
    [SerializeField] private bool autoFindPlayerLight = true;
    [SerializeField] private bool useDirectChildrenWhenEmpty = true;

    private PlayerLight subscribedPlayerLight;
    private int lastCharges = -1;
    private int lastMaxCharges = -1;

    private void Reset()
    {
        lightsRoot = transform;
        playerLight = FindAnyObjectByType<PlayerLight>();
        ResolveChargeLights();
    }

    private void Awake()
    {
        if (lightsRoot == null)
        {
            lightsRoot = transform;
        }

        ResolveChargeLights();
        ResolvePlayerLight();
        Refresh();
    }

    private void OnEnable()
    {
        ResolveChargeLights();
        ResolvePlayerLight();
        SubscribeToPlayerLight();
        Refresh();
    }

    private void OnDisable()
    {
        UnsubscribeFromPlayerLight();
    }

    private void Update()
    {
        ResolvePlayerLight();
        SubscribeToPlayerLight();

        if (playerLight != null
            && (lastCharges != playerLight.ChargesRemaining || lastMaxCharges != playerLight.MaxCharges))
        {
            Refresh();
        }
    }

    public void Refresh()
    {
        ResolveChargeLights();

        int chargesRemaining = playerLight != null ? playerLight.ChargesRemaining : 0;
        int maxCharges = playerLight != null ? playerLight.MaxCharges : 0;
        int lightCount = chargeLights != null ? chargeLights.Length : 0;
        int activeCount = GetActiveLightCount(chargesRemaining, maxCharges, lightCount);

        for (int i = 0; i < lightCount; i++)
        {
            GameObject chargeLight = chargeLights[i];
            bool shouldBeActive = i < activeCount;
            if (chargeLight != null && chargeLight.activeSelf != shouldBeActive)
            {
                chargeLight.SetActive(shouldBeActive);
            }
        }

        lastCharges = chargesRemaining;
        lastMaxCharges = maxCharges;
    }

    private void ResolvePlayerLight()
    {
        if (playerLight != null || !autoFindPlayerLight)
        {
            return;
        }

        playerLight = FindAnyObjectByType<PlayerLight>();
    }

    private int GetActiveLightCount(int chargesRemaining, int maxCharges, int lightCount)
    {
        if (chargesRemaining <= 0 || maxCharges <= 0 || lightCount <= 0)
        {
            return 0;
        }

        float chargeRatio = chargesRemaining / (float)maxCharges;
        return Mathf.Clamp(Mathf.CeilToInt(chargeRatio * lightCount), 1, lightCount);
    }

    private void ResolveChargeLights()
    {
        if (!useDirectChildrenWhenEmpty || (chargeLights != null && chargeLights.Length > 0))
        {
            return;
        }

        Transform root = lightsRoot != null ? lightsRoot : transform;
        chargeLights = new GameObject[root.childCount];
        for (int i = 0; i < root.childCount; i++)
        {
            chargeLights[i] = root.GetChild(i).gameObject;
        }
    }

    private void SubscribeToPlayerLight()
    {
        if (subscribedPlayerLight == playerLight)
        {
            return;
        }

        UnsubscribeFromPlayerLight();
        subscribedPlayerLight = playerLight;

        if (subscribedPlayerLight != null)
        {
            subscribedPlayerLight.ChargesChanged += OnChargesChanged;
        }
    }

    private void UnsubscribeFromPlayerLight()
    {
        if (subscribedPlayerLight == null)
        {
            return;
        }

        subscribedPlayerLight.ChargesChanged -= OnChargesChanged;
        subscribedPlayerLight = null;
    }

    private void OnChargesChanged(int chargesRemaining, int maxCharges)
    {
        Refresh();
    }
}
