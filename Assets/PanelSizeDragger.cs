using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PanelSizeDragger : MonoBehaviour, IDragHandler
{
    /// <summary>
    /// Enum to define the orientation of the divider.
    /// </summary>
    public enum DividerDirection
    {
        Horizontal,
        Vertical
    }

    [Tooltip("The direction of the divider: Horizontal or Vertical.")]
    [SerializeField] private DividerDirection direction = DividerDirection.Horizontal;
    public RectTransform panel;

    [Header("Movement Constraint")]
    [Tooltip("Mark this if you find the results moves inversely, it's due to the different origin choice.")]
    [SerializeField] private bool invertMovement = false;
    [Tooltip("The minimum size of window.")]
    [SerializeField] public float minimumSize = 0.5f;

    private RectTransform rectTransform;

    public void OnEnable()
    {
        rectTransform = transform as RectTransform;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (panel == null) return;

        // Don't really understand why, but it's really unstable.
        // RectTransformUtility.ScreenPointToLocalPointInRectangle(panel, eventData.position, eventData.pressEventCamera, out Vector2 currentPointerLocalPos);
        RectTransformUtility.ScreenPointToWorldPointInRectangle(panel, eventData.position, eventData.pressEventCamera, out Vector3 worldPoint);
        // Compute new canvas size.
        Vector3 diffs = worldPoint - panel.position;

        if (direction == DividerDirection.Horizontal)
        {
            float width = Mathf.Abs(Vector3.Dot(diffs, panel.right)) * 2.0f;

            if (width < minimumSize) width = minimumSize;
            panel.sizeDelta = new Vector2(width, panel.sizeDelta.y);
        }
        else
        {
            float height = Mathf.Abs(Vector3.Dot(diffs, panel.up)) * 2.0f;

            if (height < minimumSize) height = minimumSize;
            panel.sizeDelta = new Vector2(panel.sizeDelta.x, height);
        }
    }

    public void FixedUpdate()
    {
        if (panel == null)
            return;
    }
}
