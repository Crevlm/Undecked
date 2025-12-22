using UnityEngine;
using UnityEngine.InputSystem;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Library")]
    [SerializeField] private SoundLibrary library;

    [Header("Volumes")]
    [Range(0f, 1f)][SerializeField] private float sfxVolume = 1f;
    [Range(0f, 1f)][SerializeField] private float musicVolume = 0.7f;

    [Header("Variation")]
    [Tooltip("Small random pitch variation to reduce repetition.")]
    [Range(0f, 0.25f)][SerializeField] private float sfxPitchJitter = 0.05f;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource; // dedicated music source
    [SerializeField] private AudioSource sfxSource;   // one-shot SFX source

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Ensure sources exist even if not assigned.
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f;   // 0 = 2D
            sfxSource.rolloffMode = AudioRolloffMode.Logarithmic;
            sfxSource.minDistance = 1f;
            sfxSource.maxDistance = 500f;
            sfxSource.mute = false;
        }

        ApplyVolumes();

        Debug.Log($"AudioManager: Awake. Library assigned? {(library != null)}. SFX volume={sfxVolume}, Music volume={musicVolume}");

        Debug.Log($"AudioListener.pause={AudioListener.pause}, AudioListener.volume={AudioListener.volume}");
        AudioListener.pause = false;
        AudioListener.volume = 1f;

    }

    private void Update()
    {
        // TEMP TEST: press T to play a pickup sound
        if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
        {
            Debug.Log("AudioManager TEST: T pressed -> PlayOrnamentPickup()");
            PlayOrnamentPickup();
        }
    }

    /// <summary>
    /// Applies the current volume sliders to the underlying audio sources.
    /// </summary>
    private void ApplyVolumes()
    {
        if (musicSource != null) musicSource.volume = musicVolume;
        if (sfxSource != null) sfxSource.volume = sfxVolume;
    }

    /// <summary>
    /// Plays one random ornament pickup sound (one-shot).
    /// </summary>
    public void PlayOrnamentPickup()
    {
        if (library == null || library.ornamentPickupClips == null || library.ornamentPickupClips.Length == 0)
            return;

        AudioClip clip = library.ornamentPickupClips[Random.Range(0, library.ornamentPickupClips.Length)];
        PlaySfxOneShot(clip);
    }

    /// <summary>
    /// Plays a UI click sound (one-shot).
    /// </summary>
    public void PlayUiClick()
    {
        if (library == null || library.uiClick == null)
            return;

        PlaySfxOneShot(library.uiClick);
    }

    /// <summary>
    /// Plays the end-of-round sting (one-shot).
    /// </summary>
    public void PlayGameEndSting()
    {
        if (library == null || library.gameEndSting == null)
            return;

        PlaySfxOneShot(library.gameEndSting);
    }

    /// <summary>
    /// Starts looping background music (if assigned).
    /// </summary>
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
        if (musicSource == null) return;
        musicSource.Stop();
        musicSource.clip = null;
    }

    /// <summary>
    /// Plays a one-shot SFX clip with optional pitch jitter to reduce repetition.
    /// </summary>
    private void PlaySfxOneShot(AudioClip clip)
    {
        if (clip == null || sfxSource == null)
            return;

        float basePitch = 1f;
        float jitter = (sfxPitchJitter > 0f) ? Random.Range(-sfxPitchJitter, sfxPitchJitter) : 0f;

        sfxSource.pitch = basePitch + jitter;
        sfxSource.volume = sfxVolume;
        sfxSource.PlayOneShot(clip, 1f);
        sfxSource.pitch = 1f; // reset to avoid affecting future sounds unintentionally

        Debug.Log($"AudioManager: PlayOneShot '{clip.name}' on SFX source. Source exists? {(sfxSource != null)}");

        Debug.Log($"SFX src: vol={sfxSource.volume}, mute={sfxSource.mute}, spatialBlend={sfxSource.spatialBlend}, active={sfxSource.isActiveAndEnabled}");


    }
}
