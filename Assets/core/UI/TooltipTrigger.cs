using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// 记得挂在有 Raycast Target (Image/Panel) 的物体上
public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{

    private ITooltipProvider _provider;

    private bool _isHovering = false;
    private void Awake()
    {
        // 自动寻找挂载在同一个物体上实现了接口的任何脚本
        _provider = GetComponent<ITooltipProvider>();
    }
    /*private void Update()
    {
        if (_isHovering)
        {
            // 每一帧或每隔 0.1s 刷新一次文本
            UpdateTooltip();
        }
    }*/
    // 鼠标移入
    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovering = true;
        // 优先显示动态数据
        if (_provider != null && TooltipSystem.Instance != null)
        {
            // 每次进入时都会重新获取最新字符串，保证数据动态更新
            UpdateTooltip();
        }
    }

    // 鼠标移出
    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovering = false;
        if (TooltipSystem.Instance != null)
        {
            TooltipSystem.Instance.Hide();
        }
    }
    
    private void UpdateTooltip()
    {
        if (TooltipSystem.Instance != null && _provider != null)
        {
            TooltipSystem.Instance.Show(_provider);
        }
    }
}

public interface ITooltipProvider
{
    TooltipAllData GetTooltipData();
    List<TooltipAllData> GetSubEntries();
}

public struct TooltipAllData 
{
    public string title;
    public string content;
    public Color headerColor;
    // 如果以后需要图标，可以在这加 Sprite icon;
}