using UnityEngine;

/// <summary>
/// Centralized audio controller for the game.
/// </summary>
/// <remarks>
/// This manager provides a single place to trigger game audio (SFX, UI, music, stingers).
/// It is implemented as a simple singleton and persists across scene loads.
///
/// </remarks>
public class AudioManager : MonoBehaviour
{
    /// <summary>
    /// Global singleton instance of the AudioManager.
    /// </summary>
    public static AudioManager Instance { get; private set; }

    [Header("Library")]
    [Tooltip("ScriptableObject holding references to all audio clips used by the game.")]
    [SerializeField] private SoundLibrary library;

    [Header("Volumes")]
    [Tooltip("Master volume for sound effects.")]
    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 1f;

    [Tooltip("Master volume for music.")]
    [Range(0f, 1f)]
    [SerializeField] private float musicVolume = 0.7f;

    [Header("Variation")]
    [Tooltip("Small random pitch variation for one-shot SFX to reduce repetition.")]
    [Range(0f, 0.25f)]
    [SerializeField] private float sfxPitchJitter = 0.05f;

    [Header("Audio Sources")]
    [Tooltip("Dedicated AudioSource for looping background music.")]
    [SerializeField] private AudioSource musicSource;

    [Tooltip("Dedicated AudioSource for one-shot sound effects.")]
    [SerializeField] private AudioSource sfxSource;

    [Tooltip("Dedicated AudioSource for looping timer tick bed during gameplay.")]
    [SerializeField] private AudioSource timerTickSource;


    /// <summary>
    /// Initializes the singleton, ensures required AudioSources exist, and applies initial settings.
    /// </summary>
    /// <remarks>
    /// Important: Sources are created first (if needed) and then configured. This avoids null setup issues and
    /// ensures consistent behavior across platforms/build targets.
    /// </remarks>
    private void Awake()
    {
        // Enforce singleton instance.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureAudioSources();
        ApplyVolumes();

        // Safety: ensure Unity audio output is not globally muted/paused.
        AudioListener.pause = false;
        AudioListener.volume = 1f;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log(
            $"AudioManager Awake. " +
            $"library={(library != null ? library.name : "NULL")}, " +
            $"pickupClips={(library != null && library.ornamentPickupClips != null ? library.ornamentPickupClips.Length : 0)}");
#endif
    }

    /// <summary>
    /// Ensures the AudioSources exist and are configured consistently (2D, correct looping behavior, etc.).
    /// </summary>
    /// <remarks>
    /// If sources are not assigned in the Inspector, they will be created on this GameObject.
    /// </remarks>
    private void EnsureAudioSources()
    {
        // Ensure and configure the music source.
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
        }

        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.spatialBlend = 0f; // 2D music
        musicSource.mute = false;

