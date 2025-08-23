using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// A UI panel divider that supports both horizontal and vertical orientations.
/// Core Logic:
/// - In Horizontal mode, all panels in the same list (primary/secondary) will be set to the same width.
/// - In Vertical mode, all panels in the same list (primary/secondary) will be set to the same height.
/// </summary>
public class NewUIDivider : UIBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    /// <summary>
    /// Enum to define the orientation of the divider.
    /// </summary>
    public enum DividerDirection
    {
        Horizontal,
        Vertical
    }

    /// <summary>
    /// Determine to change only a single line (row, col) or all lines.
    /// </summary>
    public enum ControlRange
    {
        LocalLine,
        AllLine
    }

    [Header("Divider Settings")]
    [Tooltip("The direction of the divider: Horizontal or Vertical.")]
    [SerializeField] private DividerDirection direction = DividerDirection.Horizontal;
    [Tooltip("The control range of the divider, change only a single row/col or all the rows/cols")]
    [SerializeField] private ControlRange controlRange = ControlRange.AllLine;

    [Header("Movement Constraint")]
    [SerializeField] public float maxMovementRatio = 0.99f;
    [SerializeField] public float minMovementRatio = 0.01f;
    [Tooltip("Mark this if you find the results moves inversely, it's due to the different origin choice.")]
    [SerializeField] private bool invertMovement = false;

    // --- Drag State Variables ---
    private Vector2 pointerStartLocalPos;
    private Vector2 knobStartAnchoredPos;
    private readonly List<Vector2> startSecondaryPanelPositions = new List<Vector2>();
    private RectTransform parentRect;
    private RectTransform rectTransform;

    [Header("Controller")]
    public DynamicGridController gridController;
    [Tooltip("The row id this handle correspond to")]
    public int row;
    [Tooltip("The col id this handle correspond to")]
    public int col;

    // If we need to correct self position.
    private bool skipUpdatePosition = false;

    void Awake()
    {
        parentRect = transform.parent as RectTransform;
        rectTransform = transform as RectTransform;

        skipUpdatePosition = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // You can further improve this if you think the drag position isn't accurate enough.
        skipUpdatePosition = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Wonders why it doesn't trigger, so I have to stop using skip update position.
        skipUpdatePosition = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (parentRect == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, eventData.position, eventData.pressEventCamera, out Vector2 currentPointerLocalPos);

        if (direction == DividerDirection.Horizontal)
        {
            float parentWidth = parentRect.rect.width;
            float normalizedPos = currentPointerLocalPos.x * 0.5f + 0.5f;

            if (invertMovement)
                normalizedPos = 1.0f - normalizedPos;
            float localPos = Mathf.Clamp(normalizedPos, minMovementRatio, maxMovementRatio);
            float girdNormalizedCoord = localPos;

            Vector2 localPosVec2 = rectTransform.anchoredPosition;

            if (invertMovement)
                localPos = -localPos;

            if (Mathf.Abs(localPos - localPosVec2.x) < 1e-3)
                return;             // save time to change.

            localPosVec2.x = localPos * parentWidth;
            rectTransform.anchoredPosition = localPosVec2;

            if (gridController != null)
            {
                int changedRow = controlRange == ControlRange.AllLine ? -1 : row;
                gridController.ResizeColWidthTill(changedRow, col, girdNormalizedCoord);
            }
        }
        else
        {
            float parentHeight = parentRect.rect.height;
            // Local Position
            float normalizedPos = currentPointerLocalPos.y + 0.5f;

            if (invertMovement)
                normalizedPos = 1.0f - normalizedPos;
            float localPos = Mathf.Clamp(normalizedPos, minMovementRatio, maxMovementRatio);
            float girdNormalizedCoord = localPos;

            Vector2 localPosVec2 = rectTransform.anchoredPosition;

            if (invertMovement)
                localPos = -localPos;

            if (Mathf.Abs(localPos - localPosVec2.y) < 1e-3)
                return;             // save time to change.

            localPosVec2.y = localPos * parentHeight;
            rectTransform.anchoredPosition = localPosVec2;

            if (gridController != null)
            {
                int changedCol = controlRange == ControlRange.AllLine ? -1 : col;
                gridController.ResizeRowHeightTill(row, changedCol, girdNormalizedCoord);
            }
        }
    }

    public void FixedUpdate()
    {
        // No need to tick every frame I guess.
        if (skipUpdatePosition || gridController == null)
            return;

        /*
        // If it's a performance bottleneck, move it to some events.
        float localPosOnAxis = gridController.GetLocalHandlePositionOnAxis(row, col, direction == DividerDirection.Horizontal);
        Vector2 newPos = rectTransform.anchoredPosition;

        if (direction == DividerDirection.Horizontal)
            newPos.y = localPosOnAxis;
        else
            newPos.x = localPosOnAxis;
        rectTransform.anchoredPosition = newPos;
        */

        // If possible, you should use code above and only this code when canvas size changes.
        // But I don't want to spend time to implement a canvas notification system. Unity's builtin event cannot catch that.
        Vector2 localPos = gridController.GetLocalHandlePosition(row, col, direction == DividerDirection.Horizontal);
        rectTransform.anchoredPosition = localPos;
    }
}