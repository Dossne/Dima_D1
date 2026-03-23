using UnityEngine;

public class AudioManager : MonoBehaviour
{
    private const string MusicResourcePath = "Audio/Music/8BitDungeon";
    private const string SoundEnabledKey = "SoundEnabled";
    private const string MusicVolumeKey = "MusicVolume";
    private const float DefaultMusicVolume = 0.5f;
    private const float DefaultSfxVolume = 1f;

    public static AudioManager Instance { get; private set; }

    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField] private float musicVolume = DefaultMusicVolume;
    [SerializeField] private float sfxVolume = DefaultSfxVolume;

    private AudioSource musicSource;
    private AudioSource sfxSource;
    private bool initialized;
    private bool soundEnabled = true;
    private bool missingMusicWarningShown;

    public float MusicVolume => musicVolume;
    public float SfxVolume => sfxVolume;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureAudioSources();
        InitializeIfNeeded();
    }

    public void InitializeIfNeeded()
    {
        if (!initialized)
        {
            if (backgroundMusic == null)
            {
                backgroundMusic = Resources.Load<AudioClip>(MusicResourcePath);
            }

            musicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, DefaultMusicVolume);
            sfxVolume = DefaultSfxVolume;
            initialized = true;
        }

        EnsureAudioSources();
        ApplyVolumes();
        musicSource.clip = backgroundMusic;

        if (backgroundMusic == null)
        {
            if (!missingMusicWarningShown)
            {
                Debug.LogWarning("AudioManager could not find Resources/Audio/Music/8BitDungeon. Add 8BitDungeon.mp3 to Assets/Resources/Audio/Music/.");
                missingMusicWarningShown = true;
            }

            return;
        }

        RefreshFromPrefs();
    }

    public void PlaySfx(AudioClip clip)
    {
        if (!soundEnabled || clip == null)
        {
            return;
        }

        EnsureAudioSources();
        sfxSource.PlayOneShot(clip, sfxVolume);
    }

    public void SetMusicVolume(float value)
    {
        musicVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(MusicVolumeKey, musicVolume);
        PlayerPrefs.Save();
        ApplyVolumes();
    }

    public void SetSfxVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
        ApplyVolumes();
    }

    public void RefreshFromPrefs()
    {
        musicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, DefaultMusicVolume);
        ApplyVolumes();
        SetSoundEnabled(PlayerPrefs.GetInt(SoundEnabledKey, 1) == 1);
    }

    public void SetSoundEnabled(bool enabled)
    {
        soundEnabled = enabled;
        EnsureAudioSources();

        musicSource.mute = !enabled;
        sfxSource.mute = !enabled;

        if (!enabled)
        {
            if (musicSource.isPlaying)
            {
                musicSource.Stop();
            }

            return;
        }

        if (backgroundMusic != null)
        {
            musicSource.clip = backgroundMusic;
            if (!musicSource.isPlaying)
            {
                musicSource.Play();
            }
        }
    }

    private void EnsureAudioSources()
    {
        if (musicSource == null)
        {
            musicSource = GetComponent<AudioSource>();
        }

        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
        }

        if (sfxSource == null)
        {
            AudioSource[] sources = GetComponents<AudioSource>();
            for (int i = 0; i < sources.Length; i++)
            {
                if (sources[i] != musicSource)
                {
                    sfxSource = sources[i];
                    break;
                }
            }
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
        }

        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.ignoreListenerPause = true;
        musicSource.spatialBlend = 0f;

        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
        sfxSource.ignoreListenerPause = true;
        sfxSource.spatialBlend = 0f;
    }

    private void ApplyVolumes()
    {
        EnsureAudioSources();
        musicSource.volume = musicVolume;
        sfxSource.volume = sfxVolume;
    }
}
