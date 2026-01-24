// 文件路径：Assets/core/Inventory/InventoryItem.cs

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public class InventoryItem : MonoBehaviour,ITooltipProvider
{
    // ✨ 核心数据：这里面装着你的攻击力、法力消耗等
    [System.NonSerialized] public RuntimeItem runtimeItem;
    public CardDefinition CardDef => runtimeItem?.data;
    
    [Header("组件引用")]
    public RectTransform rectTransform;
    public CanvasGroup canvasGroup;
    [Header("状态信息 (自动管理)")]
    public int Width => (CardDef != null) ? CardDef.width : 1;
    public int Height => (CardDef != null) ? CardDef.height : 1;
    public List<Vector2Int> ShapeOffsets 
        => (CardDef != null) ? CardDef.shapeOffsets : new List<Vector2Int>();
    
    public int anchorSlotIndex; // 物品左上角所在的格子索引
    // 内部变量
    private Transform originalParent;
    private Vector3 _worldTouchOffset; // ✨ 记录世界坐标偏移

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
    }
    
    
    /// <summary>
    /// 初始化物品：注入数据并调整 UI 尺寸
    /// </summary>
    public void Initialize(RuntimeItem inventory)
    {
        runtimeItem = inventory;

        // 2. 刷新卡面显示 (攻击力、图片等)
        var display = GetComponent<CardDisplay>();
        if (display != null)
        {
            // 假设你的 CardDisplay 有这个方法，如果没有请自行添加
            // display.Init(card); 
            // 或者：
            // display.runtimeCard = card;
            // display.UpdateText();
        }

        // 3. (可选) 根据格数调整 UI 大小
        // 假设每个格子是 100x100，这里可以让物品真的变成 200x300
        if(rectTransform == null) rectTransform = GetComponent<RectTransform>();
        rectTransform.sizeDelta=(new Vector2(Width * 100, Height * 100)); 
    }
    
    public TooltipAllData GetTooltipData()=> new() {
        title = runtimeItem.data.cardName,
        content = runtimeItem.data.description,
        headerColor = Color.yellow
    };

    public List<TooltipAllData> GetSubEntries()
    {
        var subs = new List<TooltipAllData>();

        // 假设你的 runtimeItem.Data 里有一个关键字列表
        // 比如：itemData.keywords = ["战吼", "剧毒"]
        if (runtimeItem.data.keywords == null) return subs;
        foreach (var kw in runtimeItem.data.keywords)
        {
            subs.Add(new TooltipAllData {
                title = $"<color=#{ColorUtility.ToHtmlStringRGB(kw.themeColor)}>{kw.keywordName}</color>",
                content = kw.description
            });
        }

        return subs;
    }
}
