using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    [Header("核心设置")]
    public int totalSlots = 20;
    public int columns = 5; // 必须与 Grid Layout Group 一致

    [Header("UI 引用")]
    public Transform gridParent;    // 放格子的父物体 (Grid)
    public Transform itemContainer; // 放物品的父物体 (Container)
    public GameObject slotPrefab;   // 格子预制体
    public GameObject itemPrefab;   // 物品预制体



    [Header("虚影设置")]
    public Image shadowImage; 
    // 拖拽一下你的 GridContainer 里随便一个 Slot 的 Image 赋值给它，或者由代码生成
    public Color validColor = new Color(0, 1, 0, 0.5f); // 绿色半透明 (能放)
    public Color invalidColor = new Color(1, 0, 0, 0.5f); // 红色半透明 (不能放)
    
    // 运行时数据
    private InventoryItem[] gridStates;
    // 注意：这里改成 public 方便 Draggable 访问，或者写个只读属性
    public List<RectTransform> slotRects = new (); 
    
    // 缓存参数
    private Vector2 cellSize;
    private Vector2 spacing;

    private void Awake()
    {
        Instance = this;
        gridStates = new InventoryItem[totalSlots];
    }

    private void Start()
    {
        InitSystem();
    }

    // --- 核心初始化系统 ---
    private void InitSystem()
    {

        // 1. 自动修正层级：确保 ItemContainer 在 GridContainer 下面 (渲染在最上)
        if (gridParent != null && itemContainer != null)
        {
            itemContainer.SetParent(gridParent.parent); // 确保它们是兄弟
            gridParent.SetAsFirstSibling(); // 格子在底层
            itemContainer.SetAsLastSibling(); // 物品在顶层
            
            // 自动去遮挡：关掉 ItemContainer 的射线接收，防止挡住格子
            var containerImage = itemContainer.GetComponent<Image>();
            if (containerImage != null) containerImage.raycastTarget = false;
        }

        // 2. 生成格子
        GenerateSlots();

        // 3. 强制刷新 UI (确保 LayoutGroup 算出正确位置)
        Canvas.ForceUpdateCanvases();
        if(gridParent != null) LayoutRebuilder.ForceRebuildLayoutImmediate(gridParent.GetComponent<RectTransform>());

        // 4. 缓存坐标和参数
        CacheSlotData();
    }

    void GenerateSlots()
    {
        // 清理旧格子
        foreach (Transform child in gridParent) Destroy(child.gameObject);
        slotRects.Clear();

        for (int i = 0; i < totalSlots; i++)
        {
            var slotObj = Instantiate(slotPrefab, gridParent);
            var slot = slotObj.GetComponent<InventorySlot>();
            if (slot == null) slot = slotObj.AddComponent<InventorySlot>();
            
            // ✨ 关键：初始化 Slot 的索引
            slot.slotIndex = i;
            
            // ✨ 强制修正 Slot 的 Pivot 为中心点，防止坐标计算偏移
            var rect = slotObj.GetComponent<RectTransform>();
            rect.pivot = new Vector2(0.5f, 0.5f);
            
            slotRects.Add(rect);
        }
    }

    void CacheSlotData()
    {
        // 如果上面 GenerateSlots 没跑，或者需要重新获取
        if (slotRects.Count == 0)
        {
            foreach (Transform child in gridParent)
            {
                slotRects.Add(child.GetComponent<RectTransform>());
            }
        }

        var gridLayout = gridParent.GetComponent<GridLayoutGroup>();
        if (gridLayout != null)
        {
            cellSize = gridLayout.cellSize;
            spacing = gridLayout.spacing;
        }
        else
        {
            // 如果没有 LayoutGroup，给个默认值防止报错
            cellSize = new Vector2(100, 100);
            spacing = Vector2.zero;
        }
    }

    // --- 核心功能 1: 物品放置逻辑 ---
    public void PlaceItem(InventoryItem item, int targetIndex)
    {
        // 1. 记录数据：更新物品的“身份证”地址
        item.anchorSlotIndex = targetIndex;

        // 2. 锁定格子：在逻辑网格中标记占用
        List<Vector2Int> shape = null;
        if (item != null) shape = item.shapeOffsets;
        var pointsToOccupy = GetEffectiveShape(item.width, item.height, shape);
        var startCoord = GetCoord(targetIndex);
        // 标记占用
        foreach (Vector2Int point in pointsToOccupy)
        {
            var idx = GetIndex(startCoord.x + point.x, startCoord.y + point.y);
            if (idx >= 0 && idx < totalSlots)
            {
                gridStates[idx] = item;
            }
        }

        // 3. 物理吸附：父子关系与坐标转换
        if (targetIndex < slotRects.Count)
        {
            item.transform.SetParent(itemContainer, false); 
            
            RectTransform slotRect = slotRects[targetIndex];
            RectTransform itemRect = item.GetComponent<RectTransform>();

            // 重置状态
            itemRect.localScale = Vector3.one;
            itemRect.localRotation = Quaternion.identity;

            // ✨【关键】坐标转换：将格子的世界坐标转为 Container 的局部坐标
            // 这解决了 Scale 不同、父物体不同导致的所有偏移问题
            Vector3 targetLocalPos = itemContainer.InverseTransformPoint(slotRect.position);
            itemRect.localPosition = targetLocalPos;

            // 计算多格物品的中心点偏移 (如果物品大于1格，需要往右下挪一点)
            if (item.width > 1 || item.height > 1)
            {
                float offsetX = (item.width - 1) * (cellSize.x + spacing.x) * 0.5f;
                float offsetY = -(item.height - 1) * (cellSize.y + spacing.y) * 0.5f;
                itemRect.localPosition += new Vector3(offsetX, offsetY, 0);
            }
            
            // 归零 Z 轴
            Vector3 pos = itemRect.localPosition; 
            pos.z = 0; 
            itemRect.localPosition = pos;
        }
    }

    // --- 核心功能 2: 拖拽结束处理 ---
    public void OnItemDropped(InventoryItem item, int targetIndex)
    {
        // A. 先清理：把自己从老位置的数据里“抠”出来
        // 这样在计算新位置(CanPlaceItem)时，才不会被“过去的自己”挡住
        ClearGrid(item.anchorSlotIndex, item.width, item.height, item);

        // B. 尝试放置：检查新位置是否合法
        // 失败：回老家 (PlaceItem 会重新把数据填回去)
        // Debug.Log($"位置 {targetIndex} 无效或被占用，回滚至 {item.anchorSlotIndex}");
        // 成功：去新家
        PlaceItem(item, CanPlaceItem(targetIndex, item.width, item.height) 
            ? targetIndex : item.anchorSlotIndex);
    }

    // --- 核心功能 3: 检查能否放置 ---
    public bool CanPlaceItem(int startIndex, int width, int height,List<Vector2Int> shape = null)
    {
        // 1. 负数索引直接拦截
        if (startIndex < 0 || startIndex >= totalSlots) return false;

        var startPos = GetCoord(startIndex);

        // 获取实际要检测的点列表
        var pointsToCheck = GetEffectiveShape(width, height, shape);
        
        foreach (Vector2Int point in pointsToCheck)
        {
            // 计算绝对坐标
            int targetX = startPos.x + point.x;
            int targetY = startPos.y + point.y;

            // 1. 越界检查 (是否超出了 Grid 的列数)
            if (targetX < 0 || targetX >= columns) return false;
        
            // 2. 越界检查 (是否超出了总行数/总格子)
            int idx = GetIndex(targetX, targetY);
            if (idx < 0 || idx >= totalSlots) return false;

            // 3. 占用检查 (是否撞到了别的物品)
            if (gridStates[idx] != null) return false;
        }
        return true;
    }

    // --- 辅助功能 ---
    // 在 InventoryManager 类中新增
    public List<Vector2Int> GetEffectiveShape(int w, int h, List<Vector2Int> customShape)
    {
        // 如果有自定义形状，直接返回
        if (customShape != null && customShape.Count > 0) return customShape;

        // 否则生成标准的矩形点阵
        List<Vector2Int> standardShape = new List<Vector2Int>();
        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                standardShape.Add(new Vector2Int(x, y));
            }
        }
        return standardShape;
    }
    // 清理网格占用
    public void ClearGrid(int startIndex, int w, int h, InventoryItem itemToClear)
    {
        if (startIndex < 0) return;
        // 获取形状
        List<Vector2Int> shape = null;
        if (itemToClear.runtimeCard.Data != null) shape = itemToClear.runtimeCard.Data.shapeOffsets;
    
        List<Vector2Int> pointsToClear = GetEffectiveShape(w, h, shape);
        Vector2Int startCoord = GetCoord(startIndex);

        foreach (Vector2Int point in pointsToClear)
        {
            int idx = GetIndex(startCoord.x + point.x, startCoord.y + point.y);
            if (idx >= 0 && idx < totalSlots && gridStates[idx] == itemToClear)
            {
                gridStates[idx] = null;
            }
        }
    }

    // Button添加物品
    public bool AddItem(RuntimeCard card)
    {

        var w = (card.Data != null) ? card.Data.width : 1;
        var h = (card.Data != null) ? card.Data.height : 1;

        // 寻找第一个能放下的位置
        for (var i = 0; i < totalSlots; i++)
        {
            if (!CanPlaceItem(i, w, h)) continue;
            var obj = Instantiate(itemPrefab, itemContainer);
            var img = obj.GetComponent<Image>();
            img.sprite = card.Data.artwork;
            img.alphaHitTestMinimumThreshold = 0.1f;
            var script = obj.GetComponent<InventoryItem>();
            script.Initialize(card);
                
            // 放置并更新数据
            PlaceItem(script, i);
            return true;
        }
        Debug.Log("背包已满，无法添加物品");
        return false;
    }
    //鼠标拖拽添加
    public void CreateItemAt(RuntimeCard card, int slotIndex)
    {
        var obj = Instantiate(itemPrefab, itemContainer);
        var img = obj.GetComponent<Image>();
        img.sprite = card.Data.artwork;
        img.alphaHitTestMinimumThreshold = 0.1f;
        var script = obj.GetComponent<InventoryItem>();
        script.Initialize(card);

        PlaceItem(script, slotIndex);
    }

    public void RemoveItem(InventoryItem item)
    {
        // 1. 清空它在网格里的占用
        ClearGrid(item.anchorSlotIndex, item.width, item.height, item);
    
        // 2. 如果你有列表维护所有物品，在这里 Remove
        // ...
    
        // 3. 如果有音效播放，可以在这里写
    }

    
    // 坐标转换工具
    public Vector2Int GetCoord(int index) => new Vector2Int(index % columns, index / columns);
    public int GetIndex(int x, int y) => y * columns + x;

