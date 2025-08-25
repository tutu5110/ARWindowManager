using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ZoneDisplay : MonoBehaviour
{

    private List<Image> previewBackgroundsList = new List<Image>();
    // Start is called before the first frame update
    private bool is_active = true;

    void Start()
    {
        string[] previewPaths = {
                "Container/Row_0/preview_sub1",
                "Container/Row_0/preview_sub2",
                "Container/Row_1/preview_sub3",
                "Container/Row_1/preview_sub4"
            };

        for (int k = 0; k < previewPaths.Length; k++)
        {
            var t = transform.parent.Find(previewPaths[k]);
            if (t == null)
                continue;

            Image bgImg = null;

            var bgT = t.Find("Background");
            if (bgT != null)
            {
                bgImg = bgT.GetComponent<Image>();
                // 不要拦截事件
                if (bgImg != null) bgImg.raycastTarget = false;
            }


            previewBackgroundsList.Add(bgImg);

        }

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ToggleAllZones() {

        if (!is_active)
        {
            for (int i = 0; i < previewBackgroundsList.Count; i++)
            {
                SetImageAlpha(previewBackgroundsList[i], 0.2f);
            }
        }
        else {
            for (int i = 0; i < previewBackgroundsList.Count; i++)
            {
                SetImageAlpha(previewBackgroundsList[i], 0);
            }
        }
        is_active = !is_active;
    }
    private void SetImageAlpha(Image img, float a)
    {
        if (img == null) return;
        var c = img.color;
        c.a = a;
        img.color = c;
    }
}
