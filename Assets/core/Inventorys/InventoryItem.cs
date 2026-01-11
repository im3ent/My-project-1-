// 文件路径：Assets/core/Inventory/InventoryItem.cs

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public class InventoryItem : MonoBehaviour
{
    // ✨ 核心数据：这里面装着你的攻击力、法力消耗等
    public RuntimeCard runtimeCard;

    [Header("状态信息 (自动管理)")]
    public int width = 1;
    public int height = 1;
    public int anchorSlotIndex; // 物品左上角所在的格子索引
    public List<Vector2Int> shapeOffsets = new();
    /// <summary>
    /// 初始化物品：注入数据并调整 UI 尺寸
    /// </summary>
    public void Initialize(RuntimeCard inventory)
    {
        runtimeCard = inventory;

        // 1. 从配置读取尺寸 (如果没有配置，默认为 1x1)
        if (inventory.Data != null)
        {
            width = inventory.Data.width;
            height = inventory.Data.height;
            shapeOffsets =  inventory.Data.shapeOffsets;
        }

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
        SetSize(new Vector2(width * 100, height * 100)); 
    }

    private void SetSize(Vector2 size)
    {
        GetComponent<RectTransform>().sizeDelta = size;
    }
}