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

    private float previewInactiveAlpha = 0f;
    private float previewActiveAlpha = 1f;
    private List<List<Image>> previewBackgroundsList = new List<List<Image>>();

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
            var previewBgList = new List<Image>();

            string[] hotSuffix = { "WindowPanel/preview_parent/Container/Row_0/preview_sub1/Hotregion_sub1",
                "WindowPanel/preview_parent/Container/Row_0/preview_sub2/Hotregion_sub2",
                "WindowPanel/preview_parent/Container/Row_1/preview_sub3/Hotregion_sub3",
                "WindowPanel/preview_parent/Container/Row_1/preview_sub4/Hotregion_sub4"};
            string[] previewPaths = {
                "WindowPanel/preview_parent/Container/Row_0/preview_sub1",
                "WindowPanel/preview_parent/Container/Row_0/preview_sub2",
                "WindowPanel/preview_parent/Container/Row_1/preview_sub3",
                "WindowPanel/preview_parent/Container/Row_1/preview_sub4"
            };

            for (int k = 0; k < hotSuffix.Length; k++)
            {
                string hotName = hotSuffix[k];
                Transform hot = regions[i]?.Find(hotName);
                
                if (hot != null)
                    hotList.Add(hot.GetComponent<RectTransform>());

            }

            for (int k = 0; k < previewPaths.Length; k++)
            {
                var t = regions[i]?.Find(previewPaths[k]);
                if (t == null)
                    continue;

                    previewList.Add(t ? t.GetComponent<RectTransform>() : null);
                Image bgImg = null;
 
                    var bgT = t.Find("Background");
                    if (bgT != null)
                    {
                        bgImg = bgT.GetComponent<Image>();
                        // 不要拦截事件
                        if (bgImg != null) bgImg.raycastTarget = false;
                    }
                previewBgList.Add(bgImg);
            }

            hotRegionRefsList.Add(hotList);
            previewPanelsList.Add(previewList);
            previewBackgroundsList.Add(previewBgList);

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
            Vector3 worldPos = eventData.pointerPressRaycast.worldPosition;
            // dragStartWorldPos = eventData.pointerPressRaycast.worldPosition;
            if (worldPos == Vector3.zero)
            {
                RectTransformUtility.ScreenPointToWorldPointInRectangle(
                    rectTransform,
                    eventData.position,
                    mainCamera,
                    out worldPos
                );
            }
            dragStartWorldPos = worldPos;

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

        // transform.position = dragStartPanelPos + (eventData.pointerCurrentRaycast.worldPosition - dragStartWorldPos);
       // Debug.Log("Raycast world pos: " + eventData.pointerCurrentRaycast.worldPosition);

        // 先尝试用 pointerCurrentRaycast.worldPosition
        Vector3 curWorld = eventData.pointerCurrentRaycast.worldPosition;
        // 如果得到 (0,0,0) 就回退到 ScreenPointToWorldPointInRectangle
        if (curWorld == Vector3.zero)
        {
            // 用触发该事件的摄像机（Overlay 模式下为 null）
            Camera cam = eventData.pressEventCamera;
            // 投射到 rectTransform 所在平面
            bool got = RectTransformUtility.ScreenPointToWorldPointInRectangle(
                rectTransform,
                eventData.position,
                cam,
                out curWorld
            );
            if (!got)
            {
                // 都拿不到就不更新位置，避免跳回原点
                return;
            }
        }
        Vector3 delta = curWorld - dragStartWorldPos;
        Vector3 newPos = dragStartPanelPos + delta;
        newPos.z = dragStartPanelPos.z;
        transform.position = newPos;


        // 四 冷却期内只检测何时退出
        if (overlapCooldown)
        {
            if (!CheckAnyOverlap(out _, out _))
                overlapCooldown = false;
            return;
        }

        // 五 普通的高亮与预览逻辑
        bool anyOverlap = CheckAnyOverlap(out int regionIdx, out int subIdx);
        Debug.Log(anyOverlap);
        UpdatePreviewHighlight(regionIdx, subIdx, anyOverlap);
        ResetAllHotregionAlphas();

        if (anyOverlap && regionIdx >= 0 && subIdx >= 0)
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
                    updatePanelPosition(rectTransform, previewRect);
                    overlapCooldown = true;
                }
            }
        }


        // --- Cleanup ---
        isDragging = false;
        hasTriggeredOverlap = false;
        triggeredIndex = -1;
        swapTarget = null;

        SetAllPreviewBackgroundsAlpha(previewInactiveAlpha);

        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }


    #region Helper Methods
    private void SetImageAlpha(Image img, float a)
    {
        if (img == null) return;
        var c = img.color;
        c.a = a;
        img.color = c;
    }

    private void SetAllPreviewBackgroundsAlpha(float a)
    {
        for (int i = 0; i < previewBackgroundsList.Count; i++)
            for (int k = 0; k < previewBackgroundsList[i].Count; k++)
                SetImageAlpha(previewBackgroundsList[i][k], a);
    }


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

       // QuarterizeCorners(cornersA, 0.25f, 0, 0);
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
        for (int i = 0; i < previewBackgroundsList.Count; i++)
        {
            for (int k = 0; k < previewBackgroundsList[i].Count; k++)
            {
                var img = previewBackgroundsList[i][k];
                if (img == null) continue;

                float a = (i == regionIdx && k == subIdx && anyOverlap)
                    ? previewActiveAlpha
                    : previewInactiveAlpha;

                // 避免每帧反复写
                if (!Mathf.Approximately(img.color.a, a))
                {
                    var c = img.color;
                    c.a = a;
                    img.color = c;
                }
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

    private void updatePanelPosition(RectTransform panel, RectTransform target_zone) {
        panel.position = target_zone.position;
        panel.rotation = target_zone.rotation;

        float w = target_zone.rect.width * 742.5380710659898f;
        float h = target_zone.rect.height * 743.9328590697916f;

        panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w);
        panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h);

    }
    private void RestorePanel(RectTransform panel, PanelState state)
    {
        //panel.SetParent(state.parent, true);
        panel.position = state.position;
        panel.rotation = state.rotation;
  

      // panel.localScale = state.localScale;
       // panel.sizeDelta = state.sizeDelta;
       // panel.anchorMin = state.anchorMin;
       // panel.anchorMax = state.anchorMax;
       // panel.pivot = state.pivot;
    }

    #endregion
}