using UnityEngine;
using UnityEngine.InputSystem;

public enum OrnamentColor
{
    Red,
    Green,
    Gold
}

[RequireComponent(typeof(Collider2D))]
public class DragOrnaments : MonoBehaviour
{
    public bool isDragging;
    private Vector3 offset;
    private Camera cam;

    public OrnamentColor ornamentColor;

    [Header("Round Control")]
    [SerializeField] private CountdownTimer countdownTimer; // If assigned, dragging only works while the timer is running.

    [Header("Drag Visual Effects")]
    [SerializeField] private float dragScale = 1.2f; // scales slightly larger when picked up
    [SerializeField] private float dragRotationRange = 10f; // max rotation in degrees.


    [Header("Drop Detection")]
    [SerializeField] private float dropGraceSeconds = 0.15f; // How long after releasing counts as "just dropped" (for trigger evaluation).

    private float droppedUntilTime = -1f;
    private Vector3 originalScale;
    private Quaternion originalRotation;

    /// <summary>
    /// True if the ornament was dropped very recently.
    /// Used to evaluate trigger events only once per drop.
    /// Prevents multiple scoring from a single drop.
    /// </summary>
    public bool WasDroppedRecently => Time.time <= droppedUntilTime;

    private void Awake()
    {
        cam = Camera.main; // Cache the camera reference.
        originalScale = transform.localScale; // gets the original scale of the ornament
        originalRotation = transform.rotation; // gets the original rotation of the ornament
    }

    private void Start()
    {
        // Cache CountdownTimer if not assigned in the inspector.
        if (countdownTimer == null)
        {
            countdownTimer = CountdownTimer.FindFirstObjectByType<CountdownTimer>();
        }
    }

    private void Update()
    {
        if (Mouse.current == null) return; // Prevents crashes if no mouse/touch-only input/remote.
        if (cam == null) return; // If there's no main camera, dragging cannot work.

        // Only allow dragging while the timer is running (prevents interaction during 3-2-1 and end screen).
        if (countdownTimer != null && !countdownTimer.IsRunning)
            return;

        // Detect when the click on the object has started.
        if (Mouse.current.leftButton.wasPressedThisFrame)
            TryPickUp();

        // Detect when the mouse click has been released.
        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            isDragging = false;

            // Mark this ornament as "just dropped" for a short window so CollectorBox can evaluate once.
            droppedUntilTime = Time.time + dropGraceSeconds;

            transform.localScale = originalScale;
            transform.rotation = originalRotation;
        }

        // Only move the item if the mouse is clicked on it.
        if (isDragging)
            Drag();
    }

    /// <summary>
    /// Clears the "recent drop" window so triggers do not re-evaluate the same drop repeatedly.
    /// </summary>
    public void ConsumeDrop()
    {
        droppedUntilTime = -1f;
    }

    /// <summary>
    /// Resets local drag state (used when restarting a round).
    /// </summary>
    public void ResetDragState()
    {
        isDragging = false;
        droppedUntilTime = -1f;

        transform.localScale = originalScale;
        transform.rotation = originalRotation;
    }

    /// <summary>
    /// Attempts to initiate a drag operation on the object if the mouse is currently positioned over it.
    /// </summary>
    /// <remarks>
    /// This method checks whether the mouse cursor is over the object's collider and, if so, enables
    /// dragging by setting the appropriate state. It is typically called in response to a mouse input event.
    /// </remarks>
    private void TryPickUp()
    {
        Vector2 mouseWorld = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());

        
        int ornamentLayerMask = 1 << 6; // Layer 6
        RaycastHit2D hit = Physics2D.Raycast(mouseWorld, Vector2.zero, Mathf.Infinity, ornamentLayerMask);

       

        if (hit && hit.collider.gameObject == gameObject)
        {
            isDragging = true;
            offset = transform.position - (Vector3)mouseWorld;

            transform.localScale = originalScale * dragScale;
            transform.rotation = Quaternion.Euler(0, 0, Random.Range(-dragRotationRange, dragRotationRange));
        }
    }

    /// <summary>
    /// Moves the object to follow the current mouse position in world space, applying the specified offset.
    /// </summary>
    /// <remarks>
    /// This method is typically used to implement drag-and-drop behavior, updating the object's
    /// position to match the mouse cursor as it moves. The object's position is set based on the mouse's world
    /// coordinates plus any configured offset.
    /// </remarks>
    private void Drag()
    {
        Vector2 mouseWorld = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());

        // Preserve Z so the ornament doesn't jump layers/sorting unexpectedly.
        Vector3 target = (Vector3)mouseWorld + offset;
        target.z = transform.position.z;

        transform.position = target;
    }
}
