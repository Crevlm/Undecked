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
    [SerializeField] private GameObject endScreenRoot;  // Parent object for end UI.

    [Header("Buttons")]
    [SerializeField] private Button startButton; // Starts the 3-2-1 countdown (warm-up timer).
    [SerializeField] private Button restartButton; // Returns to play screen, resets ornaments, resets Start.
    [SerializeField] private Button quitButton; // Exits the application.

    [Header("Starting Countdown UI")]
    [SerializeField] private TextMeshProUGUI startCountdownText; // "3-2-1-GO" text overlay.

    [Header("End Screen Text")]
    [SerializeField] private TextMeshProUGUI finalScoreText; // Displays the player's score at the end.
    [SerializeField] private TextMeshProUGUI finalHighScoreText; // Displays the saved high score at the end.

    [Header("Ornament Reset")]
    [SerializeField] private Transform ornamentRoot; // Assign the tree root to reset all ornaments under it.

    private Ornament[] ornaments;
    private GameState state = GameState.Idle;
    private Coroutine startRoutine;

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

    private void Start()
    {
        // Cache references if not assigned.
        if (countdownTimer == null) countdownTimer = CountdownTimer.FindFirstObjectByType<CountdownTimer>();
        if (scoreManager == null) scoreManager = ScoreManager.FindFirstObjectByType<ScoreManager>();

        CacheOrnaments();
        GoToIdleState();
    }

    private void OnDestroy()
    {
        // Clean up event subscription.
        if (countdownTimer != null)
        {
            countdownTimer.CountdownCompleted -= OnTimerCompleted;
        }
    }

    /// <summary>
    /// Collects all ornaments under the provided ornamentRoot (or falls back to a scene-wide search).
    /// </summary>
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
    /// Start button handler: initiates the 3-2-1 countdown, then starts gameplay timer.
    /// </summary>
    private void OnStartPressed()
    {
        if (state != GameState.Idle)
            return;

        if (startRoutine != null)
            StopCoroutine(startRoutine);

        startRoutine = StartCoroutine(StartRoundRoutine());
    }

    /// <summary>
    /// Restart button handler: returns to play screen, resets tree and score, and re-enables Start.
    /// </summary>
    private void OnRestartPressed()
    {
        if (startRoutine != null)
        {
            StopCoroutine(startRoutine);
            startRoutine = null;
        }

        if (countdownTimer != null)
        {
            countdownTimer.StopCountdown();
            countdownTimer.ResetCountdown();
        }

        ResetRoundState();
        GoToIdleState();
    }

    /// <summary>
    /// Quit button handler: exits the application.
    /// </summary>
    private void OnQuitPressed()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    /// <summary>
    /// Handles timer completion: ends the round, saves high score, and shows end screen.
    /// </summary>
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
    /// Main start routine: disables Start, plays 3-2-1, then starts the timer and enters Running state.
    /// </summary>
    private IEnumerator StartRoundRoutine()
    {
        state = GameState.Starting;

        // Reset score and ornaments at the start of a run to ensure a clean round.
        ResetRoundState();

        // Start button should be turned off/disabled while the game is running.
        SetStartButtonEnabled(false);
        SetStartButtonVisible(false);

        // Ensure play screen is visible and end screen hidden.
        SetEndScreenVisible(false);
        SetPlayScreenVisible(true);

        // 3-2-1 countdown
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
    /// Shows a 3-2-1-GO countdown using startCountdownText if assigned; otherwise does nothing.
    /// </summary>
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
    /// Resets ornaments and score for a fresh round.
    /// </summary>
    private void ResetRoundState()
    {
        if (scoreManager != null)
        {
            scoreManager.ResetScore();
        }

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

        // Hide end screen texts until needed.
        UpdateEndScreenText();
        SetEndScreenVisible(false);
    }

    /// <summary>
    /// Sets UI and state to Idle: play screen visible, Start enabled.
    /// </summary>
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
    /// Updates end screen text fields (score and high score).
    /// </summary>
    private void UpdateEndScreenText()
    {
        int score = (scoreManager != null) ? scoreManager.CurrentScore : 0;
        int highScore = (scoreManager != null) ? scoreManager.HighScore : 0;

        if (finalScoreText != null)
            finalScoreText.text = Convert.ToString(score);

        if (finalHighScoreText != null)
            finalHighScoreText.text = Convert.ToString(highScore);
    }

    private void SetPlayScreenVisible(bool visible)
    {
        if (playScreenRoot != null)
            playScreenRoot.SetActive(visible);
    }

    private void SetEndScreenVisible(bool visible)
    {
        if (endScreenRoot != null)
            endScreenRoot.SetActive(visible);
    }

    private void SetStartButtonEnabled(bool enabled)
    {
        if (startButton != null)
            startButton.interactable = enabled;
    }

    private void SetStartButtonVisible(bool visible)
    {
        if (startButton != null)
            startButton.gameObject.SetActive(visible);
    }
}
