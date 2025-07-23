using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public class PanelDraggable : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    private Vector3 dragStartPanelPos;
    private Vector2 dragStartMouseScreenPos;
    private Camera mainCamera;
    private bool isDragging = false;
    private bool hasTriggeredOverlap = false;
    private int triggeredIndex = -1;

    // 拖拽检测相关
    public float detectionYMax = 0;
    public bool visualizeQuarter = false;
    [Range(0f, 1f)]
    public float draggableTopPercent = 0.1f;

    public List<Transform> regions = new List<Transform>(); // 支持多个region

    private List<RectTransform> hotRegionRefs = new List<RectTransform>();
    private List<RectTransform> preview_panels = new List<RectTransform>();
    private RectTransform rectTransform;

    // 鼠标图标
    public Texture2D dragCursor;
    public Vector2 cursorHotspot = Vector2.zero;

    void Start()
    {
        mainCamera = Camera.main;
        rectTransform = GetComponent<RectTransform>();

        // 自动查找每个region下的Hotregion_x_act和preview_x
        hotRegionRefs.Clear();
        preview_panels.Clear();

        for (int i = 0; i < regions.Count; i++)
        {
            string hotName = $"Hotregion_{i + 1}_act";
            string previewName = $"preview_{i + 1}";

            Transform hot = regions[i] != null ? regions[i].Find(hotName) : null;
            if (hot != null)
                hotRegionRefs.Add(hot.GetComponent<RectTransform>());
            else
                hotRegionRefs.Add(null);

            Transform preview = regions[i] != null ? regions[i].Find(previewName) : null;
            if (preview != null)
                preview_panels.Add(preview.GetComponent<RectTransform>());
            else
                preview_panels.Add(null);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, mainCamera, out localPoint);

        if (eventData.position.y > detectionYMax)
        {
            isDragging = true;
            dragStartPanelPos = transform.position;
            dragStartMouseScreenPos = eventData.position;

            if (dragCursor)
                Cursor.SetCursor(dragCursor, cursorHotspot, CursorMode.Auto);
        }
        else
        {
            isDragging = false;
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
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

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        Vector2 deltaScreen = (Vector2)eventData.position - dragStartMouseScreenPos;
        float z = mainCamera.WorldToScreenPoint(transform.position).z;
        Vector3 startWorld = mainCamera.ScreenToWorldPoint(new Vector3(dragStartMouseScreenPos.x, dragStartMouseScreenPos.y, z));
        Vector3 currWorld = mainCamera.ScreenToWorldPoint(new Vector3(eventData.position.x, eventData.position.y, z));
        Vector3 worldDelta = (currWorld - startWorld) * 6;
        transform.position = dragStartPanelPos + worldDelta;

        bool anyOverlap = false;
        int hitIdx = -1;
        for (int i = 0; i < hotRegionRefs.Count; i++)
        {
            if (hotRegionRefs[i] != null && RectTransformWorldOverlap(this.rectTransform, hotRegionRefs[i]))
            {
                anyOverlap = true;
                hitIdx = i;
                break;
            }
        }

        if (anyOverlap && hitIdx >= 0)
        {
            for (int j = 0; j < preview_panels.Count; j++)
                if (preview_panels[j] != null)
                    preview_panels[j].gameObject.SetActive(j == hitIdx);

            if (!hasTriggeredOverlap || triggeredIndex != hitIdx)
            {
                Debug.Log($"Activating preview {hitIdx + 1}...");
                hasTriggeredOverlap = true;
                triggeredIndex = hitIdx;
            }
        }
        else
        {
            for (int j = 0; j < preview_panels.Count; j++)
                if (preview_panels[j] != null)
                    preview_panels[j].gameObject.SetActive(false);
            hasTriggeredOverlap = false;
            triggeredIndex = -1;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (hasTriggeredOverlap && triggeredIndex >= 0 && preview_panels[triggeredIndex] != null)
        {
            RectTransform previewRect = preview_panels[triggeredIndex];
            rectTransform.position = previewRect.position;
            rectTransform.rotation = previewRect.rotation;
            rectTransform.localScale = previewRect.localScale;
            rectTransform.sizeDelta = previewRect.sizeDelta;
            rectTransform.anchorMin = previewRect.anchorMin;
            rectTransform.anchorMax = previewRect.anchorMax;
            rectTransform.pivot = previewRect.pivot;

            previewRect.gameObject.SetActive(false);

            Transform background = transform.GetChild(0);
            if (background != null)
            {
                Vector3 tmp = background.localScale;
                tmp.x = 2f;
                background.localScale = tmp;
                tmp.x = 1f;
                background.localScale = tmp;
            }
        }

        isDragging = false;
        hasTriggeredOverlap = false;
        triggeredIndex = -1;
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }
}
