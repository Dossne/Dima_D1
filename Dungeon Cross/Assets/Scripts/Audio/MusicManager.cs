using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    private const string MusicResourcePath = "Audio/Music/8BitDungeon";
    private const string SoundEnabledKey = "SoundEnabled";

    private AudioSource audioSource;
    private AudioClip backgroundClip;
    private bool initialized;
    private bool missingClipWarningShown;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureAudioSource();
        InitializeIfNeeded();
    }

    public void InitializeIfNeeded()
    {
        if (!initialized)
        {
            backgroundClip = Resources.Load<AudioClip>(MusicResourcePath);
            initialized = true;
        }

        EnsureAudioSource();
        audioSource.clip = backgroundClip;

        if (backgroundClip == null)
        {
            if (!missingClipWarningShown)
            {
                Debug.LogWarning("MusicManager could not find Resources/Audio/Music/8BitDungeon. Add 8BitDungeon.mp3 to Assets/Resources/Audio/Music/.");
                missingClipWarningShown = true;
            }

            return;
        }

        RefreshFromPrefs();
    }

    public void RefreshFromPrefs()
    {
        bool enabled = PlayerPrefs.GetInt(SoundEnabledKey, 1) == 1;
        SetSoundEnabled(enabled);
    }

    public void SetSoundEnabled(bool enabled)
    {
        EnsureAudioSource();

        if (backgroundClip == null)
        {
            audioSource.Stop();
            return;
        }

        audioSource.mute = !enabled;

        if (!enabled)
        {
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
            }

            return;
        }

        if (!audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    private void EnsureAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.ignoreListenerPause = true;
        audioSource.spatialBlend = 0f;
        audioSource.volume = 0.8f;
    }
}
