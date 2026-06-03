using System;
using UnityEngine;

[Serializable]
public sealed class SoundEntry
{
    [SerializeField] private string id;
    [SerializeField] private SoundType type = SoundType.Sound;
    [SerializeField] private AudioClip clip;
    [Range(0f, 1f)] [SerializeField] private float volume = 1f;
    [Range(0.1f, 3f)] [SerializeField] private float pitch = 1f;
    [SerializeField] private bool loop;

    public string Id => id;
    public SoundType Type => type;
    public AudioClip Clip => clip;
    public float Volume => volume;
    public float Pitch => pitch;
    public bool Loop => loop;
}
