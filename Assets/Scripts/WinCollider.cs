using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class WinCollider : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private string[] requiredCardIds = { "first_puzzle", "second_puzzle", "third_puzzle" };
    [SerializeField] private GameObject doorOpenEffect;

    private AlbumVictoryController victoryController;
    private bool isPlayerInside;

    private void Awake()
    {
        victoryController = FindObjectOfType<AlbumVictoryController>(true);
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnEnable()
    {
        AlbumManager.Instance.IdCollected += OnIdCollected;
        RefreshDoorEffect();
    }

    private void OnDisable()
    {
        if (AlbumManager.HasInstance)
        {
            AlbumManager.Instance.IdCollected -= OnIdCollected;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        isPlayerInside = true;
        TryWin();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            isPlayerInside = false;
        }
    }

    private void OnIdCollected(string id)
    {
        RefreshDoorEffect();

        if (isPlayerInside)
        {
            TryWin();
        }
    }

    private void RefreshDoorEffect()
    {
        if (doorOpenEffect == null)
        {
            return;
        }

        doorOpenEffect.SetActive(HasAllCards());
    }

    private bool HasAllCards()
    {
        if (requiredCardIds == null || requiredCardIds.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < requiredCardIds.Length; i++)
        {
            if (!AlbumManager.Instance.HasId(requiredCardIds[i]))
            {
                return false;
            }
        }

        return true;
    }

    private void TryWin()
    {
        if (victoryController != null)
        {
            victoryController.CheckVictory();
        }
    }
}
