using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    [Header("设置")]
    public int totalSlots = 20;
    public int columns = 5; // 必须与 Grid Layout Group 一致

    [Header("UI 引用")]
    public Transform gridParent;    // 放格子的
    public Transform itemContainer; // 放物品的
    public GameObject slotPrefab;
    public GameObject itemPrefab;

    private InventoryItem[] gridStates;
    private List<RectTransform> slotRects = new List<RectTransform>();
    public List<CardDefinition> d; 
    // 缓存网格参数
    private Vector2 cellSize;
    private Vector2 spacing;
    private bool isInitialized = false;

    private void Awake()
    {
        Instance = this;
        gridStates = new InventoryItem[totalSlots];
    }

    private void Start()
    {
        // 尝试初始化 (如果还没被 AddItem 触发过)
        if (!isInitialized)
        {
            InitSystem();
            foreach (var ds in d)
            {
                AddItem(new RuntimeCard(ds,null));
            }
        }
    }

    // --- 核心初始化系统 (防弹逻辑) ---
    private void InitSystem()
    {
        if (isInitialized) return;

        // 1. 自动修正层级：确保 ItemContainer 在 GridContainer 下面 (渲染在最上)
        itemContainer.SetParent(gridParent.parent); // 确保它们是兄弟
        gridParent.SetAsFirstSibling(); // 格子在底层
        itemContainer.SetAsLastSibling(); // 物品在顶层

        // 2. 自动去遮挡：关掉 ItemContainer 的射线接收
        var containerImage = itemContainer.GetComponent<Image>();
        if (containerImage != null) containerImage.raycastTarget = false;

        // 3. 生成格子
        GenerateSlots();

        // 4. ✨✨✨ 暴力强制刷新 UI ✨✨✨
        // 这一步至关重要！它迫使 Unity 在这一行代码执行完时，就把所有格子的坐标算出来
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(gridParent.GetComponent<RectTransform>());

        // 5. 缓存坐标
        CacheSlotData();

        isInitialized = true;
    }

    void GenerateSlots()
    {
        foreach (Transform child in gridParent) Destroy(child.gameObject);

        for (int i = 0; i < totalSlots; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, gridParent);
            var slot = slotObj.GetComponent<InventorySlot>();
            if (slot == null) slot = slotObj.AddComponent<InventorySlot>();
            slot.slotIndex = i;
            
            // ✨ 强制修正 Slot 的 Pivot 为中心点，防止偏移
            var rect = slotObj.GetComponent<RectTransform>();
            rect.pivot = new Vector2(0.5f, 0.5f);
        }
    }

    void CacheSlotData()
    {
        slotRects.Clear();
        foreach (Transform child in gridParent)
        {
            slotRects.Add(child.GetComponent<RectTransform>());
        }

        var gridLayout = gridParent.GetComponent<GridLayoutGroup>();
        if (gridLayout != null)
        {
            cellSize = gridLayout.cellSize;
            spacing = gridLayout.spacing;
        }
    }

    // --- 放置逻辑 (含偏移修正) ---
// 在 InventoryManager.cs 中替换 PlaceItem 方法

    public void PlaceItem(InventoryItem item, int targetIndex)
    {
        // 1. 数据逻辑 (保持不变)
        Vector2Int startPos = GetCoord(targetIndex);
        for (int x = 0; x < item.width; x++)
        {
            for (int y = 0; y < item.height; y++)
            {
                int idx = GetIndex(startPos.x + x, startPos.y + y);
                if (idx < totalSlots) gridStates[idx] = item;
            }
        }

        // 1. 改变父子关系
        item.transform.SetParent(itemContainer, false); // false 表示不保留原本的世界状态，完全重置

        // 2. ✨✨✨ 核心修复：坐标系精准转换 ✨✨✨
        if (targetIndex < slotRects.Count)
        {
            RectTransform slotRect = slotRects[targetIndex];
            RectTransform itemRect = item.GetComponent<RectTransform>();

            // A. 强制重置 缩放 和 旋转 (防止变小或歪掉)
            itemRect.localScale = Vector3.one;
            itemRect.localRotation = Quaternion.identity;

            // B. 【关键】问 itemContainer：“这个格子的世界坐标，在你目前的局部坐标系里是多少？”
            // 这能自动处理 itemContainer 和 gridParent 之间的位置、缩放差异。
            Vector3 localPos = itemContainer.InverseTransformPoint(slotRect.position);

            // C. 设置位置
            itemRect.localPosition = localPos;

            // D. 计算多格偏移 (注意：偏移量也是基于局部坐标的)
            float offsetX = (item.width - 1) * (cellSize.x + spacing.x) * 0.5f;
            float offsetY = -(item.height - 1) * (cellSize.y + spacing.y) * 0.5f;

            itemRect.localPosition += new Vector3(offsetX, offsetY, 0);
        
            // E. 再次强制 Z 轴归零 (双重保险)
            Vector3 finalPos = itemRect.localPosition;
            finalPos.z = 0;
            itemRect.localPosition = finalPos;
        }
    }

    // --- AddItem (懒加载入口) ---
    public bool AddItem(RuntimeCard card)
    {
        // ✨ 如果还没初始化（比如在 Start 里调用），先强制初始化
        if (!isInitialized) InitSystem();

        int w = (card.Data != null) ? card.Data.width : 1;
        int h = (card.Data != null) ? card.Data.height : 1;

        for (int i = 0; i < totalSlots; i++)
        {
            if (CanPlaceItem(i, w, h))
            {
                GameObject obj = Instantiate(itemPrefab, itemContainer);
                var script = obj.GetComponent<InventoryItem>();
                script.Initialize(card);
                PlaceItem(script, i);
                return true;
            }
        }
        Debug.Log("背包满");
        return false;
    }

    // ... 其他辅助函数 (GetCoord, CanPlaceItem, ClearGrid, OnItemDropped) 保持之前提供的逻辑不变 ...
    
    // (为了代码完整性，这里补全辅助函数，防止你复制漏了)
    private Vector2Int GetCoord(int index) => new Vector2Int(index % columns, index / columns);
    private int GetIndex(int x, int y) => y * columns + x;
    
    public bool CanPlaceItem(int startIndex, int width, int height) {
        Vector2Int startPos = GetCoord(startIndex);
        for(int x=0; x<width; x++) {
            for(int y=0; y<height; y++) {
                int cx = startPos.x + x;
                int cy = startPos.y + y;
                if(cx >= columns) return false;
                int idx = GetIndex(cx, cy);
                if(idx >= totalSlots || gridStates[idx] != null) return false;
            }
        }
        return true;
    }

    private void ClearGrid(int startIndex, int w, int h, InventoryItem itemToClear) {
        Vector2Int startPos = GetCoord(startIndex);
        for(int x=0; x<w; x++) {
            for(int y=0; y<h; y++) {
                int idx = GetIndex(startPos.x + x, startPos.y + y);
                if(idx < totalSlots && gridStates[idx] == itemToClear) gridStates[idx] = null;
            }
        }
    }

    public void OnItemDropped(InventoryItem item, int targetIndex) {
        ClearGrid(item.anchorSlotIndex, item.width, item.height, item);
        if(CanPlaceItem(targetIndex, item.width, item.height)) {
            PlaceItem(item, targetIndex);
            var drag = item.GetComponent<Draggable>();
            if(drag) drag.OnDropSuccess();
        } else {
            PlaceItem(item, item.anchorSlotIndex);
        }
    }
}