using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways, RequireComponent(typeof(Canvas))]
public class DisplayOnlyCanvas : MonoBehaviour
{
    [Tooltip("启用后本画布仅用于显示，不接收任何 UI 事件")]
    public bool displayOnly = true;

    void OnEnable() => Apply();
    void OnValidate() => Apply();

    void Apply()
    {
        var canvas = GetComponent<Canvas>();

        // 1) 关闭本画布上的 Raycaster（旧输入系统）
        var gr = GetComponent<GraphicRaycaster>();
        if (gr) gr.enabled = !displayOnly;

        // 2) 如果你用的是新输入系统/XR：同样关闭 TrackedDeviceGraphicRaycaster


        // 3) 用 CanvasGroup 彻底屏蔽命中
        var cg = GetComponent<CanvasGroup>();
        if (!cg) cg = gameObject.AddComponent<CanvasGroup>();
        cg.interactable = !displayOnly;
        cg.blocksRaycasts = !displayOnly;

        // 4) （可选）把所有子 Graphic 的 raycastTarget 关掉
        foreach (var g in GetComponentsInChildren<Graphic>(true))
            g.raycastTarget = !displayOnly ? g.raycastTarget : false;
    }
}
