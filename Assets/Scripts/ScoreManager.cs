using System;
using TMPro;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    [Header("Score UI (Optional)")]
    [SerializeField] private TextMeshProUGUI liveScoreText; // Displays the score during gameplay.

    // Use this event to notify other systems when the score changes (UI, audio, etc).
    public event Action<int> ScoreChanged;

    public int CurrentScore { get; private set; }
    public int HighScore => PlayerPrefs.GetInt(HighScoreKey, 0);

    private const string HighScoreKey = "HighScore";

    /// <summary>
    /// Resets the current score to 0 and updates any assigned UI.
    /// </summary>
    public void ResetScore()
    {
        CurrentScore = 0;
        UpdateLiveScoreUI();
        ScoreChanged?.Invoke(CurrentScore);
    }

    /// <summary>
    /// Adds points to the current score and updates any assigned UI.
    /// </summary>
    /// <remarks>Points are clamped so score never becomes negative.</remarks>
    public void AddPoints(int points)
    {
        CurrentScore = Mathf.Max(0, CurrentScore + points);
        UpdateLiveScoreUI();
        ScoreChanged?.Invoke(CurrentScore);
    }

    /// <summary>
    /// Saves the current score as the new high score if it exceeds the previously saved value.
    /// </summary>
    public void SaveHighScoreIfNeeded()
    {
        int highScore = HighScore;
        if (CurrentScore > highScore)
        {
            PlayerPrefs.SetInt(HighScoreKey, CurrentScore);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// Updates the in-game score UI text.
    /// </summary>
    private void UpdateLiveScoreUI()
    {
        if (liveScoreText != null)
        {
            liveScoreText.text = Convert.ToString(CurrentScore);
        }
    }
}
