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

        Vector3 planeNormal0 = targetObject.rotation * Vector3.forward;
        Vector3 pivotW0 = targetObject.TransformPoint(pivotLocal);
        dragPlane0 = new Plane(planeNormal0, pivotW0);

        Vector3 pWorld0 = RayToPlane(cam, e.position, dragPlane0, pivotW0);
        pLocal0 = w2l0.MultiplyPoint3x4(pWorld0);

        if (lockPivotWorld) pivotWorld0 = pivotW0;
    }

    public void OnDrag(PointerEventData e)
    {
        if (!dragging || !targetObject) return;

        Vector3 pWorld = RayToPlane(cam, e.position, dragPlane0, pivotWorld0);
        Vector3 pLocal = w2l0.MultiplyPoint3x4(pWorld);
        Vector3 dLocal = pLocal - pLocal0;

        float sx = scale0.x, sy = scale0.y;

        // X 方向保持不变
        if (axes == Axes.X || axes == Axes.XY)
            sx = Mathf.Clamp(scale0.x + dLocal.x * factorX, clampX.x, clampX.y);

        // Y 方向：当且仅当只约束 Y 时反向
        if (axes == Axes.Y)
            sy = Mathf.Clamp(scale0.y - dLocal.y * factorY, clampY.x, clampY.y);
        else if (axes == Axes.XY)
            sy = Mathf.Clamp(scale0.y + dLocal.y * factorY, clampY.x, clampY.y);

        targetObject.localScale = new Vector3(sx, sy, scale0.z);

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
