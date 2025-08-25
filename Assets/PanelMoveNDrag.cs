using UnityEngine;
using UnityEngine.EventSystems;

public class PanelMoveNDrag : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Drag Target")]
    public RectTransform target;          // 想要被移动的目标；不设则默认移动本脚本所在的 RectTransform

    [Header("Drag Config")]
    public float detectionYMax = 0f;      // 只有当按下位置 y > detectionYMax 才允许拖动（设为0可关闭限制）
    public Texture2D dragCursor;          // 可选：拖动时的鼠标样式
    public Vector2 cursorHotspot = Vector2.zero;

    private RectTransform selfRect;       // 脚本所在对象的 RectTransform
    private RectTransform dragRect;       // 实际被拖动的 RectTransform（= target ? target : selfRect）
    private bool isDragging;
    private Vector3 dragStartWorldPos;    // 按下时指针在“拖动平面”上的世界坐标
    private Vector3 dragStartPanelPos;    // 按下时目标的世界位置
    private Camera pressCam;              // 触发事件的摄像机（Overlay Canvas 下为 null）

    void Awake()
    {
        selfRect = GetComponent<RectTransform>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.position.y <= detectionYMax) return;

        dragRect = target != null ? target : selfRect;
        if (dragRect == null) return;

        isDragging = true;
        pressCam = eventData.pressEventCamera;
        dragStartPanelPos = dragRect.position;

        // 优先用射线命中点；若为 (0,0,0) 或无效，则用 ScreenPointToWorldPointInRectangle 投到“目标”的平面
        Vector3 worldPos = eventData.pointerPressRaycast.worldPosition;
        if (worldPos == Vector3.zero ||
            !RectTransformUtility.RectangleContainsScreenPoint(dragRect, eventData.position, pressCam))
        {
            if (!RectTransformUtility.ScreenPointToWorldPointInRectangle(
                    dragRect, eventData.position, pressCam, out worldPos))
            {
                isDragging = false;
                return;
            }
        }
        dragStartWorldPos = worldPos;

        if (dragCursor != null)
            Cursor.SetCursor(dragCursor, cursorHotspot, CursorMode.Auto);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || dragRect == null) return;

        // 将当前指针位置投影到“目标”的平面，计算位移
        if (!RectTransformUtility.ScreenPointToWorldPointInRectangle(
                dragRect, eventData.position, eventData.pressEventCamera, out var curWorld))
        {
            return;
        }

        Vector3 delta = curWorld - dragStartWorldPos;
        Vector3 newPos = dragStartPanelPos + delta;
        newPos.z = dragStartPanelPos.z; // 保持原始 z，不在层级中前后跳动
        dragRect.position = newPos;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isDragging) return;

        isDragging = false;
        dragRect = null;
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }
}
