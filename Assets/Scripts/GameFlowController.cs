using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Coordinates the overall round lifecycle and screen flow:
/// Idle (Start available) > Starting (3-2-1-GO) > Running (timer active) > EndScreen (score shown, restart available).
/// </summary>
/// <remarks>
/// Key design intent:
/// - Ornaments spawn once on game launch via OrnamentSpawner.Start().
/// - Start button begins the 3-2-1-GO countdown and then starts the gameplay timer (no respawn here).
/// - End screen appears when the timer completes; Start remains hidden/disabled.
/// - Restart button respawns a fresh ornament set AND immediately begins 3-2-1-GO (no extra Start click).
///
/// Audio:
/// - UI clicks trigger via <see cref="AudioManager.Instance"/> if present.
/// - End-of-round sting triggers when the timer completes.
/// - Background music can be started once (optional) on Start.
/// </remarks>
public class GameFlowController : MonoBehaviour
{
    private enum GameState
    {
        Idle,       // Play screen visible, not running, Start enabled.
        Starting,   // 3-2-1 countdown playing.
        Running,    // Timer running; gameplay active.
        EndScreen   // End screen visible; Start stays off until Restart.
    }

    [Header("Core References")]
    [SerializeField] private CountdownTimer countdownTimer;
    [SerializeField] private ScoreManager scoreManager;

    [Header("UI Screens")]
    [SerializeField] private GameObject playScreenRoot; // Parent object for play UI.
    [SerializeField] private GameObject endScreenRoot;  // Parent object for end UI.

    [Header("Buttons")]
    [SerializeField] private Button startButton;   // Starts the 3-2-1 countdown (warm-up timer).
    [SerializeField] private Button restartButton; // Spawns a new set of ornaments and immediately starts a new round.
    [SerializeField] private Button quitButton;    // Exits the application.

    [Header("Starting Countdown UI")]
    [SerializeField] private TextMeshProUGUI startCountdownText; // "3-2-1-GO" text overlay.

    [Header("End Screen Text")]
    [SerializeField] private TextMeshProUGUI finalScoreText;     // Displays the player's score at the end.
    [SerializeField] private TextMeshProUGUI finalHighScoreText; // Displays the saved high score at the end.

    [Header("Ornaments")]
    [SerializeField] private Transform ornamentRoot;        // Assign the tree root to find/reset all ornaments under it (fallback path).
    [SerializeField] private OrnamentSpawner ornamentSpawner; // Spawns ornaments and can respawn on Restart.

    [Header("Audio (Optional)")]
    [Tooltip("If enabled, the controller will request background music when the scene starts.")]
    [SerializeField] private bool startBackgroundMusicOnSceneStart = true;

    //[Header("How to Play")]
    //[SerializeField] private GameObject instructionsImage; // The instructions image

    private Ornament[] ornaments;
    private GameState state = GameState.Idle;
    private Coroutine startRoutine;

    /// <summary>
    /// Registers UI button handlers and subscribes to gameplay timer completion.
    /// </summary>
    /// <remarks>
    /// This wiring occurs in Awake so UI clicks are captured immediately.
    /// </remarks>
    private void Awake()
    {
        if (startButton != null) startButton.onClick.AddListener(OnStartPressed);
        if (restartButton != null) restartButton.onClick.AddListener(OnRestartPressed);
        if (quitButton != null) quitButton.onClick.AddListener(OnQuitPressed);

        if (countdownTimer != null)
        {
            countdownTimer.CountdownCompleted += OnTimerCompleted;
        }

    }

    /// <summary>
    /// Caches core references (timer, score, spawner), discovers ornaments, and initializes UI to Idle.
    /// </summary>
    /// <remarks>
    /// Ornaments are expected to spawn via <see cref="OrnamentSpawner.Start"/>. This controller does not spawn on load.
    /// </remarks>
    private void Start()
    {
        // Cache references if not assigned in Inspector.
        if (countdownTimer == null) countdownTimer = FindFirstObjectByType<CountdownTimer>();
        if (scoreManager == null) scoreManager = FindFirstObjectByType<ScoreManager>();
        if (ornamentSpawner == null) ornamentSpawner = FindFirstObjectByType<OrnamentSpawner>();

        CacheOrnaments();
        GoToIdleState();

        // Optional: request background music once the scene starts.
        if (startBackgroundMusicOnSceneStart)
        {
            if (AudioManager.Instance == null)
            {
                Debug.LogWarning("GameFlowController: AudioManager.Instance is NULL. No background music will play.");
            }
            else
            {
                AudioManager.Instance.PlayBackgroundMusic();
            }
        }

        //Hide the instructions button unless it's clicked
        //if (instructionsImage != null)
        // {
        //     instructionsImage.SetActive(false);
        // }

    }

    /// <summary>
    /// Unsubscribes from timer events when this controller is destroyed.
    /// </summary>
    private void OnDestroy()
    {
        if (countdownTimer != null)
        {
            countdownTimer.CountdownCompleted -= OnTimerCompleted;
        }

    }


