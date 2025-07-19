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

    public RectTransform hotRegionRef; // �ϵ�Inspector


    // ֻ������10%��ק
    [Range(0f, 1f)]
    public float draggableTopPercent = 0.1f;
    private RectTransform rectTransform;

    // ���ͼ��
    public Texture2D dragCursor;
    public Vector2 cursorHotspot = Vector2.zero; // ����Ϊ(0,0)��ͼ������

    void Start()
    {
        mainCamera = Camera.main;
        rectTransform = GetComponent<RectTransform>();

        // �õ�canvas�ϵ�raycaster
        raycaster = GetComponentInParent<GraphicRaycaster>();
        eventSystem = EventSystem.current;
    }


    public void OnPointerDown(PointerEventData eventData)
    {
        // ��ȡ�����UI��������
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, mainCamera, out localPoint);
        Debug.Log(eventData.position.y);

        if (eventData.position.y > detectionYMax)
        {
            isDragging = true;
            dragStartPanelPos = transform.position;
            dragStartMouseScreenPos = eventData.position;

            // �л����ͼ��
            if (dragCursor)
                Cursor.SetCursor(dragCursor, cursorHotspot, CursorMode.Auto);
        }
        else
        {
            isDragging = false;
            // �ָ�Ĭ�����
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
    }

    void QuarterizeCorners(Vector3[] corners, float scale, float xOffset, float yOffset)
    {
        // corners˳��0���£�1���ϣ�2���ϣ�3����
        Vector3 leftTop = corners[1];
        for (int i = 0; i < 4; i++)
        {
            corners[i] = leftTop + (corners[i] - leftTop) * scale;
            // X��Y����ƫ��
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
        // ��ȡA��B������ռ��Χ��min/max
        float axMin = Mathf.Min(cornersA[0].x, cornersA[1].x, cornersA[2].x, cornersA[3].x);
        float axMax = Mathf.Max(cornersA[0].x, cornersA[1].x, cornersA[2].x, cornersA[3].x);
        float ayMin = Mathf.Min(cornersA[0].y, cornersA[1].y, cornersA[2].y, cornersA[3].y);
        float ayMax = Mathf.Max(cornersA[0].y, cornersA[1].y, cornersA[2].y, cornersA[3].y);

        float bxMin = Mathf.Min(cornersB[0].x, cornersB[1].x, cornersB[2].x, cornersB[3].x);
        float bxMax = Mathf.Max(cornersB[0].x, cornersB[1].x, cornersB[2].x, cornersB[3].x);
        float byMin = Mathf.Min(cornersB[0].y, cornersB[1].y, cornersB[2].y, cornersB[3].y);
        float byMax = Mathf.Max(cornersB[0].y, cornersB[1].y, cornersB[2].y, cornersB[3].y);

        // AABB��ά��Χ���ཻ
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
            Debug.Log("����panel�����������·����ص���");
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;
        // �ָ�Ĭ�����
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }
}
