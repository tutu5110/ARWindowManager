using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/*
 * Dynamic control the following grid layout composed by Horizontal Layout and Vertical Layout Groups
 */
public class DynamicGridController : MonoBehaviour
{
    private List<LayoutElement> m_rowElements;
    private List<List<LayoutElement>> m_colElements;

    private VerticalLayoutGroup m_verticalGroup;

    private RectTransform rectTransform;

    static public float MINIMUM_RATIO = 0.05f;


    void OnEnable()
    {
        m_rowElements = new List<LayoutElement>();
        m_colElements = new List<List<LayoutElement>>();

        m_verticalGroup = GetComponent<VerticalLayoutGroup>();
        rectTransform = rectTransform as RectTransform;

        RebuildRowColLists();
    }

    void OnDisable()
    {
        m_rowElements = null;
        m_colElements = null;
    }

    protected void OnTransformChildrenChanged()
    {
        RebuildRowColLists();
    }

    protected void RebuildRowColLists()
    {
        m_colElements.Clear();
        m_rowElements.Clear();

        // Find all rows in children. Just in one layer.
        List<HorizontalLayoutGroup> rows = new List<HorizontalLayoutGroup>();
        for (int childIdx = 0; childIdx < transform.childCount; ++childIdx)
        {
            HorizontalLayoutGroup group = transform.GetChild(childIdx).GetComponent<HorizontalLayoutGroup>();
            if (group != null)
                rows.Add(group);
        }

        if (rows.Count > 0)
        {
            // Seems Unity C# version is low and doesn't support this.
            // m_rowElements.EnsureCapacity(rows.Length);
            for (int i = 0; i < rows.Count; ++i)
            {
                LayoutElement le = rows[i].GetComponent<LayoutElement>();
                if (le == null)
                {
                    Debug.LogError("Find a row without layout information. Could lead to wrong layout.");
                    continue;
                }
                m_rowElements.Add(le);

                // Also only find one layer children.
                Transform leTrans = le.transform;
                m_colElements.Add(new List<LayoutElement>());
                for (int childIdx = 0; childIdx < leTrans.childCount; ++childIdx)
                {
                    LayoutElement colLe = leTrans.GetChild(childIdx).GetComponent<LayoutElement>();
                    if (colLe != null)
                        m_colElements[i].Add(colLe);
                }
            }
        }
    }

    public void ResizeColWidthTill(int rowIdx, int colIdx, float normalizedCoord)
    {
        if (rowIdx < 0)
        {
            // For all rows
            for (int ridx = 0; ridx < m_rowElements.Count; ++ridx)
            {
                ResizeColWidthTill(ridx, colIdx, normalizedCoord);
            }
            return;
        }

        if (rowIdx >= m_rowElements.Count || colIdx < 0)
            return;

        // It's a bit tricky, we need to recompute all weights for each cols which also involves spacing.
        // Get all weights
        List<LayoutElement> rowElements = m_colElements[rowIdx];
        if (colIdx >= rowElements.Count)
            return;

        // Current computation doesn't consider spacing. It might have some artifacts if spacing is very large.

        float prevWeights = 0.0f;
        float newWeights = 0.0f;
        float laterWeights = 0.0f;
        for (int cidx = 0; cidx < rowElements.Count; ++cidx)
        {
            if (cidx < colIdx)
            {
                prevWeights += rowElements[cidx].flexibleWidth;
            }
            else if (cidx == colIdx)
            {
                newWeights = normalizedCoord - prevWeights;
            }
            else
            {
                laterWeights += rowElements[cidx].flexibleWidth;
            }
        }

        // The first condition, easiest one, prev weights are not exceed.
        if (newWeights >= MINIMUM_RATIO)
        {
            rowElements[colIdx].flexibleWidth = newWeights;
            prevWeights += newWeights;
        }
        else if (prevWeights >= MINIMUM_RATIO)      // avoid a case when there's no previous elements and will cause NAN due to division by 0.
        {
            // We also need to resize all previous cols to be able to put the tile.
            float newPrevWeights = normalizedCoord - MINIMUM_RATIO;
            rowElements[colIdx].flexibleWidth = MINIMUM_RATIO;
            ScaleColWeightRange(rowIdx, 0, colIdx, newPrevWeights / prevWeights);
            prevWeights = normalizedCoord;

        }
        ScaleColWeightRange(rowIdx, colIdx + 1, rowElements.Count, (1.0f - prevWeights) / (laterWeights));
    }

