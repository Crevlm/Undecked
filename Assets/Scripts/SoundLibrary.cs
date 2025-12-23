using UnityEngine;

[CreateAssetMenu(fileName = "SoundLibrary", menuName = "Audio/Sound Library")]
public class SoundLibrary : ScriptableObject
{
    [Header("Ornaments")]
    [Tooltip("One of these plays when the player picks up an ornament.")]
    public AudioClip[] ornamentPickupClips;

    [Header("UI")]
    public AudioClip[] uiClick;

    [Header("Game State")]
    public AudioClip gameEndSting;

    [Header("Music")]
    public AudioClip backgroundMusicLoop;

    [Header("Start Countdown")]
    public AudioClip startCountdownTick;
    public AudioClip countdownGo;

    [Header("Timer")]
    public AudioClip timerTickLoop;

}
