using UnityEngine;

public class DrawWorldCorners : MonoBehaviour
{
    public Color gizmoColor = Color.green;
    public float gizmoRadius = 0.005f;
    public bool visualizeQuarter = false;
    public float xOffset = 0f;
    public float yOffset = 0f;
    void OnDrawGizmos()
    {
        RectTransform rt = GetComponent<RectTransform>();
        if (rt == null) return;

        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);

        // ��ѡ����С��1/4
        if (visualizeQuarter)
        {
            QuarterizeCorners(corners, 0.25f, xOffset, yOffset);
        }

        Gizmos.color = gizmoColor;
        for (int i = 0; i < 4; i++)
        {
            Gizmos.DrawSphere(corners[i], gizmoRadius);
            Gizmos.DrawLine(corners[i], corners[(i + 1) % 4]);
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
}

