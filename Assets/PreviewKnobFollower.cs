// All code in English, as requested.
using UnityEngine;
using UnityEngine.EventSystems; // Required for drag interfaces
using System.Collections.Generic;

/// <summary>
/// A multi-purpose knob script. 
/// In 'StickToTarget' mode, it follows a target's edge.
/// In 'Draggable' mode, it can be freely dragged by the user, leading its children.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class PreviewKnobFollower : MonoBehaviour, IPointerDownHandler, IDragHandler // Added drag interfaces
{
    // --- NEW: Behavior Mode Switch ---
    [Header("Behavior Mode")]
    [Tooltip("Choose the primary behavior of this knob.")]
    public BehaviorMode mode = BehaviorMode.StickToTarget;

    public enum BehaviorMode
    {
        StickToTarget, // The original behavior: snaps to the edge of the Target
        Draggable      // The new behavior: can be freely dragged by the user
    }

    // --- Original Settings ---
    [Header("Knob Follow Settings (StickToTarget Mode)")]
    public RectTransform target;   // The target RectTransform to follow
    public FollowPoint follow = FollowPoint.BottomCenter;
    public Vector2 offsetInTargetLocal = new Vector2(0, -24); // Offset in the target's local space
    public float zOffset = 0f;     // Lifts the knob on the Z-axis

    [Header("Follower Settings (Both Modes)")]
    [Tooltip("Constrain the movement axis for followers or dragging.")]
    public FollowConstraint constraint = FollowConstraint.FollowXY;

    [Tooltip("List of GameObjects that should follow this knob's movement.")]
    public List<GameObject> followingChildren = new List<GameObject>();


    // --- Enums ---
    public enum FollowPoint
    {
        LeftCenter, RightCenter, TopCenter, BottomCenter,
        TopLeft, TopRight, BottomLeft, BottomRight
    }

    public enum FollowConstraint
    {
        FollowX,  // Follow/Drag on X-axis only
        FollowY,  // Follow/Drag on Y-axis only
        FollowXY  // Follow/Drag on both X and Y axes
    }

    // --- Private Fields ---
    private RectTransform self;
    private Vector3 lastPosition; // Used only in StickToTarget mode

    // --- NEW: Private fields for Dragging ---
    private Canvas canvas;
    private Vector3 startDragWorldPosition;
    private Vector3 selfStartPosition;
    private List<Vector3> followerStartPositions = new List<Vector3>();

    void Awake()
    {
        self = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>(); // Get the parent canvas for drag calculations
        lastPosition = self.position;
    }

    // --- UPDATE: This logic now ONLY runs in StickToTarget mode ---
    void LateUpdate()
    {
        // If the mode is Draggable, this entire function is skipped to avoid conflict.
        if (mode != BehaviorMode.StickToTarget)
        {
            return;
        }

        // --- Part 1: Update the Knob's own position (original logic) ---
        if (!target) return;

        Vector3 localPoint = GetLocalPoint(target, follow);
        Vector3 worldPoint = target.TransformPoint(localPoint + (Vector3)offsetInTargetLocal);
        worldPoint += target.forward * zOffset;
        self.position = worldPoint;
        self.rotation = target.rotation;

        // --- Part 2: Make children follow the Knob's movement (original logic) ---
        Vector3 deltaMovement = self.position - lastPosition;
        if (deltaMovement != Vector3.zero)
        {
            ApplyMovementToFollowers(deltaMovement);
        }

        lastPosition = self.position;
    }

    // --- NEW: Dragging Logic ---

    public void OnPointerDown(PointerEventData eventData)
    {
        // This method only works in Draggable mode.
        if (mode != BehaviorMode.Draggable) return;

        // Record the starting positions of this object and all its followers
        selfStartPosition = self.position;
        followerStartPositions.Clear();
        if (followingChildren != null)
        {
            foreach (var child in followingChildren)
            {
                if (child != null)
                {
                    followerStartPositions.Add(child.transform.position);
                }
            }
        }

        RectTransformUtility.ScreenPointToWorldPointInRectangle(self, eventData.position, eventData.pressEventCamera, out startDragWorldPosition);
    }

    public void OnDrag(PointerEventData eventData)
    {
        // This method only works in Draggable mode.
        if (mode != BehaviorMode.Draggable) return;

        Vector3 currentWorldPosition;
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(self, eventData.position, eventData.pressEventCamera, out currentWorldPosition))
        {
            Vector3 worldDelta = currentWorldPosition - startDragWorldPosition;

            // Apply movement to self and all followers based on the calculated delta
            ApplyMovementToFollowers(worldDelta, true);
        }
    }

    // --- NEW: Helper method to move followers, used by both modes ---

    private void ApplyMovementToFollowers(Vector3 delta, bool isDragMode = false)
    {
        // Constrain the movement delta based on the enum setting
        switch (constraint)
        {
            case FollowConstraint.FollowX:
                delta.y = 0;
                break;
            case FollowConstraint.FollowY:
                delta.x = 0;
                break;
        }
        delta.z = 0; // Always keep Z the same for UI

        // If in Drag mode, apply movement to self as well
        if (isDragMode)
        {
            self.position = selfStartPosition + delta;
        }

        // Apply movement to all followers
        if (followingChildren != null)
        {
            for (int i = 0; i < followingChildren.Count; i++)
            {
                if (followingChildren[i] != null)
                {
                    if (isDragMode && i < followerStartPositions.Count)
                    {
                        // In drag mode, use start positions for precision
                        followingChildren[i].transform.position = followerStartPositions[i] + delta;
                    }
                    else if (!isDragMode)
                    {
                        // In stick mode, apply delta frame by frame
                        followingChildren[i].transform.position += delta;
                    }
                }
            }
        }
    }


    // This helper function remains unchanged
    static Vector3 GetLocalPoint(RectTransform rt, FollowPoint p)
    {
        Vector2 pv = rt.pivot;
        float w = rt.sizeDelta.x, h = rt.sizeDelta.y;
        Vector3 leftCenter = new Vector3(-pv.x * w, (0.5f - pv.y) * h, 0);
        Vector3 rightCenter = new Vector3((1f - pv.x) * w, (0.5f - pv.y) * h, 0);
        Vector3 topCenter = new Vector3((0.5f - pv.x) * w, (1f - pv.y) * h, 0);
        Vector3 bottomCenter = new Vector3((0.5f - pv.x) * w, -pv.y * h, 0);
        Vector3 topLeft = new Vector3(-pv.x * w, (1f - pv.y) * h, 0);
        Vector3 topRight = new Vector3((1f - pv.x) * w, (1f - pv.y) * h, 0);
        Vector3 bottomLeft = new Vector3(-pv.x * w, -pv.y * h, 0);
        Vector3 bottomRight = new Vector3((1f - pv.x) * w, -pv.y * h, 0);
        switch (p)
        {
            case FollowPoint.LeftCenter: return leftCenter;
            case FollowPoint.RightCenter: return rightCenter;
            case FollowPoint.TopCenter: return topCenter;
            case FollowPoint.BottomCenter: return bottomCenter;
            case FollowPoint.TopLeft: return topLeft;
            case FollowPoint.TopRight: return topRight;
            case FollowPoint.BottomLeft: return bottomLeft;
            case FollowPoint.BottomRight: return bottomRight;
        }
        return Vector3.zero;
    }
}