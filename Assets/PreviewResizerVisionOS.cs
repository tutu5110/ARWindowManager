using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// Attach to knob objects (Image + Raycast Target).
public class PreviewResizerVisionOS_V4 :
    MonoBehaviour, IPointerDownHandler, IDragHandler, IEndDragHandler
{
    [Header("Main Resize Target")]
    public List<RectTransform> targets = new List<RectTransform>();
    public Mode mode = Mode.Width;


    [Header("Width options")]
    public bool knobOnRightEdge = true;

    [Header("Height anchor & direction")]
    public HeightAnchor heightAnchor = HeightAnchor.Top;
    public bool heightDragUpIncreases = true;

    [Header("Uniform")]
    public bool uniformAnchorTopLeft = true;

    [Header("Clamp")]
    public float minWidth = 100f, maxWidth = 4096f;
    public float minHeight = 100f, maxHeight = 4096f;

    public enum Mode { Width, Height, Uniform }
    public enum HeightAnchor { Top, Bottom }

    private List<Vector2> startSizes = new List<Vector2>();
    private Vector3 startWorld;
    private List<Vector3> anchorWorldStarts = new List<Vector3>();
    private Camera evtCam;

    private List<Vector2> subpanelStartSizes = new List<Vector2>();
    private List<Vector2> subpanelStartPositions = new List<Vector2>();

    public void OnPointerDown(PointerEventData eventData)
    {
        if (targets == null || targets.Count == 0) return;
        evtCam = eventData.pressEventCamera ?? Camera.main;
        startSizes.Clear();
        anchorWorldStarts.Clear();
        subpanelStartSizes.Clear();
        subpanelStartPositions.Clear();
        startWorld = GetWorld(eventData, targets[0]);

        foreach (var currentTarget in targets)
        {
            if (currentTarget == null) continue;
            Vector2 currentStartSize = currentTarget.sizeDelta;
            startSizes.Add(currentStartSize);
            anchorWorldStarts.Add(currentTarget.TransformPoint(GetAnchorLocal(currentTarget, currentStartSize)));
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (targets == null || targets.Count == 0 || startSizes.Count == 0) return;

        RectTransform referenceTarget = targets[0];
        Vector3 curWorld = GetWorld(eventData, referenceTarget);
        Vector3 startLocal = referenceTarget.InverseTransformPoint(startWorld);
        Vector3 curLocal = referenceTarget.InverseTransformPoint(curWorld);
        Vector2 delta = (Vector2)(curLocal - startLocal);

        for (int i = 0; i < targets.Count; i++)
        {
            RectTransform currentTarget = targets[i];
            if (currentTarget == null || i >= startSizes.Count) continue;

            Vector2 currentStartSize = startSizes[i];
            Vector2 newSize = currentStartSize;

            switch (mode)
            {
                case Mode.Width:
                    newSize.x = Mathf.Clamp(currentStartSize.x + delta.x, minWidth, maxWidth);
                    newSize.y = Mathf.Clamp(currentStartSize.y, minHeight, maxHeight);
                    break;
                case Mode.Height:
                    float dy = delta.y;
                    float sign = heightDragUpIncreases ? 1f : -1f;
                    newSize.y = Mathf.Clamp(currentStartSize.y + sign * dy, minHeight, maxHeight);
                    newSize.x = Mathf.Clamp(currentStartSize.x, minWidth, maxWidth);
                    break;
                case Mode.Uniform:
                    float sx = (currentStartSize.x > 0f) ? 1f + delta.x / currentStartSize.x : 1f;
                    float sy = (currentStartSize.y > 0f) ? 1f + delta.y / currentStartSize.y : 1f;
                    float s = Mathf.Max(sx, sy);
                    newSize.x = Mathf.Clamp(currentStartSize.x * s, minWidth, maxWidth);
                    newSize.y = Mathf.Clamp(currentStartSize.y * s, minHeight, maxHeight);
                    break;
            }

            currentTarget.sizeDelta = newSize;

            Vector3 anchorWorldNow = currentTarget.TransformPoint(GetAnchorLocal(currentTarget, newSize));
            currentTarget.position += (anchorWorldStarts[i] - anchorWorldNow);
        }
    }

    public void OnEndDrag(PointerEventData eventData) { }

    private Vector3 GetAnchorLocal(RectTransform currentTarget, Vector2 size)
    {
        float w = size.x, h = size.y;
        Vector2 pv = currentTarget.pivot;
        Vector3 leftCenter = new Vector3(-pv.x * w, (0.5f - pv.y) * h, 0f);
        Vector3 rightCenter = new Vector3((1f - pv.x) * w, (0.5f - pv.y) * h, 0f);
        Vector3 topCenter = new Vector3((0.5f - pv.x) * w, (1f - pv.y) * h, 0f);
        Vector3 bottomCenter = new Vector3((0.5f - pv.x) * w, -pv.y * h, 0f);
        Vector3 leftTop = new Vector3(-pv.x * w, (1f - pv.y) * h, 0f);

        switch (mode)
        {
            case Mode.Width: return knobOnRightEdge ? leftCenter : rightCenter;
            case Mode.Height: return (heightAnchor == HeightAnchor.Top) ? topCenter : bottomCenter;
            case Mode.Uniform: return uniformAnchorTopLeft ? leftTop : leftTop;
        }
        return Vector3.zero;
    }

    private static Vector3 GetWorld(PointerEventData e, RectTransform refRect)
    {
        Vector3 w = e.pointerCurrentRaycast.worldPosition;
        if (w == Vector3.zero)
        {
            if (!RectTransformUtility.ScreenPointToWorldPointInRectangle(
                    refRect, e.position, e.pressEventCamera ?? Camera.main, out w))
            {
                w = refRect.position;
            }
        }
        return w;
    }
}