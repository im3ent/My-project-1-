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
    private protected Vector2 touchOffset;


    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
        item = GetComponent<InventoryItem>();
    }

    public virtual void OnBeginDrag(PointerEventData eventData)
    {
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
        
        InventoryManager.Instance.ClearGrid(item.anchorSlotIndex, item.width, item.height, item);
    }

    public virtual void OnDrag(PointerEventData eventData)
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
        
        int bestIndex = GetCurrentGridIndex(); // 获取当前瞄准的格子
        InventoryManager.Instance.UpdateShadow(bestIndex, item.width, item.height);
    }

    public virtual void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1.0f;

        // ✨ 隐藏虚影
        InventoryManager.Instance.HideShadow();
        // ✨✨✨ 新增：检测是否扔进了售卖区 ✨✨✨
        SellZone sellZone = RaycastForSellZone(eventData.position);
        if (sellZone != null)
        {
            SellItem();
            return; // 卖掉了就直接结束，不用再找格子了
        }
        // 获取最终位置
        int bestTargetIndex = GetCurrentGridIndex();

        if (bestTargetIndex != -1)
        {
            InventoryManager.Instance.OnItemDropped(item, bestTargetIndex);
        }
        else
        {
            InventoryManager.Instance.PlaceItem(item, item.anchorSlotIndex);
        }
    }


    // 射线检测辅助方法
    private protected InventorySlot RaycastForSlot(Vector3 worldPos)
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
    private protected int CalculateAnchorIndex(int hitIndex, int colOffset, int rowOffset)
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
    
    // ✨ 将之前的 DetectBestGridPosition 改名为这个，只负责返回 int，不处理逻辑
    protected int GetCurrentGridIndex(RectTransform targetRect, int w, int h)
    {
        if (item == null) return -1;

        float width = targetRect.rect.width;
        float height = targetRect.rect.height;
        float cellW = width / item.width;
        float cellH = height / item.height;

        for (int x = 0; x < item.width; x++)
        {
            for (int y = 0; y < item.height; y++)
            {
                float localX = -width * 0.5f + (x + 0.5f) * cellW;
                float localY = height * 0.5f - (y + 0.5f) * cellH;
            
                Vector3 worldPos = rectTransform.TransformPoint(new Vector3(localX, localY, 0));
                InventorySlot hitSlot = RaycastForSlot(worldPos);

                if (hitSlot != null)
                {
                    int finalIndex = CalculateAnchorIndex(hitSlot.slotIndex, x, y);
                    // 只要找到了合法的左上角锚点，就立即返回
                    if (finalIndex != -1) return finalIndex;
                }
            }
        }
        return -1;
    }
    private protected int GetCurrentGridIndex()
    {
        return GetCurrentGridIndex(this.rectTransform, item.width, item.height);
    }
    
    // ✨ 处理售卖逻辑
    private void SellItem()
    {
        if (item != null && item.runtimeCard != null)
        {
            // 1. 加钱
            MoneyManager.Instance.AddGold(item.runtimeCard.Data.price);
            
            // 2. 从背包数据里彻底清除
            // InventoryManager 现在的 ClearGrid 只是清空引用，我们需要一个彻底移除的方法
            InventoryManager.Instance.RemoveItem(item); 
        
            // 3. 销毁物体
            Destroy(gameObject);

        }
    }

// ✨ 射线检测售卖区 (和找格子类似，但只找 SellZone)
    private SellZone RaycastForSellZone(Vector2 screenPos)
    {
        PointerEventData pointerData = new PointerEventData(EventSystem.current);
        pointerData.position = screenPos;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (var result in results)
        {
            var zone = result.gameObject.GetComponent<SellZone>();
            if (zone != null) return zone;
        }
        return null;
    }
}