        // Ensure and configure the SFX source.
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
        }

        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f; // 2D SFX
        sfxSource.mute = false;

        // Ensure and configure the timer tick source (separate from one-shot SFX).
        if (timerTickSource == null)
        {
            timerTickSource = gameObject.AddComponent<AudioSource>();
        }

        timerTickSource.loop = true;
        timerTickSource.playOnAwake = false;
        timerTickSource.spatialBlend = 0f; // 2D tick bed
        timerTickSource.mute = false;

    }

    /// <summary>
    /// Applies volume sliders to the underlying music and SFX AudioSources.
    /// </summary>
    /// <remarks>
    /// Call this after changing volumes at runtime (options menu).
    /// </remarks>
    private void ApplyVolumes()
    {
        if (musicSource != null) musicSource.volume = musicVolume;
        if (sfxSource != null) sfxSource.volume = sfxVolume;
        if (timerTickSource != null) timerTickSource.volume = sfxVolume;
    }

    /// <summary>
    /// Plays one random ornament pickup sound (one-shot).
    /// </summary>
    /// <remarks>
    /// Intended to be called when the player successfully begins dragging/picking up an ornament.
    /// Uses the array in <see cref="SoundLibrary.ornamentPickupClips"/> and chooses randomly each time.
    /// </remarks>
    public void PlayOrnamentPickup()
    {
        if (library == null)
        {
            Debug.LogError("AudioManager: SoundLibrary is not assigned.");
            return;
        }

        if (library.ornamentPickupClips == null || library.ornamentPickupClips.Length == 0)
        {
            Debug.LogError("AudioManager: ornamentPickupClips is empty (assign 1+ clips in SoundLibrary).");
            return;
        }

        AudioClip clip = library.ornamentPickupClips[Random.Range(0, library.ornamentPickupClips.Length)];
        PlaySfxOneShot(clip);
    }

    /// <summary>
    /// Plays a UI click sound (one-shot).
    /// </summary>
    /// <remarks>
    /// Intended for Start/Restart/Quit button clicks and other UI interactions.
    /// </remarks>
    public void PlayUiClick()
    {
        if (library == null)
        {
            Debug.LogError("AudioManager: SoundLibrary is not assigned.");
            return;
        }

        if (library.uiClick == null || library.uiClick.Length == 0)
        {
            Debug.LogError("AudioManager: uiClick is empty (assign a clip in SoundLibrary).");
            return;
        }
        AudioClip clip = library.uiClick[Random.Range(0, library.uiClick.Length)];
        PlaySfxOneShot(clip);
    }

    /// <summary>
    /// Plays the end-of-round sting (one-shot).
    /// </summary>
    /// <remarks>
    /// Intended to be called when the gameplay timer ends and the end screen appears.
    /// </remarks>
    public void PlayGameEndSting()
    {
        if (library == null || library.gameEndSting == null)
            return;

        PlaySfxOneShot(library.gameEndSting);
    }

    /// <summary>
    /// Starts looping background music (if assigned).
    /// </summary>
    /// <remarks>
    /// Safe to call multiple times; if the same track is already playing, it will restart it.
    /// For more advanced control (fade, resume, track switching), extend this method.
    /// </remarks>
    public void PlayBackgroundMusic()
    {
        if (library == null || library.backgroundMusicLoop == null || musicSource == null)
            return;

        musicSource.clip = library.backgroundMusicLoop;
        musicSource.volume = musicVolume;
        musicSource.loop = true;
        musicSource.Play();
    }

    /// <summary>
    /// Stops background music if currently playing.
    /// </summary>
    public void StopBackgroundMusic()
    {
        if (musicSource == null)
            return;

        musicSource.Stop();
        musicSource.clip = null;
    }

    /// <summary>
    /// Plays a one-shot SFX clip using the SFX AudioSource with optional pitch jitter.
    /// </summary>
    /// <param name="clip">The audio clip to play.</param>
    /// <remarks>
    /// Pitch jitter reduces repetitiveness when the same sound is triggered frequently.
    /// Volume is controlled by <see cref="sfxVolume"/>.
    /// </remarks>
    private void PlaySfxOneShot(AudioClip clip)
    {
        if (clip == null || sfxSource == null)
            return;

        float jitter = (sfxPitchJitter > 0f) ? Random.Range(-sfxPitchJitter, sfxPitchJitter) : 0f;

        sfxSource.pitch = 1f + jitter;
        sfxSource.volume = sfxVolume;

        // One-shot playback (does not interrupt other music).
        sfxSource.PlayOneShot(clip, 1f);

        // Reset pitch so future sounds are not unintentionally affected.
        sfxSource.pitch = 1f;
    }

    /// <summary>
    /// Plays the countdown tick SFX for "3", "2", and "1".
    /// </summary>
    /// <remarks>
    /// Uses <see cref="SoundLibrary.startCountdownTick"/>.
    /// </remarks>
    public void PlayCountdownTick()
    {
        if (library == null || library.startCountdownTick == null)
            return;

        PlaySfxOneShot(library.startCountdownTick);
    }

    /// <summary>
    /// Plays the countdown "GO!" SFX.
    /// </summary>
    /// <remarks>
    /// Uses <see cref="SoundLibrary.countdownGo"/>.
    /// </remarks>
    public void PlayCountdownGo()
    {
        if (library == null || library.countdownGo == null)
            return;

        PlaySfxOneShot(library.countdownGo);
    }

    /// <summary>
    /// Starts the looping timer tick bed used during the active round.
    /// </summary>
    /// <remarks>
    /// Uses <see cref="SoundLibrary.timerTickLoop"/>. Safe to call multiple times.
    /// </remarks>
    public void PlayTimerTickLoop()
    {
        if (library == null || library.timerTickLoop == null || timerTickSource == null)
            return;

        // If already playing the correct clip, do nothing.
        if (timerTickSource.isPlaying && timerTickSource.clip == library.timerTickLoop)
            return;

        timerTickSource.clip = library.timerTickLoop;
        timerTickSource.volume = sfxVolume;
        timerTickSource.loop = true;
        timerTickSource.Play();
    }

    /// <summary>
    /// Stops the looping timer tick bed if currently playing.
    /// </summary>
    public void StopTimerTickLoop()
    {
        if (timerTickSource == null)
            return;

        timerTickSource.Stop();
        timerTickSource.clip = null;
    }


}
