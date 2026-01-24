using System.Collections.Generic;
using UnityEngine;

public class ListProvider : MonoBehaviour, ITooltipProvider
{
    [Header("配置")]
    public string providerTitle; // 比如填 "当前状态" 或 "激活光环"
    
    // 运行时由 CharacterStateManager 实时更新此列表
    [HideInInspector] 
    public List<TooltipAllData> currentEntries = new ();

    // 实现接口：返回主窗口信息（这里通常只显示标题）
    public TooltipAllData GetTooltipData()
    {
        return new TooltipAllData { title = providerTitle, content = "" };
    }

    public List<TooltipAllData> GetSubEntries() => currentEntries;
    
    



    
}