    private void ScaleColWeightRange(int rowIdx, int colStart, int colEnd, float scale)
    {
        if (colStart >= colEnd) return;
        // It should only be called inside this class and all values are already checked.
        List<LayoutElement> rowElements = m_colElements[rowIdx];
        for (int cidx = colStart; cidx < colEnd; ++cidx)
        {
            rowElements[cidx].flexibleWidth *= scale;
            if (rowElements[cidx].flexibleWidth < MINIMUM_RATIO)
            {
                rowElements[cidx].flexibleWidth = MINIMUM_RATIO;
            }
        }
    }

    public void ResizeRowHeightTill(int rowIdx, int colIdx, float normalizedCoord)
    {
        if (rowIdx < 0 || rowIdx >= m_rowElements.Count)
            return;

        if (colIdx < 0)
        {
            int colCount = m_colElements[rowIdx].Count;
            // For all cols
            for (int cidx = 0; cidx < colCount; ++cidx)
            {
                ResizeRowHeightTill(rowIdx, cidx, normalizedCoord);
            }
            return;
        }

        // Current computation doesn't consider spacing. It might have some artifacts if spacing is very large.

        float prevWeights = 0.0f;
        float newWeights = 0.0f;
        float laterWeights = 0.0f;
        for (int ridx = 0; ridx < m_rowElements.Count; ++ridx)
        {
            if (ridx < rowIdx)
            {
                prevWeights += m_rowElements[ridx].flexibleHeight;
            }
            else if (ridx == rowIdx)
            {
                newWeights = normalizedCoord - prevWeights;
            }
            else
            {
                laterWeights += m_rowElements[ridx].flexibleHeight;
            }
        }

        // The first condition, easiest one, prev weights are not exceed.
        if (newWeights >= MINIMUM_RATIO)
        {
            m_rowElements[rowIdx].flexibleHeight = newWeights;
            prevWeights += newWeights;
        }
        else if (prevWeights >= MINIMUM_RATIO)      // avoid a case when there's no previous elements and will cause NAN due to division by 0.
        {
            // We also need to resize all previous cols to be able to put the tile.
            float newPrevWeights = normalizedCoord - MINIMUM_RATIO;
            m_rowElements[rowIdx].flexibleHeight = MINIMUM_RATIO;
            ScaleRowWeightRange(colIdx, 0, rowIdx, newPrevWeights / prevWeights);
            prevWeights = normalizedCoord;

        }
        ScaleRowWeightRange(colIdx, rowIdx + 1, m_rowElements.Count, (1.0f - prevWeights) / (laterWeights));
    }

    private void ScaleRowWeightRange(int colIdx, int rowStart, int rowEnd, float scale)
    {
        if (rowStart >= rowEnd) return;
        // It should only be called inside this class and all values are already checked.
        for (int ridx = rowStart; ridx < rowEnd; ++ridx)
        {
            m_rowElements[ridx].flexibleHeight *= scale;
            if (m_rowElements[ridx].flexibleHeight < MINIMUM_RATIO)
            {
                m_rowElements[ridx].flexibleHeight = MINIMUM_RATIO;
            }
        }
    }

    public float GetLocalHandlePositionOnAxis(int rowIdx, int colIdx, bool vertical)
    {
        if (vertical)
        {
            // Get vertical endposition.
            if (rowIdx < 0) return 0.0f;
            if (rowIdx >= m_rowElements.Count) return rectTransform.rect.height;

            RectTransform objTransform = m_rowElements[rowIdx].transform as RectTransform;

            return objTransform.rect.center.y + objTransform.anchoredPosition.y;
        }
        else
        {
            if (colIdx < 0) return 0.0f;
            if (colIdx >= m_colElements[rowIdx].Count) return rectTransform.rect.width;

            RectTransform objTransform = m_colElements[rowIdx][colIdx].transform as RectTransform;

            return objTransform.rect.center.x + objTransform.anchoredPosition.x;
        }
    }

    public Vector2 GetLocalHandlePosition(int rowIdx, int colIdx, bool vertical)
    {
        if (rowIdx < 0 || colIdx < 0) return Vector2.zero;
        if (rowIdx >= m_rowElements.Count || colIdx >= m_colElements[rowIdx].Count) return new Vector2(rectTransform.rect.width, rectTransform.rect.height);
        if (vertical)
        {
            RectTransform objTransform = m_colElements[rowIdx][colIdx].transform as RectTransform;

            return new Vector2(objTransform.rect.width + objTransform.anchoredPosition.x, objTransform.rect.center.y + objTransform.anchoredPosition.y);
        }
        else
        {
            RectTransform objTransform = m_colElements[rowIdx][colIdx].transform as RectTransform;
            return new Vector2(objTransform.rect.center.x + objTransform.anchoredPosition.x, objTransform.anchoredPosition.y);
        }
    }
}
