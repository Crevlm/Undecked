using UnityEngine;

public class Ornament : MonoBehaviour
{
    private Vector3 startPosition;

    /// <summary>
    /// This enum is now redundant, as OrnamentColor is now defined in DragOrnaments.cs.
    /// </summary>
    //public enum OrnamentColor
    //{
    //    Red,
    //    Green,
    //    Gold
    //}


    private void Start()
    {
        // Stores the starting position for return.
        startPosition = transform.position;
    }

    /// <summary>
    /// Returns the ornament to its starting position (on the tree).
    /// </summary>
    public void ReturnToStart()
    {
        transform.position = startPosition;
    }

    /// <summary>
    /// Marks the ornament as collected by disabling the GameObject.
    /// </summary>
    /// <remarks>
    /// This replaces Destroy() so the ornament can be restored on Restart.
    /// </remarks>
    public void Collect()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Fully resets the ornament back onto the tree and re-enables it.
    /// </summary>
    /// <remarks>
    /// Also clears drag state if a DragOrnaments component is attached.
    /// </remarks>
    public void ResetOrnament()
    {
        gameObject.SetActive(true);
        ReturnToStart();

        DragOrnaments drag = GetComponent<DragOrnaments>();
        if (drag != null)
        {
            drag.ResetDragState();
        }
    }
}
