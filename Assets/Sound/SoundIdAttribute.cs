using UnityEngine;

public sealed class SoundIdAttribute : PropertyAttribute
{
    public SoundType Type { get; }
    public bool FilterByType { get; }

    public SoundIdAttribute()
    {
        FilterByType = false;
        Type = SoundType.Sound;
    }

    public SoundIdAttribute(SoundType type)
    {
        FilterByType = true;
        Type = type;
    }
}
