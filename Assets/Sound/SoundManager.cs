using Plugins.RProjects.RUtils.Scripts.Core;
using UnityEngine;

public sealed class SoundManager : SingleBehaviour<SoundManager>
{
    private const string DefaultLibraryResourcesPath = "SoundLibrary";

    [SerializeField] private SoundLibrary library;
    [SerializeField] private AudioSource soundSource;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource uiSoundSource;
    [Range(0f, 1f)] [SerializeField] private float soundVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float musicVolume = 1f;

    private string currentMusicId;

    public SoundLibrary Library => library;
    public string CurrentMusicId => currentMusicId;

    public static void SetLibrary(SoundLibrary soundLibrary)
    {
        Instance.library = soundLibrary;
    }

    public static bool PlaySound(string id)
    {
        return Instance.PlaySoundInternal(id);
    }

    public static bool PlayUISound(string id)
    {
        return Instance.PlayUISoundInternal(id);
    }

    public static bool PlayMusic(string id, bool restartIfSame = false)
    {
        return Instance.PlayMusicInternal(id, restartIfSame);
    }

    public static void StopSounds()
    {
        if (HasInstance)
        {
            Instance.StopSoundsInternal();
        }
    }

    public static void StopMusic()
    {
        if (HasInstance)
        {
            Instance.StopMusicInternal();
        }
    }

    protected override void Init()
    {
        EnsureAudioSources();
        LoadDefaultLibraryIfNeeded();
    }

    protected override void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        base.Awake();
        EnsureAudioSources();
        LoadDefaultLibraryIfNeeded();
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    private bool PlaySoundInternal(string id)
    {
        if (!TryGetEntry(id, SoundType.Sound, out SoundEntry entry))
        {
            return false;
        }

        if (entry.Clip == null)
        {
            Debug.LogWarning($"Sound '{id}' has no clip.", this);
            return false;
        }

        soundSource.pitch = entry.Pitch;
        soundSource.PlayOneShot(entry.Clip, soundVolume * entry.Volume);
        return true;
    }

    private bool PlayUISoundInternal(string id)
    {
        if (!TryGetEntry(id, SoundType.Sound, out SoundEntry entry))
        {
            return false;
        }

        if (entry.Clip == null)
        {
            Debug.LogWarning($"Sound '{id}' has no clip.", this);
            return false;
        }

        uiSoundSource.pitch = entry.Pitch;
        uiSoundSource.PlayOneShot(entry.Clip, soundVolume * entry.Volume);
        return true;
    }

    private bool PlayMusicInternal(string id, bool restartIfSame)
    {
        if (!TryGetEntry(id, SoundType.Music, out SoundEntry entry))
        {
            return false;
        }

        if (entry.Clip == null)
        {
            Debug.LogWarning($"Music '{id}' has no clip.", this);
            return false;
        }

        if (!restartIfSame && currentMusicId == id && musicSource.isPlaying)
        {
            return true;
        }

        currentMusicId = id;
        musicSource.Stop();
        musicSource.clip = entry.Clip;
        musicSource.volume = musicVolume * entry.Volume;
        musicSource.pitch = entry.Pitch;
        musicSource.loop = entry.Loop;
        musicSource.Play();
        return true;
    }

    private void StopSoundsInternal()
    {
        EnsureAudioSources();
        soundSource.Stop();
    }

    private void StopMusicInternal()
    {
        EnsureAudioSources();
        currentMusicId = null;
        musicSource.Stop();
        musicSource.clip = null;
    }

    private bool TryGetEntry(string id, SoundType type, out SoundEntry entry)
    {
        EnsureAudioSources();
        LoadDefaultLibraryIfNeeded();

        if (library == null)
        {
            Debug.LogWarning("Sound library is not assigned.", this);
            entry = null;
            return false;
        }

        if (library.TryGetEntry(id, type, out entry))
        {
            return true;
        }

        Debug.LogWarning($"Sound id '{id}' with type '{type}' was not found in {library.name}.", this);
        return false;
    }

    private void EnsureAudioSources()
    {
        if (soundSource == null)
        {
            soundSource = CreateAudioSource("Sound Source", false);
        }

        if (musicSource == null)
        {
            musicSource = CreateAudioSource("Music Source", true);
            musicSource.ignoreListenerPause = true;
        }

        if (uiSoundSource == null)
        {
            uiSoundSource = CreateAudioSource("UI Sound Source", false);
            uiSoundSource.ignoreListenerPause = true;
        }
    }

    private AudioSource CreateAudioSource(string sourceName, bool loop)
    {
        GameObject sourceObject = new GameObject(sourceName);
        sourceObject.transform.SetParent(transform);

        AudioSource source = sourceObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.loop = loop;
        return source;
    }

    private void LoadDefaultLibraryIfNeeded()
    {
        if (library != null)
        {
            return;
        }

        library = Resources.Load<SoundLibrary>(DefaultLibraryResourcesPath);
        if (library != null)
        {
            return;
        }

        SoundLibrary[] libraries = Resources.LoadAll<SoundLibrary>(string.Empty);
        if (libraries.Length > 0)
        {
            library = libraries[0];
        }
    }

    private void OnValidate()
    {
        soundVolume = Mathf.Clamp01(soundVolume);
        musicVolume = Mathf.Clamp01(musicVolume);
    }
}
