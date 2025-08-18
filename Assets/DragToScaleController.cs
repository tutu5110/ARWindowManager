using UnityEngine;
using UnityEngine.EventSystems;

public class DragToScaleController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Transform targetObject;

    public enum Axes { X, Y, XY }
    [Header("Axes & Factors")]
    public Axes axes = Axes.XY;
    public float factorX = 1f;
    public float factorY = 1f;
    public Vector2 clampX = new Vector2(0.1f, 5f);
    public Vector2 clampY = new Vector2(0.1f, 5f);

    [Header("Anchor/Pivot (world-locked)")]
    [Tooltip("����������ص���������겻����3D ����ԭ��ͨ���� (0,0,0)������ģ�����½ǣ����� (-width/2, -height/2, 0)��")]
    public Vector3 pivotLocal = Vector3.zero;
    public bool lockPivotWorld = true;

    // --- cached at drag-begin ---
    private bool dragging;
    private Camera cam;
    private Vector3 scale0;              // initial localScale
    private Matrix4x4 w2l0;              // world->local matrix at t0 (frozen)
    private Plane dragPlane0;            // frozen plane for ray projection
    private Vector3 pLocal0;             // pointer local (in start frame) at t0
    private Vector3 pivotWorld0;         // world pivot at t0

    public void OnBeginDrag(PointerEventData e)
    {
        if (!targetObject) return;
        dragging = true;

        cam = e.pressEventCamera ?? Camera.main;
        scale0 = targetObject.localScale;
        w2l0 = targetObject.worldToLocalMatrix;

        // ƽ�棺�� t0 �ĳ��� & pivot �����
        Vector3 planeNormal0 = targetObject.rotation * Vector3.forward;
        Vector3 pivotW0 = targetObject.TransformPoint(pivotLocal);
        dragPlane0 = new Plane(planeNormal0, pivotW0);

        // �ѵ�ǰָ��Ͷ���̶�ƽ�� �� ת������ʼ����ϵ��
        Vector3 pWorld0 = RayToPlane(cam, e.position, dragPlane0, pivotW0);
        pLocal0 = w2l0.MultiplyPoint3x4(pWorld0);

        if (lockPivotWorld) pivotWorld0 = pivotW0;
    }

    public void OnDrag(PointerEventData e)
    {
        if (!dragging || !targetObject) return;

        // ʼ��ͶӰ���������ƽ�桱�����á���ʼ����ϵ������
        Vector3 pWorld = RayToPlane(cam, e.position, dragPlane0, pivotWorld0);
        Vector3 pLocal = w2l0.MultiplyPoint3x4(pWorld);

        // --- �����޸Ŀ�ʼ ---

        // �������ű���������λ������
        // To avoid division by zero, handle cases where the initial pointer position is at the origin.
        float ratioX = (pLocal0.x != 0) ? pLocal.x / pLocal0.x : 1f;
        float ratioY = (pLocal0.y != 0) ? pLocal.y / pLocal0.y : 1f;

        float sx = scale0.x;
        float sy = scale0.y;

        if (axes == Axes.X || axes == Axes.XY)
            // ������Ӧ�õ���ʼ����ֵ��
            sx = Mathf.Clamp(scale0.x * ratioX, clampX.x, clampX.y);

        if (axes == Axes.Y || axes == Axes.XY)
            // ������Ӧ�õ���ʼ����ֵ��
            sy = Mathf.Clamp(scale0.y * ratioY, clampY.x, clampY.y);

        // �����XY��ͳһ���ţ���ȷ������һ��
        if (axes == Axes.XY)
        {
            // Use the axis that has been dragged further to determine the uniform scale
            float finalRatio = (Mathf.Abs(ratioX - 1f) > Mathf.Abs(ratioY - 1f)) ? ratioX : ratioY;
            sx = Mathf.Clamp(scale0.x * finalRatio, clampX.x, clampX.y);
            sy = Mathf.Clamp(scale0.y * finalRatio, clampY.x, clampY.y);
        }

        // --- �����޸Ľ��� ---

        targetObject.localScale = new Vector3(sx, sy, scale0.z);

        // �������� pivot �������꣺����������/����Ư��
        if (lockPivotWorld)
        {
            Vector3 pivotWorldNow = targetObject.TransformPoint(pivotLocal);
            targetObject.position += (pivotWorld0 - pivotWorldNow);
        }
    }

    public void OnEndDrag(PointerEventData e) { dragging = false; }

    private static Vector3 RayToPlane(Camera c, Vector2 screenPos, Plane plane, Vector3 fallback)
    {
        if (c != null)
        {
            Ray r = c.ScreenPointToRay(screenPos);
            if (plane.Raycast(r, out float dist)) return r.GetPoint(dist);
        }
        return fallback;
    }
}
