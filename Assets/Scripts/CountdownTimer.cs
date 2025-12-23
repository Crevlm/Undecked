using System;
using TMPro;
using UnityEngine;

public class CountdownTimer : MonoBehaviour
{
    [Header("Timer Settings")]
    [SerializeField] private float countDownTime = 12f; // Total number of seconds for the countdown.

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI timerText; // TMPro Text asset for the timer display.

    // Use these events to notify other systems when the countdown starts and ends.
    public event Action CountdownStarted;
    public event Action CountdownCompleted;

    private float currentTime;
    private bool timerRunning = false;

    /// <summary>
    /// Gets whether the timer is currently running.
    /// </summary>
    public bool IsRunning => timerRunning;

    /// <summary>
    /// Gets the configured duration of the countdown timer in seconds.
    /// </summary>
    public float Duration => countDownTime;

    // Start is called once before the first execution of Update after the MonoBehaviour is created.
    private void Start()
    {
        currentTime = Mathf.Max(0f, countDownTime); // Reset the timer to the configured duration.
        UpdateTimerDisplay(); // Update the timer display to show the correct starting text 
    }

    // Update is called once per frame.
    private void Update()
    {
        if (!timerRunning)
            return;

        // Reduce time by deltaTime, clamping to 0 to avoid negative time display.
        currentTime = Mathf.Max(0f, currentTime - Time.deltaTime);
        UpdateTimerDisplay();

        // If time is finished, shut off the boolean, set the current time to 0, and run OnCountdownComplete.
        if (currentTime <= 0f)
        {
            timerRunning = false;
            currentTime = 0f;
            OnCountdownComplete();
        }
    }

    /// <summary>
    /// Starts the countdown timer if it is not already running.
    /// </summary>
    /// <remarks>
    /// Resets the timer to the configured countdown time. Intended to be triggered by a separate game flow controller.
    /// </remarks>
    public void StartCountdown()
    {
        if (timerRunning)
            return;

        currentTime = Mathf.Max(0f, countDownTime);
        timerRunning = true;

        UpdateTimerDisplay();
        CountdownStarted?.Invoke();
    }

    /// <summary>
    /// Stops the countdown timer if it is currently running.
    /// </summary>
    /// <remarks>Calling this method has no effect if the countdown timer is already stopped.</remarks>
    public void StopCountdown()
    {
        timerRunning = false;
    }

    /// <summary>
    /// Resets the countdown timer to its initial value and updates the timer display.
    /// </summary>
    /// <remarks>This method does not start the timer; it only returns it to its initial state.</remarks>
    public void ResetCountdown()
    {
        timerRunning = false;
        currentTime = Mathf.Max(0f, countDownTime);
        UpdateTimerDisplay();
    }

    /// <summary>
    /// Updates the timer display to show the current time in seconds and centiseconds (SS:CC).
    /// </summary>
    /// <remarks>
    /// This method formats the current timer value and updates the associated text display.
    /// If the timer text component is not assigned, the method performs no action.
    /// </remarks>
    private void UpdateTimerDisplay()
    {
        if (timerText == null)
            return;

        // Convert to centiseconds so 12.00 displays as "12:00" and 0 displays as "00:00".
        int totalCentiseconds = Mathf.CeilToInt(Mathf.Max(0f, currentTime) * 100f);
        int seconds = totalCentiseconds / 100;
        int centiseconds = totalCentiseconds % 100;

        timerText.text = string.Format("Timer: {0:00}:{1:00}", seconds, centiseconds);
    }

    /// <summary>
    /// Handles actions to perform when the countdown has finished.
    /// </summary>
    /// <remarks>
    /// This method updates the timer display and raises the completion event.
    /// It is intended to be called when a countdown sequence completes.
    /// </remarks>
    private void OnCountdownComplete()
    {
        Debug.Log("Countdown Complete!");
        UpdateTimerDisplay();
        CountdownCompleted?.Invoke();
    }


    // Add the functionality of what happens after the countdown ends.
    // Ideas:
    // Dialogue Trigger
    // Score Saved - DONE
    // Option to read the instructions again
    // See High Scores - DONE
    // Exit Game
}
