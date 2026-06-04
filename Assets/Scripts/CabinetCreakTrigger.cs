using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class CabinetCreakTrigger : MonoBehaviour
{
    [SerializeField] private LockPuzzle lockPuzzle;
    [SerializeField] private string playerTag = "Player";

    [Header("Cooldown (seconds)")]
    [SerializeField, Min(0f)] private float cooldown = 10f;

    [Header("Sounds")]
    [SerializeField, SoundId(SoundType.Sound)] private string[] creakIds =
    {
        "Skrezhet1",
        "Skrezhet2"
    };

    private float nextAllowedTime;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        if (lockPuzzle != null && lockPuzzle.IsSolved)
        {
            return;
        }

        if (Time.time < nextAllowedTime)
        {
            return;
        }

        if (creakIds == null || creakIds.Length == 0)
        {
            return;
        }

        string id = creakIds[Random.Range(0, creakIds.Length)];
        if (string.IsNullOrEmpty(id))
        {
            return;
        }

        SoundManager.PlaySound(id);
        nextAllowedTime = Time.time + cooldown;
    }
}
