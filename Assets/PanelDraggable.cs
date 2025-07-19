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

    private GraphicRaycaster raycaster;
    private EventSystem eventSystem;

    public float detectionXMin = -100;
    public float detectionXMax = 100;
    public float detectionYMin = -50;
    public float detectionYMax = 0;
    public bool visualizeQuarter = false;

    public RectTransform hotRegionRef; // 拖到Inspector


    // 只允许顶部10%拖拽
    [Range(0f, 1f)]
    public float draggableTopPercent = 0.1f;
    private RectTransform rectTransform;

    // 鼠标图标
    public Texture2D dragCursor;
    public Vector2 cursorHotspot = Vector2.zero; // 可设为(0,0)或图标中心

    void Start()
    {
        mainCamera = Camera.main;
        rectTransform = GetComponent<RectTransform>();

        // 拿到canvas上的raycaster
        raycaster = GetComponentInParent<GraphicRaycaster>();
        eventSystem = EventSystem.current;
    }


    public void OnPointerDown(PointerEventData eventData)
    {
        // 获取鼠标在UI本地坐标
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, mainCamera, out localPoint);
        Debug.Log(eventData.position.y);

        if (eventData.position.y > detectionYMax)
        {
            isDragging = true;
            dragStartPanelPos = transform.position;
            dragStartMouseScreenPos = eventData.position;

            // 切换鼠标图标
            if (dragCursor)
                Cursor.SetCursor(dragCursor, cursorHotspot, CursorMode.Auto);
        }
        else
        {
            isDragging = false;
            // 恢复默认鼠标
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
    }

    void QuarterizeCorners(Vector3[] corners, float scale, float xOffset, float yOffset)
    {
        // corners顺序：0左下，1左上，2右上，3右下
        Vector3 leftTop = corners[1];
        for (int i = 0; i < 4; i++)
        {
            corners[i] = leftTop + (corners[i] - leftTop) * scale;
            // X和Y方向偏移
            corners[i] += new Vector3(xOffset, yOffset, 0);
        }
    }

    bool RectTransformWorldOverlap(RectTransform a, RectTransform b)
    {
        Vector3[] cornersA = new Vector3[4];
        Vector3[] cornersB = new Vector3[4];
        a.GetWorldCorners(cornersA);
        b.GetWorldCorners(cornersB);

        QuarterizeCorners(cornersA, 0.25f, 0,0);
        // 获取A和B的世界空间包围盒min/max
        float axMin = Mathf.Min(cornersA[0].x, cornersA[1].x, cornersA[2].x, cornersA[3].x);
        float axMax = Mathf.Max(cornersA[0].x, cornersA[1].x, cornersA[2].x, cornersA[3].x);
        float ayMin = Mathf.Min(cornersA[0].y, cornersA[1].y, cornersA[2].y, cornersA[3].y);
        float ayMax = Mathf.Max(cornersA[0].y, cornersA[1].y, cornersA[2].y, cornersA[3].y);

        float bxMin = Mathf.Min(cornersB[0].x, cornersB[1].x, cornersB[2].x, cornersB[3].x);
        float bxMax = Mathf.Max(cornersB[0].x, cornersB[1].x, cornersB[2].x, cornersB[3].x);
        float byMin = Mathf.Min(cornersB[0].y, cornersB[1].y, cornersB[2].y, cornersB[3].y);
        float byMax = Mathf.Max(cornersB[0].y, cornersB[1].y, cornersB[2].y, cornersB[3].y);

        // AABB二维包围盒相交
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
        Vector3 worldDelta = (currWorld - startWorld)*2;
        transform.position = dragStartPanelPos + worldDelta;

        if (RectTransformWorldOverlap(this.rectTransform, hotRegionRef))
        {
            Debug.Log("两个panel在世界坐标下发生重叠！");
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;
        // 恢复默认鼠标
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }
}
