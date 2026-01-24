using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SubTooltipItem : MonoBehaviour
{
    [Header("UI 引用")]
    public TextMeshProUGUI headerText;
    public TextMeshProUGUI contentText;
    public RectTransform rectTransform;

    /// <summary>
    /// 初始化子悬浮窗内容
    /// </summary>
    public void Setup(string header, string content)
    {
        // 1. 赋值
        if (headerText != null)
        {
            headerText.text = header;
            headerText.gameObject.SetActive(!string.IsNullOrEmpty(header));
        }
        
        if (contentText != null)
        {
            contentText.text = content;
        }

        // 2. 强制刷新当前子项的布局
        // 这步很重要，因为 SubTooltip 通常是动态生成的，如果不刷新，
        // 父容器（VerticalLayoutGroup）可能拿不到正确的高度
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
    }
}