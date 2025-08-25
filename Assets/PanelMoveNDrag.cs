using UnityEngine;
using UnityEngine.EventSystems;

public class PanelMoveNDrag : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Drag Target")]
    public RectTransform target;          // ��Ҫ���ƶ���Ŀ�ꣻ������Ĭ���ƶ����ű����ڵ� RectTransform

    [Header("Drag Config")]
    public float detectionYMax = 0f;      // ֻ�е�����λ�� y > detectionYMax �������϶�����Ϊ0�ɹر����ƣ�
    public Texture2D dragCursor;          // ��ѡ���϶�ʱ�������ʽ
    public Vector2 cursorHotspot = Vector2.zero;

    private RectTransform selfRect;       // �ű����ڶ���� RectTransform
    private RectTransform dragRect;       // ʵ�ʱ��϶��� RectTransform��= target ? target : selfRect��
    private bool isDragging;
    private Vector3 dragStartWorldPos;    // ����ʱָ���ڡ��϶�ƽ�桱�ϵ���������
    private Vector3 dragStartPanelPos;    // ����ʱĿ�������λ��
    private Camera pressCam;              // �����¼����������Overlay Canvas ��Ϊ null��

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

        // �������������е㣻��Ϊ (0,0,0) ����Ч������ ScreenPointToWorldPointInRectangle Ͷ����Ŀ�ꡱ��ƽ��
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

        // ����ǰָ��λ��ͶӰ����Ŀ�ꡱ��ƽ�棬����λ��
        if (!RectTransformUtility.ScreenPointToWorldPointInRectangle(
                dragRect, eventData.position, eventData.pressEventCamera, out var curWorld))
        {
            return;
        }

        Vector3 delta = curWorld - dragStartWorldPos;
        Vector3 newPos = dragStartPanelPos + delta;
        newPos.z = dragStartPanelPos.z; // ����ԭʼ z�����ڲ㼶��ǰ������
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