    /// <summary>
    /// Collects all ornaments under the provided <see cref="ornamentRoot"/> (or falls back to a scene-wide search).
    /// </summary>
    /// <remarks>
    /// Used after a respawn to refresh internal references, and as a fallback path if no spawner is present.
    /// </remarks>
    private void CacheOrnaments()
    {
        if (ornamentRoot != null)
        {
            ornaments = ornamentRoot.GetComponentsInChildren<Ornament>(true);
        }
        else
        {
            // Fallback: searches the whole scene (including inactive).
            ornaments = FindObjectsByType<Ornament>(FindObjectsSortMode.None);
        }
    }

    /// <summary>
    /// Handles Start button clicks: begins the 3-2-1-GO countdown and then starts the gameplay timer.
    /// </summary>
    /// <remarks>
    /// Start does not spawn or reset ornaments. Ornaments are expected to already exist from initial game launch.
    /// </remarks>
    private void OnStartPressed()
    {
        AudioManager.Instance?.PlayUiClick();

        if (state != GameState.Idle)
            return;

        StopStartRoutineIfRunning();
        startRoutine = StartCoroutine(StartRoundRoutine());
    }

    /// <summary>
    /// Handles Restart button clicks: respawns a fresh set of ornaments and immediately begins a new 3-2-1-GO countdown.
    /// </summary>
    /// <remarks>
    /// Restart is only valid from the EndScreen state. It does not require the player to press Start again.
    /// </remarks>
    private void OnRestartPressed()
    {
        AudioManager.Instance?.PlayUiClick();
        AudioManager.Instance?.StopTimerTickLoop();

        if (state != GameState.EndScreen)
            return;

        StopStartRoutineIfRunning();

        // Restart should spawn a new set and immediately begin 3-2-1-GO.
        RespawnOrnaments();

        startRoutine = StartCoroutine(StartRoundRoutine());
    }

