using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// A UI panel divider that supports both horizontal and vertical orientations.
/// Core Logic:
/// - In Horizontal mode, all panels in the same list (primary/secondary) will be set to the same width.
/// - In Vertical mode, all panels in the same list (primary/secondary) will be set to the same height.
/// </summary>
public class UIDivider : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    /// <summary>
    /// Enum to define the orientation of the divider.
    /// </summary>
    public enum DividerDirection
    {
        Horizontal,
        Vertical
    }

    [Header("Divider Settings")]
    [Tooltip("The direction of the divider: Horizontal or Vertical.")]
    [SerializeField] private DividerDirection direction = DividerDirection.Horizontal;

    [Header("UI Elements")]
    [Tooltip("The RectTransform of the knob (the draggable handle).")]
    [SerializeField] private RectTransform knob;

    [Tooltip("The list of primary panels (Left panels in Horizontal mode, Upper panels in Vertical mode).")]
    [SerializeField] private List<RectTransform> panelsPrimary = new List<RectTransform>();
    [Tooltip("The list of secondary panels (Right panels in Horizontal mode, Lower panels in Vertical mode).")]
    [SerializeField] private List<RectTransform> panelsSecondary = new List<RectTransform>();

    [Header("Horizontal Constraints")]
    [Tooltip("[Horizontal Mode] The minimum local X-position the knob can move to.")]
    [SerializeField] private float knobLocalX_Min = -0.899f;
    [Tooltip("[Horizontal Mode] The maximum local X-position the knob can move to.")]
    [SerializeField] private float knobLocalX_Max = 0f;
    [Tooltip("[Horizontal Mode] The combined total width of the primary and secondary panel groups.")]
    [SerializeField] private float totalPanelsWidth = 1200f;
    [Tooltip("[Horizontal Mode] The minimum width for either of the panel groups.")]
    [SerializeField] private float minPanelWidth = 100f;

    [Header("Vertical Constraints")]
    [Tooltip("[Vertical Mode] The minimum local Y-position the knob can move to.")]
    [SerializeField] private float knobLocalY_Min = -500f;
    [Tooltip("[Vertical Mode] The maximum local Y-position the knob can move to.")]
    [SerializeField] private float knobLocalY_Max = 0f;
    [Tooltip("[Vertical Mode] The combined total height of the primary and secondary panel groups.")]
    [SerializeField] private float totalPanelsHeight = 1000f;
    [Tooltip("[Vertical Mode] The minimum height for either of the panel groups.")]
    [SerializeField] private float minPanelHeight = 100f;

    // --- Drag State Variables ---
    private Vector2 pointerStartLocalPos;
    private Vector2 knobStartAnchoredPos;
    private readonly List<Vector2> startSecondaryPanelPositions = new List<Vector2>();
    private RectTransform parentRect;

    void Awake()
    {
        parentRect = transform.parent as RectTransform;
        ValidateConstraints();
    }

    /// <summary>
    /// Validates the constraints in the editor and at runtime to ensure they are logical.
    /// </summary>
    private void ValidateConstraints()
    {
        if (direction == DividerDirection.Horizontal)
        {
            if (minPanelWidth * 2 > totalPanelsWidth)
            {
                Debug.LogWarning("Horizontal constraints are illogical: 'minPanelWidth' * 2 cannot be greater than 'totalPanelsWidth'.");
            }
        }
        else // Vertical
        {
            if (minPanelHeight * 2 > totalPanelsHeight)
            {
                Debug.LogWarning("Vertical constraints are illogical: 'minPanelHeight' * 2 cannot be greater than 'totalPanelsHeight'.");
            }
        }
    }


    public void OnPointerDown(PointerEventData eventData)
    {
        if (knob == null || (panelsPrimary.Count == 0 && panelsSecondary.Count == 0)) return;

        // Convert screen point to local point in the parent RectTransform
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, eventData.position, eventData.pressEventCamera, out pointerStartLocalPos);
        knobStartAnchoredPos = knob.anchoredPosition;

        // Cache the initial anchored positions of all secondary panels (Right or Lower)
        startSecondaryPanelPositions.Clear();
        foreach (var panel in panelsSecondary)
        {
            startSecondaryPanelPositions.Add(panel.anchoredPosition);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (parentRect == null) return;

        // Based on the current direction, call the corresponding handler function
        if (direction == DividerDirection.Horizontal)
        {
            HandleHorizontalDrag(eventData);
        }
        else
        {
            HandleVerticalDrag(eventData);
        }
    }

    /// <summary>
    /// Handles the drag logic for the Horizontal direction.
    /// </summary>
    private void HandleHorizontalDrag(PointerEventData eventData)
    {
        // --- Step 1: Calculate the pointer's delta in local coordinates ---
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, eventData.position, eventData.pressEventCamera, out Vector2 currentPointerLocalPos);
        Vector2 pointerLocalDelta = currentPointerLocalPos - pointerStartLocalPos;

        // --- Step 2: Calculate the new panel widths based on the delta and constraints ---
        float desiredKnobX = knobStartAnchoredPos.x + pointerLocalDelta.x;
        float totalKnobRange = knobLocalX_Max - knobLocalX_Min;
        if (Mathf.Approximately(totalKnobRange, 0)) return;

        float desiredRatio = (desiredKnobX - knobLocalX_Min) / totalKnobRange;
        float desiredLeftWidth = totalPanelsWidth * desiredRatio;

        // Apply the minimum/maximum width constraints
        float maxPanelWidth = totalPanelsWidth - minPanelWidth;
        float newPrimaryWidth = Mathf.Clamp(desiredLeftWidth, minPanelWidth, maxPanelWidth);
        float newSecondaryWidth = totalPanelsWidth - newPrimaryWidth;

        // --- Step 3: Based on the final calculated width, deduce and update the knob's exact position ---
        float finalRatio = newPrimaryWidth / totalPanelsWidth;
        float finalClampedKnobX = knobLocalX_Min + totalKnobRange * finalRatio;
        knob.anchoredPosition = new Vector2(finalClampedKnobX, knob.anchoredPosition.y);

        // --- Step 4: Update the size of all primary (Left) panels ---
        foreach (var panel in panelsPrimary)
        {
            panel.sizeDelta = new Vector2(newPrimaryWidth, panel.sizeDelta.y);
        }

        // --- Step 5: Update the size of all secondary (Right) panels ---
        foreach (var panel in panelsSecondary)
        {
            panel.sizeDelta = new Vector2(newSecondaryWidth, panel.sizeDelta.y);
        }

        // --- Step 6: Update the position of all secondary (Right) panels ---
        float effectiveLocalDeltaX = finalClampedKnobX - knobStartAnchoredPos.x;
        for (int i = 0; i < panelsSecondary.Count; i++)
        {
            panelsSecondary[i].anchoredPosition = new Vector2(startSecondaryPanelPositions[i].x + effectiveLocalDeltaX, startSecondaryPanelPositions[i].y);
        }
    }

    /// <summary>
    /// Handles the drag logic for the Vertical direction (mirrors the horizontal logic).
    /// </summary>
    private void HandleVerticalDrag(PointerEventData eventData)
    {
        // --- Step 1: Calculate the pointer's delta in local coordinates ---
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, eventData.position, eventData.pressEventCamera, out Vector2 currentPointerLocalPos);
        Vector2 pointerLocalDelta = currentPointerLocalPos - pointerStartLocalPos;

        // --- Step 2: Calculate the new panel heights based on the delta and constraints ---
        float desiredKnobY = knobStartAnchoredPos.y + pointerLocalDelta.y;
        float totalKnobRange = knobLocalY_Max - knobLocalY_Min;
        if (Mathf.Approximately(totalKnobRange, 0)) return;

        // NOTE: In vertical mode, we assume the upper panel's ratio increases as the knob moves up.
        // Hence, the numerator is (desiredKnobY - minY).
        float desiredRatio = (desiredKnobY - knobLocalY_Min) / totalKnobRange;
        float desiredUpperHeight = totalPanelsHeight * desiredRatio;

        // Apply the minimum/maximum height constraints
        float maxPanelHeight = totalPanelsHeight - minPanelHeight;
        float newPrimaryHeight = Mathf.Clamp(desiredUpperHeight, minPanelHeight, maxPanelHeight);
        float newSecondaryHeight = totalPanelsHeight - newPrimaryHeight;

        // --- Step 3: Based on the final calculated height, deduce and update the knob's exact position ---
        float finalRatio = newPrimaryHeight / totalPanelsHeight;
        float finalClampedKnobY = knobLocalY_Min + totalKnobRange * finalRatio;
        knob.anchoredPosition = new Vector2(knob.anchoredPosition.x, finalClampedKnobY);

        // --- Step 4: Update the size of all primary (Upper) panels ---
        foreach (var panel in panelsPrimary)
        {
            panel.sizeDelta = new Vector2(panel.sizeDelta.x, newPrimaryHeight);
        }

        // --- Step 5: Update the size of all secondary (Lower) panels ---
        foreach (var panel in panelsSecondary)
        {
            panel.sizeDelta = new Vector2(panel.sizeDelta.x, newSecondaryHeight);
        }

        // --- Step 6: Update the position of all secondary (Lower) panels ---
        float effectiveLocalDeltaY = finalClampedKnobY - knobStartAnchoredPos.y;
        for (int i = 0; i < panelsSecondary.Count; i++)
        {
            panelsSecondary[i].anchoredPosition = new Vector2(startSecondaryPanelPositions[i].x, startSecondaryPanelPositions[i].y + effectiveLocalDeltaY);
        }
    }
}