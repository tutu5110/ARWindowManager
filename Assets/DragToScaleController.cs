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
    [Tooltip("锁定这个本地点的世界坐标不动；3D 物体原点通常是 (0,0,0)。若想模拟左下角，可填 (-width/2, -height/2, 0)。")]
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

        // 平面：用 t0 的朝向 & pivot 世界点
        Vector3 planeNormal0 = targetObject.rotation * Vector3.forward;
        Vector3 pivotW0 = targetObject.TransformPoint(pivotLocal);
        dragPlane0 = new Plane(planeNormal0, pivotW0);

        // 把当前指针投到固定平面 → 转到“起始坐标系”
        Vector3 pWorld0 = RayToPlane(cam, e.position, dragPlane0, pivotW0);
        pLocal0 = w2l0.MultiplyPoint3x4(pWorld0);

        if (lockPivotWorld) pivotWorld0 = pivotW0;
    }

    public void OnDrag(PointerEventData e)
    {
        if (!dragging || !targetObject) return;

        // 始终投影到“冻结的平面”，并用“起始坐标系”计算
        Vector3 pWorld = RayToPlane(cam, e.position, dragPlane0, pivotWorld0);
        Vector3 pLocal = w2l0.MultiplyPoint3x4(pWorld);

        // --- 核心修改开始 ---

        // 计算缩放比例而不是位移增量
        // To avoid division by zero, handle cases where the initial pointer position is at the origin.
        float ratioX = (pLocal0.x != 0) ? pLocal.x / pLocal0.x : 1f;
        float ratioY = (pLocal0.y != 0) ? pLocal.y / pLocal0.y : 1f;

        float sx = scale0.x;
        float sy = scale0.y;

        if (axes == Axes.X || axes == Axes.XY)
            // 将比例应用到初始缩放值上
            sx = Mathf.Clamp(scale0.x * ratioX, clampX.x, clampX.y);

        if (axes == Axes.Y || axes == Axes.XY)
            // 将比例应用到初始缩放值上
            sy = Mathf.Clamp(scale0.y * ratioY, clampY.x, clampY.y);

        // 如果是XY（统一缩放），确保比例一致
        if (axes == Axes.XY)
        {
            // Use the axis that has been dragged further to determine the uniform scale
            float finalRatio = (Mathf.Abs(ratioX - 1f) > Mathf.Abs(ratioY - 1f)) ? ratioX : ratioY;
            sx = Mathf.Clamp(scale0.x * finalRatio, clampX.x, clampX.y);
            sy = Mathf.Clamp(scale0.y * finalRatio, clampY.x, clampY.y);
        }

        // --- 核心修改结束 ---

        targetObject.localScale = new Vector3(sx, sy, scale0.z);

        // 绝对锁定 pivot 世界坐标：不允许左右/上下漂移
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