    /// <summary>
    /// Handles Quit button clicks: exits the application (and stops play mode when running in the Unity Editor).
    /// </summary>
    private void OnQuitPressed()
    {
        AudioManager.Instance?.PlayUiClick();
        AudioManager.Instance?.StopTimerTickLoop();

        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    //private void OnInstructionsPressed()
    //{

    //    if (instructionsImage != null)
    //    {

    //        instructionsImage.SetActive(true);
    //    }
    //    if (closeInstructionsButton != null)
    //    {
    //        closeInstructionsButton.gameObject.SetActive(true);
    //    }
    //    else
    //    {
    //        Debug.LogError("Instructions image is not assigned in the Inspector!");
    //    }
    //}

    //private void OnCloseInstructionsPressed()
    //{
    //    Debug.Log("Close instructions button pressed!");
    //    if (instructionsImage != null)
    //    {
    //        instructionsImage.SetActive(false);
    //    }
    //    if (closeInstructionsButton != null)
    //    {
    //        closeInstructionsButton.gameObject.SetActive(false);
    //    }
    //}



    /// <summary>
    /// Handles gameplay timer completion: ends the round, saves high score, and shows the end screen UI.
    /// </summary>
    /// <remarks>
    /// Start remains hidden/disabled during the end screen; Restart is the intended path to begin another round.
    /// </remarks>
    private void OnTimerCompleted()
    {
        if (state != GameState.Running)
            return;

        AudioManager.Instance?.StopTimerTickLoop();
        AudioManager.Instance?.PlayGameEndSting();

        state = GameState.EndScreen;

        // Play end-of-round sting once when the timer completes.
        AudioManager.Instance?.PlayGameEndSting();

        if (scoreManager != null)
        {
            scoreManager.SaveHighScoreIfNeeded();
        }

        UpdateEndScreenText();
        SetPlayScreenVisible(false);
        SetEndScreenVisible(true);

        // Start button stays off while end screen is active; Restart is the path forward.
        SetStartButtonEnabled(false);
        SetStartButtonVisible(false);
    }

    /// <summary>
    /// Runs the round start sequence: transitions to Starting, prepares UI/system state, plays 3-2-1-GO, then starts the timer.
    /// </summary>
    /// <remarks>
    /// This routine does not respawn ornaments. Restart is responsible for generating a fresh ornament set.
    /// </remarks>
    private IEnumerator StartRoundRoutine()
    {
        state = GameState.Starting;

        // Prepare UI + timer + score for a new run (but DO NOT touch ornaments here).
        PrepareForNewRunUI();

        // 3-2-1 countdown.
        yield return ShowStartCountdown();

        // Start the gameplay timer.
        state = GameState.Running;
        if (countdownTimer != null)
        {
            countdownTimer.StartCountdown();
        }

        state = GameState.Running;
        if (countdownTimer != null)
        {
            countdownTimer.StartCountdown();
        }

        AudioManager.Instance?.PlayTimerTickLoop();


        startRoutine = null;
    }

    /// <summary>
    /// Displays the 3-2-1-GO countdown using <see cref="startCountdownText"/> if assigned.
    /// </summary>
    /// <remarks>
    /// If no countdown text is assigned, this routine exits immediately and the round proceeds without the warm-up overlay.
    /// </remarks>
    private IEnumerator ShowStartCountdown()
    {
        if (startCountdownText == null)
            yield break;

        startCountdownText.gameObject.SetActive(true);

        startCountdownText.text = "3";
        AudioManager.Instance?.PlayCountdownTick();
        yield return new WaitForSeconds(.9f);

        startCountdownText.text = "2";
        AudioManager.Instance?.PlayCountdownTick();
        yield return new WaitForSeconds(.9f);

        startCountdownText.text = "1";
        AudioManager.Instance?.PlayCountdownTick();
        yield return new WaitForSeconds(.9f);

        startCountdownText.text = "GO!";
        AudioManager.Instance?.PlayCountdownGo();
        yield return new WaitForSeconds(.85f);

        startCountdownText.gameObject.SetActive(false);
    }

    /// <summary>
    /// Sets the game to Idle: shows the play screen, hides the end screen, and enables the Start button.
    /// </summary>
    /// <remarks>
    /// This state is used on initial load. It does not respawn ornaments; initial tree population is expected to
    /// occur via <see cref="OrnamentSpawner.Start"/>.
    /// </remarks>
    private void GoToIdleState()
    {
        state = GameState.Idle;

        SetEndScreenVisible(false);
        SetPlayScreenVisible(true);

        // Start button should be available only in Idle.
        SetStartButtonVisible(true);
        SetStartButtonEnabled(true);

        if (startCountdownText != null)
        {
            startCountdownText.gameObject.SetActive(false);
        }

        if (countdownTimer != null)
        {
            countdownTimer.ResetCountdown();
        }
    }

    /// <summary>
    /// Updates the end screen score fields using the current score and persisted high score.
    /// </summary>
    /// <remarks>
    /// This does not save the high score; saving is performed when the timer completes.
    /// </remarks>
    private void UpdateEndScreenText()
    {
        int score = (scoreManager != null) ? scoreManager.CurrentScore : 0;
        int highScore = (scoreManager != null) ? scoreManager.HighScore : 0;

        if (finalScoreText != null)
            finalScoreText.text = Convert.ToString(score);

        if (finalHighScoreText != null)
            finalHighScoreText.text = Convert.ToString(highScore);
    }

    /// <summary>
    /// Shows or hides the play screen root UI.
    /// </summary>
    private void SetPlayScreenVisible(bool visible)
    {
        if (playScreenRoot != null)
            playScreenRoot.SetActive(visible);
    }

    /// <summary>
    /// Shows or hides the end screen root UI.
    /// </summary>
    private void SetEndScreenVisible(bool visible)
    {
        if (endScreenRoot != null)
            endScreenRoot.SetActive(visible);
    }

    /// <summary>
    /// Enables or disables interaction with the Start button.
    /// </summary>
    private void SetStartButtonEnabled(bool enabled)
    {
        if (startButton != null)
            startButton.interactable = enabled;
    }

    /// <summary>
    /// Shows or hides the Start button GameObject.
    /// </summary>
    private void SetStartButtonVisible(bool visible)
    {
        if (startButton != null)
            startButton.gameObject.SetActive(visible);
    }

    /// <summary>
    /// Prepares UI and core systems for a new run (timer, score, and screen visibility).
    /// </summary>
    /// <remarks>
    /// This method intentionally does not spawn or reset ornaments. It is safe to call from both Start and Restart paths.
    /// </remarks>
    private void PrepareForNewRunUI()
    {
        SetEndScreenVisible(false);
        SetPlayScreenVisible(true);

        // Start button should be turned off/disabled while the game is running (and during countdown).
        SetStartButtonEnabled(false);
        SetStartButtonVisible(false);

        if (startCountdownText != null)
        {
            startCountdownText.gameObject.SetActive(false);
        }

        if (countdownTimer != null)
        {
            countdownTimer.StopCountdown();
            countdownTimer.ResetCountdown(); // Resets display to initial time.
        }

        if (scoreManager != null)
        {
            scoreManager.ResetScore();
        }
    }

    /// <summary>
    /// Respawns ornaments via <see cref="ornamentSpawner"/> and refreshes internal ornament references.
    /// </summary>
    /// <remarks>
    /// If no spawner is assigned/found, this method performs no action.
    /// </remarks>
    private void RespawnOrnaments()
    {
        if (ornamentSpawner != null)
        {
            ornamentSpawner.RespawnAllOrnaments();
            CacheOrnaments(); // If ornaments[] is used elsewhere, this ensures references are current.
        }
        else
        {
            Debug.LogWarning("GameFlowController: ornamentSpawner is NULL. Cannot respawn ornaments.");
        }
    }

    /// <summary>
    /// Stops the currently running start coroutine if one is active.
    /// </summary>
    private void StopStartRoutineIfRunning()
    {
        if (startRoutine != null)
        {
            StopCoroutine(startRoutine);
            startRoutine = null;
        }
    }
}