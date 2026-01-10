using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Draggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private InventoryItem item;
    private Transform originalParent;
    // ✨ 新增：记录鼠标点击点和物品中心点的距离
    private Vector2 touchOffset;
    // 用于记录是否已经成功处理了放置（防止重复逻辑）
    private bool isProcessed = false;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
        item = GetComponent<InventoryItem>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isProcessed = false;
        
        // 记录原始父物体（以防万一需要回滚，虽然现在主要靠 Manager）
        originalParent = transform.parent;
        
        // ✨✨✨ 1. 计算触点偏移 ✨✨✨
        // 算出鼠标当前位置，减去物品当前位置，得到偏移向量
        // 如果是 Screen Space Overlay，可以直接减
        // 如果是 Camera 模式，需要用 RectTransformUtility.ScreenPointToLocalPointInRectangle
        
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform.parent as RectTransform, // 基于父物体坐标系
            eventData.position, 
            eventData.pressEventCamera, 
            out var localMousePos
        );
        
        // 记录：鼠标点在哪里？(相对于物品原本的 anchors position)
        touchOffset = localMousePos - rectTransform.anchoredPosition;
        
        // 提到 UI 最上层
        transform.SetParent(transform.root); 
        canvasGroup.blocksRaycasts = false; // 允许射线穿透物品检测到底下的格子
        canvasGroup.alpha = 0.6f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // 移动时，不是把物品中心直接设为鼠标位置
        // 而是：目标位置 = 鼠标位置 - 刚才记录的偏移

        if (rectTransform == null) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out var localMousePos
        );
            
        rectTransform.anchoredPosition = localMousePos - touchOffset;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1.0f;

        // ✨✨✨ 核心：主动探测逻辑 ✨✨✨
        // 不再等待 InventorySlot 的 OnDrop，而是自己主动去找
        
        int bestTargetIndex = DetectBestGridPosition();

        if (bestTargetIndex != -1)
        {
            // 找到了有效位置，通知管理器尝试放置
            // Manager 内部会判断 CanPlaceItem，如果能放就放，不能放会自动回滚
            InventoryManager.Instance.OnItemDropped(item, bestTargetIndex);
        }
        else
        {
            // 没找到任何格子（扔到了空地），直接回滚
            Debug.Log("未检测到有效格子，回滚");
            InventoryManager.Instance.PlaceItem(item, item.anchorSlotIndex);
        }
    }

    // --- 多点探测算法 ---
    private int DetectBestGridPosition()
    {
        if (item == null) return -1;

        // 1. 获取物品尺寸和格子大小
        var width = rectTransform.rect.width;
        var height = rectTransform.rect.height;
        var cellW = width / item.width;
        var cellH = height / item.height;

        // 2. 遍历物品的每一个"子格"
        for (int x = 0; x < item.width; x++)
        {
            for (int y = 0; y < item.height; y++)
            {
                // A. 计算当前子格中心的世界坐标
                // 假设 Pivot 是中心点 (0.5, 0.5)
                float localX = -width * 0.5f + (x + 0.5f) * cellW;
                float localY = height * 0.5f - (y + 0.5f) * cellH; // UI坐标系Y轴向上，但Grid通常向下，这里注意方向
                
                Vector3 worldPos = rectTransform.TransformPoint(new Vector3(localX, localY, 0));

                // B. 发射射线寻找 InventorySlot
                InventorySlot hitSlot = RaycastForSlot(worldPos);

                if (hitSlot != null)
                {
                    // 如果物品的第 (x, y) 格命中了 Slot N
                    // 那么物品的左上角 (0, 0) 应该对齐到 Slot [N - x列 - y行]
                    int finalIndex = CalculateAnchorIndex(hitSlot.slotIndex, x, y);
                    
                    // 只要有一个点命中了合法位置，我们就认为意图明确，直接返回这个结果
                    if (finalIndex != -1) return finalIndex;
                }
            }
        }
        return -1;
    }

    // 射线检测辅助方法
    private InventorySlot RaycastForSlot(Vector3 worldPos)
    {
        PointerEventData pointerData = new PointerEventData(EventSystem.current);
        // 注意：如果是 Overlay 模式，Camera 传 null；如果是 Camera 模式，传 UICamera
        pointerData.position = RectTransformUtility.WorldToScreenPoint(null, worldPos); 

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (var result in results)
        {
            var slot = result.gameObject.GetComponent<InventorySlot>();
            if (slot != null) return slot;
        }
        return null;
    }

    // 计算反推的左上角索引
    private int CalculateAnchorIndex(int hitIndex, int colOffset, int rowOffset)
    {
        int columns = InventoryManager.Instance.columns;
        
        int hitCol = hitIndex % columns;
        int hitRow = hitIndex / columns;

        int targetCol = hitCol - colOffset;
        int targetRow = hitRow - rowOffset;

        // 越界检查（防止算出负数）
        if (targetCol < 0 || targetRow < 0) return -1;
        
        // 注意：这里只检查左上角是否越界，真正的"能不能放"由 Manager.CanPlaceItem 决定
        return targetRow * columns + targetCol;
    }
    
    // 给外部调用的成功回调（如果有其他脚本需要）
    public void OnDropSuccess()
    {
        isProcessed = true;
    }
}