// --- 虚影控制方法 ---
// 在 InventoryManager.cs 中
    private void CreateShadow()
{
    if (shadowImage == null)
    {
        GameObject shadowObj = new GameObject("DragShadow");
        shadowObj.transform.SetParent(itemContainer); // 必须和物品在同一层
        shadowObj.transform.SetAsFirstSibling(); // 放在最底层，不要挡住拖拽的物品
        shadowImage = shadowObj.AddComponent<Image>();
        shadowImage.raycastTarget = false; // ✨ 关键：虚影绝对不能阻挡射线！
        shadowImage.enabled = false;
        
        // 复制格子的外观 (可选)
        // if(slotPrefab != null) shadowImage.sprite = slotPrefab.GetComponent<Image>().sprite;
    }
}

    public void UpdateShadow(int targetIndex, int width, int height, 
        Sprite itemSprite = null, List<Vector2Int> shape = null)
{
    if (shadowImage == null) CreateShadow();

    // 1. 基础检查：索引是否存在
    if (targetIndex < 0 || targetIndex >= totalSlots)
    {
        shadowImage.enabled = false;
        return;
    }
    
    // 如果物品超出了右边界或下边界，直接隐藏虚影，而不是显示红色
    if (IsOutOfBounds(targetIndex, width, height))
    {
        shadowImage.enabled = false;
        return;
    }

    // 让虚影显示为物品本身的形状
    if (itemSprite != null)
    {
        shadowImage.sprite = itemSprite;
        shadowImage.type = Image.Type.Simple; // 不规则图片不要用 Sliced
        shadowImage.preserveAspect = true;
    }
    // 设置颜色
    // 因为前面已经检查过边界了，CanPlaceItem 现在的 false 只代表“被占用”
    bool canPlace = CanPlaceItem(targetIndex, width, height);
    shadowImage.color = canPlace ? validColor : invalidColor;
    
    // 2. 通过了边界检查，说明物品完全在格子范围内
    // 现在显示虚影，并根据是否重叠来决定颜色 (绿/红)
    shadowImage.enabled = true;

    // 设置大小
    float totalW = width * cellSize.x + (width - 1) * spacing.x;
    float totalH = height * cellSize.y + (height - 1) * spacing.y;
    shadowImage.rectTransform.sizeDelta = new Vector2(totalW, totalH);

    // 设置位置
    if (targetIndex < slotRects.Count)
    {
        RectTransform slotRect = slotRects[targetIndex];
        Vector3 targetLocalPos = itemContainer.InverseTransformPoint(slotRect.position);
        
        // 中心点偏移修正
        if (width > 1 || height > 1)
        {
            float offsetX = (width - 1) * (cellSize.x + spacing.x) * 0.5f;
            float offsetY = -(height - 1) * (cellSize.y + spacing.y) * 0.5f;
            targetLocalPos += new Vector3(offsetX, offsetY, 0);
        }
        shadowImage.rectTransform.localPosition = targetLocalPos;
        shadowImage.rectTransform.localScale = Vector3.one;
    }

   
}

// ✨ 辅助方法：检查是否出界
    private bool IsOutOfBounds(int targetIndex, int width, int height)
{
    Vector2Int pos = GetCoord(targetIndex);

    // A. 检查右边界 (比如在第5列放了个宽2的物品)
    if (pos.x + width > columns) return true;

    // B. 检查下边界 (检查物品右下角那个格子是否存在)
    // 计算右下角的坐标
    int endX = pos.x + width - 1;
    int endY = pos.y + height - 1;
    
    // 算出右下角的索引
    int cornerIndex = GetIndex(endX, endY);

    // 如果右下角索引超过了总格子数，说明下面出界了
    if (cornerIndex >= totalSlots) return true;

    return false;
}

    public void HideShadow()
    {
        if (shadowImage != null) shadowImage.enabled = false;
    }
    
    // 在 InventoryManager.cs 中


}