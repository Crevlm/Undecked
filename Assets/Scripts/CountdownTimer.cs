using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CountdownTimer : MonoBehaviour
{
    [Header("Timer Settings")]
    [SerializeField] private float countDownTime = 12f; // total number of seconds for the countdown

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI timerText; //TMPro Text Asset
    [SerializeField] private Button gameStartButton; // Start button to start the timer

    private float currentTime;
    private bool timerRunning = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentTime = countDownTime; //Reset the timer to 12 seconds
        UpdateTimerDisplay(); // Update thes the timer to showcase the correct text 0:00

        //Adds function of starting the countdown when the player hits the start button.
        if (gameStartButton != null)
        {
            gameStartButton.onClick.AddListener(StartCountdown);
        }

    }

    // Update is called once per frame
    void Update()
    {
        
        if (timerRunning)
        {
            //if there is currently time left on the clock continue reducing it by time.deltaTime
            if (currentTime > 0)
            {
                currentTime -= Time.deltaTime;
                UpdateTimerDisplay();
            }
            //if time is finished shut off the boolean, set the current time to 0, run OnCountdownComplete which allows for the game to be restarted
            else
            {
                currentTime = 0;
                timerRunning = false;
                OnCountdownComplete();
            }
        }

    }

    /// <summary>
    /// Starts the countdown timer if it is not already running.
    /// </summary>
    /// <remarks>Resets the timer to the configured countdown time and disables the game start button, if
    /// assigned, to prevent multiple countdowns from being started simultaneously.</remarks>
    public void StartCountdown()
    {
        if (!timerRunning)
        {
            currentTime = countDownTime;
            timerRunning = true;

            if (gameStartButton != null)
            {
                gameStartButton.interactable = false;
            }
        }
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
    /// <remarks>This method also enables the game start button if it is available, allowing the countdown to
    /// be started again.</remarks>
    public void ResetCountdown()
    {
        timerRunning = false;
        currentTime = countDownTime;
        UpdateTimerDisplay();

        if (gameStartButton != null)
        {
            gameStartButton.interactable = true;
        }
    }


    /// <summary>
    /// Updates the timer display to show the current time in minutes and seconds.
    /// </summary>
    /// <remarks>This method formats the current timer value and updates the associated text display. If the
    /// timer text component is not assigned, the method performs no action.</remarks>
    private void UpdateTimerDisplay()
    {
        if (timerText != null)
        {
            int seconds = Mathf.FloorToInt(currentTime);
            int milliseconds = Mathf.FloorToInt((currentTime - seconds) * 100);
            timerText.text = string.Format("{0:00}:{1:00}", seconds, milliseconds);
        }
    }


    /// <summary>
    /// Handles actions to perform when the countdown has finished.
    /// </summary>
    /// <remarks>This method updates the timer display and re-enables the start button if it is available. It
    /// is intended to be called when a countdown sequence completes.</remarks>
    private void OnCountdownComplete()
    {
        Debug.Log("Countdown Complete!");
        UpdateTimerDisplay();

        //Re-enable the start button
        if (gameStartButton != null)
        {
            gameStartButton.interactable = true;
        }
    }


    // Add the functionality of what happens after the countdown ends.
    //Ideas:

    //Dialogue Trigger
    //Score Saved
    //Option to read the instructions again
    //See High Scores
    //Exit Game 


}
