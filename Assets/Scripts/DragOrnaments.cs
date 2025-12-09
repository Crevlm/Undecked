using UnityEngine;
using UnityEngine.InputSystem;

public class DragOrnaments : MonoBehaviour
{
    private bool isDragging;
    private Vector3 offset;
    private Camera cam;

    void Awake()
    {
        cam = Camera.main; //cache's the camera reference
    }

    void Update()
    {
        if (Mouse.current == null) return; // prevents crashes if no mouse/touch-only input/remote

        //Detect when the click on the object has started
        if (Mouse.current.leftButton.wasPressedThisFrame)
            TryPickUp();
        //Detect when the mouse click has been released
        if (Mouse.current.leftButton.wasReleasedThisFrame)
            isDragging = false;
        //Only move the item if the mouse is clicked on it
        if (isDragging)
            Drag();
    }


    /// <summary>
    /// Attempts to initiate a drag operation on the object if the mouse is currently positioned over it.
    /// </summary>
    /// <remarks>This method checks whether the mouse cursor is over the object's collider and, if so, enables
    /// dragging by setting the appropriate state. It is typically called in response to a mouse input event, such as a
    /// button press.</remarks>
    void TryPickUp()
    {
        Vector2 mouseWorld = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        RaycastHit2D hit = Physics2D.Raycast(mouseWorld, Vector2.zero);

        if (hit && hit.collider.gameObject == gameObject) 
        {
            isDragging = true;
            offset = transform.position - (Vector3)mouseWorld;
        }
    }
    /// <summary>
    /// Moves the object to follow the current mouse position in world space, applying the specified offset.
    /// </summary>
    /// <remarks>This method is typically used to implement drag-and-drop behavior, updating the object's
    /// position to match the mouse cursor as it moves. The object's position is set based on the mouse's world
    /// coordinates plus any configured offset.</remarks>
    void Drag()
    {
        Vector2 mouseWorld = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        transform.position = (Vector3)mouseWorld + offset;
    }
}
