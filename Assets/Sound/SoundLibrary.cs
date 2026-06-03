using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SoundLibrary", menuName = "Audio/Sound Library")]
public sealed class SoundLibrary : ScriptableObject
{
    [SerializeField] private List<SoundEntry> entries = new List<SoundEntry>();

    public IReadOnlyList<SoundEntry> Entries => entries;

    public bool TryGetEntry(string id, out SoundEntry entry)
    {
        return TryGetEntry(id, null, out entry);
    }

    public bool TryGetEntry(string id, SoundType type, out SoundEntry entry)
    {
        return TryGetEntry(id, (SoundType?)type, out entry);
    }

    private bool TryGetEntry(string id, SoundType? type, out SoundEntry entry)
    {
        entry = null;

        if (string.IsNullOrWhiteSpace(id))
        {
            return false;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            SoundEntry current = entries[i];
            if (current == null || string.IsNullOrWhiteSpace(current.Id))
            {
                continue;
            }

            if (type.HasValue && current.Type != type.Value)
            {
                continue;
            }

            if (current.Id == id)
            {
                entry = current;
                return true;
            }
        }

        return false;
    }
}
