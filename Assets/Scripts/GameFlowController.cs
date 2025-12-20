using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameFlowController : MonoBehaviour
{
    private enum GameState
    {
        Idle, // Play screen visible, not running, Start enabled.
        Starting, // 3-2-1 countdown playing.
        Running, // Timer running; gameplay active.
        EndScreen // End screen visible; Start stays off until Restart.
    }

    [Header("Core References")]
    [SerializeField] private CountdownTimer countdownTimer;
    [SerializeField] private ScoreManager scoreManager;

    [Header("UI Screens")]
    [SerializeField] private GameObject playScreenRoot; // Parent object for play UI.
    [SerializeField] private GameObject endScreenRoot; // Parent object for end UI.

    [Header("Buttons")]
    [SerializeField] private Button startButton; // Starts the 3-2-1 countdown (warm-up timer).
    [SerializeField] private Button restartButton; // Spawns a new set of ornaments and immediately starts a new round.
    [SerializeField] private Button quitButton; // Exits the application.

    [Header("Starting Countdown UI")]
    [SerializeField] private TextMeshProUGUI startCountdownText; // "3-2-1-GO" text overlay.

    [Header("End Screen Text")]
    [SerializeField] private TextMeshProUGUI finalScoreText; // Displays the player's score at the end.
    [SerializeField] private TextMeshProUGUI finalHighScoreText; // Displays the saved high score at the end.

    [Header("Ornaments")]
    [SerializeField] private Transform ornamentRoot; // Assign the tree root to find/reset all ornaments under it.
    [SerializeField] private OrnamentSpawner ornamentSpawner; // Spawns ornaments and can respawn on Restart.

    private Ornament[] ornaments;
    private GameState state = GameState.Idle;
    private Coroutine startRoutine;

    /// <summary>
    /// Registers UI button handlers and subscribes to the gameplay timer completion event.
    /// </summary>
    /// <remarks>
    /// This method wires up input/events early so the controller can transition between game states.
    /// </remarks>
    private void Awake()
    {
        // Button assignments.
        if (startButton != null) startButton.onClick.AddListener(OnStartPressed);
        if (restartButton != null) restartButton.onClick.AddListener(OnRestartPressed);
        if (quitButton != null) quitButton.onClick.AddListener(OnQuitPressed);

        // Subscribe to timer completion so round can end.
        if (countdownTimer != null)
        {
            countdownTimer.CountdownCompleted += OnTimerCompleted;
        }
    }

    /// <summary>
    /// Caches core references (timer, score, spawner), discovers ornaments, and initializes the UI to the Idle state.
    /// </summary>
    /// <remarks>
    /// The initial ornament population is expected to occur via <see cref="OrnamentSpawner.Start"/> (not here).
    /// </remarks>
    private void Start()
    {
        // Cache references if not assigned.
        if (countdownTimer == null) countdownTimer = FindFirstObjectByType<CountdownTimer>();
        if (scoreManager == null) scoreManager = FindFirstObjectByType<ScoreManager>();

        // NOTE: The previous line called FindObjectsByType but did not assign the result.
        // This ensures ornamentSpawner gets a usable reference when not set in the inspector.
        if (ornamentSpawner == null) ornamentSpawner = FindFirstObjectByType<OrnamentSpawner>();

        CacheOrnaments();
        GoToIdleState();
    }

    /// <summary>
    /// Unsubscribes from timer events when this controller is destroyed.
    /// </summary>
    private void OnDestroy()
    {
        // Clean up event subscription.
        if (countdownTimer != null)
        {
            countdownTimer.CountdownCompleted -= OnTimerCompleted;
        }
    }

    /// <summary>
    /// Collects all ornaments under the provided <see cref="ornamentRoot"/> (or falls back to a scene-wide search).
    /// </summary>
    /// <remarks>
    /// This is used after a respawn to refresh internal references, and also provides a fallback path if no spawner is present.
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
    /// This does not spawn or reset ornaments. Ornaments are expected to already exist from the initial game launch,
    /// and Restart is responsible for respawning a fresh set between rounds.
    /// </remarks>
    private void OnStartPressed()
    {
        if (state != GameState.Idle)
            return;

        if (startRoutine != null)
            StopCoroutine(startRoutine);

        // IMPORTANT: Start should NOT remove/spawn ornaments.
        // Launch already spawned them; Start should just begin the round.
        startRoutine = StartCoroutine(StartRoundRoutine());
    }

    /// <summary>
    /// Handles Restart button clicks: spawns a fresh set of ornaments and immediately begins a new 3-2-1-GO countdown.
    /// </summary>
    /// <remarks>
    /// Restart is only valid from the EndScreen state. It does not require the player to press Start again.
    /// </remarks>
    private void OnRestartPressed()
    {
        if (state != GameState.EndScreen)
            return;

        if (startRoutine != null)
        {
            StopCoroutine(startRoutine);
            startRoutine = null;
        }

        // Restart should spawn a new set and immediately begin 3-2-1-GO.
        RespawnOrnaments();

        startRoutine = StartCoroutine(StartRoundRoutine());
    }

    /// <summary>
    /// Handles Quit button clicks: exits the application (and stops play mode when running in the Unity Editor).
    /// </summary>
    private void OnQuitPressed()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

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

        state = GameState.EndScreen;

        if (scoreManager != null)
        {
            scoreManager.SaveHighScoreIfNeeded();
        }

        UpdateEndScreenText();
        SetPlayScreenVisible(false);
        SetEndScreenVisible(true);

        // Start button stays off while end screen is active; Restart re-enables it.
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
        yield return new WaitForSeconds(1.2f);

        startCountdownText.text = "2";
        yield return new WaitForSeconds(1.2f);

        startCountdownText.text = "1";
        yield return new WaitForSeconds(1.2f);

        startCountdownText.text = "GO!";
        yield return new WaitForSeconds(1f);

        startCountdownText.gameObject.SetActive(false);
    }

    /// <summary>
    /// Resets score and ornaments to prepare for a fresh round, then hides the end screen.
    /// </summary>
    /// <remarks>
    /// This method respawns ornaments if a spawner is available; otherwise it falls back to resetting any existing
    /// ornament instances that are present in the scene hierarchy.
    /// </remarks>
    private void ResetRoundState()
    {
        if (scoreManager != null)
        {
            scoreManager.ResetScore();
        }

        // If using spawner-driven ornaments, respawn a fresh set.
        if (ornamentSpawner != null)
        {
            ornamentSpawner.RespawnAllOrnaments();
            CacheOrnaments(); // Re-cache ornaments after respawn if you rely on the ornaments array elsewhere.
        }
        else
        {
            // Fallback: reset existing ornaments if no spawner is assigned.
            if (ornaments == null || ornaments.Length == 0)
            {
                CacheOrnaments();
            }

            if (ornaments != null)
            {
                for (int i = 0; i < ornaments.Length; i++)
                {
                    if (ornaments[i] != null)
                    {
                        ornaments[i].ResetOrnament();
                    }
                }
            }
        }

        UpdateEndScreenText();
        SetEndScreenVisible(false);
    }

    /// <summary>
    /// Sets the game to Idle: shows the play screen, hides the end screen, and enables the Start button.
    /// </summary>
    /// <remarks>
    /// This state is used on initial load. It does not respawn ornaments; the initial tree population is expected to
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

        // Do NOT respawn ornaments here.
        // Game launch spawns them via OrnamentSpawner.Start().
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
    }
}
