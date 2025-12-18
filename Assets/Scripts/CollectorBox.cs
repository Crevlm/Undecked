using UnityEngine;

public class CollectorBox : MonoBehaviour
{
    [SerializeField] private OrnamentColor boxColor;

    [Header("Scoring")]
    [SerializeField] private int pointsPerCorrect = 12;  // Points awarded for a correct ornament.
    [SerializeField] private int pointsPerWrong = -6;    // Points deducted for a wrong ornament (not currently in use, can be implemented later).
    [SerializeField] private ScoreManager scoreManager; // Score receiver.

    private void Start()
    {
        // Cache ScoreManager.
        if (scoreManager == null)
        {
            scoreManager = ScoreManager.FindFirstObjectByType<ScoreManager>();
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        DragOrnaments drag = other.GetComponent<DragOrnaments>();
        if (drag == null) return;

        // Only evaluate when the ornament was just dropped.
        if (!drag.WasDroppedRecently) return;

        // If the ornament is still being dragged, do nothing.
        if (drag.isDragging) return;

        // Consume the drop so we evaluate this placement only once.
        drag.ConsumeDrop();

        if (drag.ornamentColor == boxColor)
        {
            Debug.Log("Correct!");

            if (scoreManager != null)
            {
                scoreManager.AddPoints(pointsPerCorrect);
            }

            // Disable instead of Destroy so Restart can restore the tree.
            Ornament ornament = other.GetComponent<Ornament>();
            if (ornament != null)
            {
                ornament.Collect();
            }
            else
            {
                // Failsafe: if no Ornament component is present, disable the object anyway.
                other.gameObject.SetActive(false);
            }
        }
        else
        {
            Debug.Log("Wrong color!");

            // Return to starting position on tree if it was dropped into the wrong box.
            Ornament ornament = other.GetComponent<Ornament>();
            if (ornament != null)
            {
                ornament.ReturnToStart();
            }
        }
    }
}
