using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways, RequireComponent(typeof(Canvas))]
public class DisplayOnlyCanvas : MonoBehaviour
{
    [Tooltip("���ú󱾻�����������ʾ���������κ� UI �¼�")]
    public bool displayOnly = true;

    void OnEnable() => Apply();
    void OnValidate() => Apply();

    void Apply()
    {
        var canvas = GetComponent<Canvas>();

        // 1) �رձ������ϵ� Raycaster��������ϵͳ��
        var gr = GetComponent<GraphicRaycaster>();
        if (gr) gr.enabled = !displayOnly;

        // 2) ������õ���������ϵͳ/XR��ͬ���ر� TrackedDeviceGraphicRaycaster


        // 3) �� CanvasGroup ������������
        var cg = GetComponent<CanvasGroup>();
        if (!cg) cg = gameObject.AddComponent<CanvasGroup>();
        cg.interactable = !displayOnly;
        cg.blocksRaycasts = !displayOnly;

        // 4) ����ѡ���������� Graphic �� raycastTarget �ص�
        foreach (var g in GetComponentsInChildren<Graphic>(true))
            g.raycastTarget = !displayOnly ? g.raycastTarget : false;
    }
}
