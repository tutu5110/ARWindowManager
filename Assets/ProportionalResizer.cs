using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class ProportionalResizer : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    [Header("Controlled Elements")]
    [SerializeField] private RectTransform mainPanel;
    [SerializeField] private List<RectTransform> subPanels;

    [Header("Constraints (Currently Ignored)")]
    [SerializeField] private float minWidth = 200f;
    [SerializeField] private float maxWidth = 1000f;

    private struct PanelState
    {
        public Vector2 initialSize;
        public Vector2 initialPosition;
    }

    private RectTransform dragRectTransform;
    private RectTransform parentRect;

    private Vector2 pointerStartLocalPos;
    private Vector2 knobStartAnchoredPos;
    private float initialMainWidth;
    private readonly List<PanelState> initialSubPanelStates = new List<PanelState>();

    void Awake()
    {
        dragRectTransform = GetComponent<RectTransform>();
        parentRect = transform.parent as RectTransform;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (mainPanel == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, eventData.position, eventData.pressEventCamera, out pointerStartLocalPos);

        knobStartAnchoredPos = dragRectTransform.anchoredPosition;
        initialMainWidth = mainPanel.sizeDelta.x;

        initialSubPanelStates.Clear();
        if (subPanels != null)
        {
            foreach (var panel in subPanels)
            {
                initialSubPanelStates.Add(new PanelState
                {
                    initialSize = panel.sizeDelta,
                    initialPosition = panel.anchoredPosition
                });
            }
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (mainPanel == null || parentRect == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, eventData.position, eventData.pressEventCamera, out Vector2 currentPointerLocalPos);
        Vector2 pointerDelta = currentPointerLocalPos - pointerStartLocalPos;

        float newMainWidth = initialMainWidth + pointerDelta.x;

        // --- CHANGE HIGHLIGHT ---
        // 按照你的要求，暂时移除了 minWidth 和 maxWidth 的限制。
        // 这里只保留了宽度不能小于0的基础限制，以防止UI元素出错。
        float clampedMainWidth = Mathf.Max(0f, newMainWidth);
        // ------------------------

        mainPanel.sizeDelta = new Vector2(clampedMainWidth, mainPanel.sizeDelta.y);

        float actualWidthChange = clampedMainWidth - initialMainWidth;
        dragRectTransform.anchoredPosition = new Vector2(knobStartAnchoredPos.x + actualWidthChange, knobStartAnchoredPos.y);

        if (subPanels == null || subPanels.Count == 0 || Mathf.Approximately(initialMainWidth, 0)) return;

        float scaleRatio = clampedMainWidth / initialMainWidth;
        float currentXOffset = 0f;

        for (int i = 0; i < subPanels.Count; i++)
        {
            RectTransform currentSubPanel = subPanels[i];
            PanelState initialState = initialSubPanelStates[i];

            float newSubWidth = initialState.initialSize.x * scaleRatio;

            currentSubPanel.sizeDelta = new Vector2(newSubWidth, initialState.initialSize.y);
            currentSubPanel.anchoredPosition = new Vector2(currentXOffset, initialState.initialPosition.y);

            currentXOffset += newSubWidth;
        }
    }
}