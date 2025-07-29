using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public class PanelDraggable : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    // --- Member Variables ---
    private Vector3 dragStartPanelPos;
    private Camera mainCamera;
    private bool isDragging = false;
    private bool hasTriggeredOverlap = false;
    private int triggeredIndex = -1; // Encoded as regionIdx*10+subIdx
    private Vector3 dragStartWorldPos;
    private bool overlapCooldown = false;

    public struct PanelState
    {
        public Transform parent;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 localScale;
        public Vector2 sizeDelta;
        public Vector2 anchorMin, anchorMax, pivot;
    }

    private PanelState initialState;

    [Header("Drag Configuration")]
    public float detectionYMax = 0;
    public Texture2D dragCursor;
    public Vector2 cursorHotspot = Vector2.zero;

    [Header("Object References")]
    public List<Transform> regions = new List<Transform>();
    public List<RectTransform> panelList = new List<RectTransform>();

    private RectTransform rectTransform;
    private RectTransform swapTarget = null;

    // Dictionaries for panel visuals
    private Dictionary<RectTransform, Image> panelBackgrounds = new Dictionary<RectTransform, Image>();
    private Dictionary<RectTransform, Color> panelOriginalColors = new Dictionary<RectTransform, Color>();

    // Lists for hot regions and previews
    private List<Image> allSubHotRegionBackgrounds = new List<Image>();
    private List<List<RectTransform>> hotRegionRefsList = new List<List<RectTransform>>();
    private List<List<RectTransform>> previewPanelsList = new List<List<RectTransform>>();
    private List<RectTransform> preview_swaps = new List<RectTransform>();

    void Start()
    {
        if (EventSystem.current != null)
        {
            EventSystem.current.pixelDragThreshold = 1;
        }

        mainCamera = Camera.main;
        rectTransform = GetComponent<RectTransform>();

        hotRegionRefsList.Clear();
        previewPanelsList.Clear();
        preview_swaps.Clear();

        for (int i = 0; i < regions.Count; i++)
        {
            var hotList = new List<RectTransform>();
            var previewList = new List<RectTransform>();

            string[] hotSuffix = { "act", "sub1", "sub2", "sub3", "sub4" };
            string[] previewSuffix = { "", "_sub1", "_sub2", "_sub3", "_sub4" };

            for (int k = 0; k < hotSuffix.Length; k++)
            {
                string hotName = $"Hotregion_{i + 1}_{hotSuffix[k]}";
                Transform hot = regions[i]?.Find(hotName);
                hotList.Add(hot ? hot.GetComponent<RectTransform>() : null);

                string previewName = $"preview_{i + 1}{previewSuffix[k]}";
                Transform preview = regions[i]?.Find(previewName);
                previewList.Add(preview ? preview.GetComponent<RectTransform>() : null);
            }

            hotRegionRefsList.Add(hotList);
            previewPanelsList.Add(previewList);

            Transform swap = regions[i]?.Find("swap_prev");
            preview_swaps.Add(swap ? swap.GetComponent<RectTransform>() : null);
        }

        foreach (var panel in panelList)
        {
            var bg = panel.Find("Background");
            if (bg != null)
            {
                var img = bg.GetComponent<Image>();
                if (img != null)
                {
                    panelBackgrounds[panel] = img;
                    panelOriginalColors[panel] = img.color;
                }
            }
        }

        for (int i = 0; i < hotRegionRefsList.Count; i++)
        {
            foreach (var hotRect in hotRegionRefsList[i])
            {
                if (hotRect != null)
                {
                    var bgTrans = hotRect.Find("Background");
                    if (bgTrans != null)
                    {
                        var img = bgTrans.GetComponent<Image>();
                        if (img != null)
                            allSubHotRegionBackgrounds.Add(img);
                    }
                }
            }
        }
    }

    void Update()
    {
        if (isDragging) return;

        Vector2 pointerPos = Input.mousePosition;

        foreach (var panel in panelList)
        {
            if (!panelBackgrounds.ContainsKey(panel)) continue;

            bool isInside = RectTransformUtility.ScreenPointToLocalPointInRectangle(
                panel, pointerPos, mainCamera, out Vector2 localPoint) && panel.rect.Contains(localPoint);

            var img = panelBackgrounds[panel];
            if (isInside)
            {
                Color baseColor = panelOriginalColors[panel];
                float alpha = Mathf.Lerp(100f / 255f, 166f / 255f, Mathf.PingPong(Time.time * 1.2f, 1f));
                img.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
            }
            else
            {
                img.color = panelOriginalColors[panel];
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.position.y > detectionYMax)
        {
            isDragging = true;
            dragStartPanelPos = transform.position;
            dragStartWorldPos = eventData.pointerPressRaycast.worldPosition;

            if (dragCursor)
                Cursor.SetCursor(dragCursor, cursorHotspot, CursorMode.Auto);

            initialState = new PanelState
            {
                parent = rectTransform.parent,
                position = rectTransform.position,
                rotation = rectTransform.rotation,
                localScale = rectTransform.localScale,
                sizeDelta = rectTransform.sizeDelta,
                anchorMin = rectTransform.anchorMin,
                anchorMax = rectTransform.anchorMax,
                pivot = rectTransform.pivot
            };
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        transform.position = dragStartPanelPos + (eventData.pointerCurrentRaycast.worldPosition - dragStartWorldPos);

        if (overlapCooldown)
        {
            if (!CheckAnyOverlap(out _, out _))
            {
                overlapCooldown = false;
            }
            return;
        }

        bool anyOverlap = CheckAnyOverlap(out int regionIdx, out int subIdx);

        UpdatePreviewHighlight(regionIdx, subIdx, anyOverlap);
        ResetAllHotregionAlphas();

        if (anyOverlap && (regionIdx >= 0 && subIdx >= 0))
        {
            if (!hasTriggeredOverlap || triggeredIndex != regionIdx * 10 + subIdx)
            {
                hasTriggeredOverlap = true;
                triggeredIndex = regionIdx * 10 + subIdx;
            }
            HighlightHotregion(regionIdx, subIdx);
        }
        else
        {
            hasTriggeredOverlap = false;
            triggeredIndex = -1;
        }

        HandlePanelSwap();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isDragging) return;

        // --- THIS IS THE CORRECTED LOGIC ---

        // 1. Finalize position if dropped on a hot region
        if (hasTriggeredOverlap && triggeredIndex >= 0)
        {
            int regionIdx = triggeredIndex / 10;
            int subIdx = triggeredIndex % 10;

            if (regionIdx < previewPanelsList.Count && subIdx < previewPanelsList[regionIdx].Count)
            {
                RectTransform previewRect = previewPanelsList[regionIdx][subIdx];
                if (previewRect != null)
                {
                    // Snap to the preview panel's transform
                    RestorePanel(rectTransform, new PanelState
                    {
                        parent = previewRect.parent,
                        position = previewRect.position,
                        rotation = previewRect.rotation,
                        localScale = previewRect.localScale,
                        sizeDelta = previewRect.sizeDelta,
                        anchorMin = previewRect.anchorMin,
                        anchorMax = previewRect.anchorMax,
                        pivot = previewRect.pivot
                    });
                    overlapCooldown = true;
                }
            }
        }
        // 2. Else, finalize panel swap if a target exists
        else if (swapTarget != null)
        {
            // The logic here swaps the initial state of the dragged panel
            // with the current state of the target panel.
            SwapPanels(rectTransform, swapTarget);
        }

        // 3. If neither of the above, DO NOTHING. The panel now stays where you dropped it.
        // The incorrect 'else' block that restored the panel has been removed.

        // --- Cleanup ---
        isDragging = false;
        hasTriggeredOverlap = false;
        triggeredIndex = -1;
        swapTarget = null;

        // Hide all previews
        foreach (var previewList in previewPanelsList)
        {
            foreach (var preview in previewList)
            {
                if (preview != null) preview.gameObject.SetActive(false);
            }
        }
        foreach (var preview in preview_swaps)
        {
            if (preview != null) preview.gameObject.SetActive(false);
        }

        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    #region Helper Methods

    private bool CheckAnyOverlap(out int regionIdx, out int subIdx)
    {
        for (int i = 0; i < hotRegionRefsList.Count; i++)
        {
            for (int k = 0; k < hotRegionRefsList[i].Count; k++)
            {
                var hotRect = hotRegionRefsList[i][k];
                if (hotRect != null && RectTransformWorldOverlap(this.rectTransform, hotRect))
                {
                    regionIdx = i;
                    subIdx = k;
                    return true;
                }
            }
        }
        regionIdx = -1;
        subIdx = -1;
        return false;
    }

    private void HandlePanelSwap()
    {
        swapTarget = null;
        foreach (var p in panelList)
        {
            if (p == null || p == rectTransform) continue;

            if (RectTransformWorldOverlap(rectTransform, p))
            {
                swapTarget = p;
                int swapIdx = panelList.IndexOf(p);
                int myIdx = panelList.IndexOf(rectTransform);

                for (int j = 0; j < preview_swaps.Count; j++)
                {
                    if (preview_swaps[j] != null)
                        preview_swaps[j].gameObject.SetActive(j == swapIdx || j == myIdx);
                }
                return;
            }
        }

        foreach (var preview in preview_swaps)
        {
            if (preview != null)
                preview.gameObject.SetActive(false);
        }
    }

    void QuarterizeCorners(Vector3[] corners, float scale, float xOffset, float yOffset)
    {
        Vector3 leftTop = corners[1];
        for (int i = 0; i < 4; i++)
        {
            corners[i] = leftTop + (corners[i] - leftTop) * scale;
            corners[i] += new Vector3(xOffset, yOffset, 0);
        }
    }

    bool RectTransformWorldOverlap(RectTransform a, RectTransform b)
    {
        if (a == null || b == null) return false;
        Vector3[] cornersA = new Vector3[4];
        Vector3[] cornersB = new Vector3[4];
        a.GetWorldCorners(cornersA);
        b.GetWorldCorners(cornersB);

        QuarterizeCorners(cornersA, 0.25f, 0, 0);
        // AABB
        float axMin = Mathf.Min(cornersA[0].x, cornersA[1].x, cornersA[2].x, cornersA[3].x);
        float axMax = Mathf.Max(cornersA[0].x, cornersA[1].x, cornersA[2].x, cornersA[3].x);
        float ayMin = Mathf.Min(cornersA[0].y, cornersA[1].y, cornersA[2].y, cornersA[3].y);
        float ayMax = Mathf.Max(cornersA[0].y, cornersA[1].y, cornersA[2].y, cornersA[3].y);

        float bxMin = Mathf.Min(cornersB[0].x, cornersB[1].x, cornersB[2].x, cornersB[3].x);
        float bxMax = Mathf.Max(cornersB[0].x, cornersB[1].x, cornersB[2].x, cornersB[3].x);
        float byMin = Mathf.Min(cornersB[0].y, cornersB[1].y, cornersB[2].y, cornersB[3].y);
        float byMax = Mathf.Max(cornersB[0].y, cornersB[1].y, cornersB[2].y, cornersB[3].y);

        bool overlap = axMax > bxMin && axMin < bxMax && ayMax > byMin && ayMin < byMax;
        return overlap;
    }

    private void UpdatePreviewHighlight(int regionIdx, int subIdx, bool anyOverlap)
    {
        for (int i = 0; i < previewPanelsList.Count; i++)
        {
            for (int k = 0; k < previewPanelsList[i].Count; k++)
            {
                if (previewPanelsList[i][k] != null)
                    previewPanelsList[i][k].gameObject.SetActive(i == regionIdx && k == subIdx && anyOverlap);
            }
        }
    }

    private void ResetAllHotregionAlphas()
    {
        foreach (var img in allSubHotRegionBackgrounds)
        {
            Color c = img.color;
            c.a = 59f / 255f;
            img.color = c;
        }
    }

    private void HighlightHotregion(int regionIdx, int subIdx)
    {
        if (regionIdx < hotRegionRefsList.Count && subIdx < hotRegionRefsList[regionIdx].Count)
        {
            var hotRect = hotRegionRefsList[regionIdx][subIdx];
            if (hotRect != null)
            {
                var bgTrans = hotRect.Find("Background");
                if (bgTrans != null && bgTrans.TryGetComponent<Image>(out var img))
                {
                    Color c = img.color;
                    c.a = 240f / 255f;
                    img.color = c;
                }
            }
        }
    }

    private void SwapPanels(RectTransform panelA, RectTransform panelB)
    {
        // Capture the target's current state
        var stateB = new PanelState
        {
            parent = panelB.parent,
            position = panelB.position,
            rotation = panelB.rotation,
            localScale = panelB.localScale,
            sizeDelta = panelB.sizeDelta,
            anchorMin = panelB.anchorMin,
            anchorMax = panelB.anchorMax,
            pivot = panelB.pivot
        };

        // panelA started at 'initialState'. So we move panelB to that state.
        RestorePanel(panelB, this.initialState);
        // And we move panelA to panelB's old state.
        RestorePanel(panelA, stateB);
    }

    private void RestorePanel(RectTransform panel, PanelState state)
    {
        panel.SetParent(state.parent, true);
        panel.position = state.position;
        panel.rotation = state.rotation;
        panel.localScale = state.localScale;
        panel.sizeDelta = state.sizeDelta;
        panel.anchorMin = state.anchorMin;
        panel.anchorMax = state.anchorMax;
        panel.pivot = state.pivot;
    }

    #endregion
}