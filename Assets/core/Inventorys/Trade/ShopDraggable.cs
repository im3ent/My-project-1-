using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShopDraggable : Draggable
{
    public ShopItem shopItem;
    private CardDefinition data;
    private CanvasGroup cg;
    
    // 🎯 商店特有：记录原物品屏幕位置用于虚影显示
    private Vector2 originalItemScreenPos;
    
    protected override void Awake()
    {
        base.Awake();
        cg = GetComponent<CanvasGroup>();
        shopItem = GetComponent<ShopItem>();
    }
    
    /// <summary>
    /// 🎯 在点击瞬间记录位置（继承基类并添加商店特有逻辑）
    /// </summary>
    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData); // 调用基类记录 touchOffset
        
        if (shopItem.itemToSell == null) return;
        
        // 🎯 商店特有：记录 iconImage 的屏幕位置
        RectTransform iconRect = shopItem.iconImage.rectTransform;
        Vector3[] corners = new Vector3[4];
        iconRect.GetWorldCorners(corners);
        Vector3 visualCenter = (corners[0] + corners[2]) / 2f;
        
        originalItemScreenPos = RectTransformUtility.WorldToScreenPoint(
            eventData.pressEventCamera,
            visualCenter
        );
    }

    public override void OnBeginDrag(PointerEventData eventData)
    {
        data = shopItem.itemToSell;
        if (data == null) return;
        
        if (GlobalDragGhostManager.Instance == null)
        {
            Debug.LogError("[ShopDraggable] GlobalDragGhostManager.Instance is null!");
            return;
        }
        
        // 检查钱够不够
        if (MoneyManager.Instance.currentGold < data.price)
        {
            Debug.Log("钱不够，拖不动！");
            hasRecordedDownPosition = false;
            return;
        }
        
        // 使用 GlobalDragGhostManager 显示虚影
        const float s = 100f;
        Vector2 ghostSize = new Vector2(data.width * s, data.height * s);
        
        // 🎯 使用 OnPointerDown 中记录的位置（避免 drag threshold 偏差）
        Vector2 mousePos = hasRecordedDownPosition ? pointerDownPosition : eventData.position;
        
        GlobalDragGhostManager.Instance.ShowGhostWithOffset(
            shopItem.iconImage.sprite, 
            ghostSize, 
            mousePos,
            originalItemScreenPos
        );
        GlobalDragGhostManager.Instance.SetGhostAlpha(0.7f);
        
        hasRecordedDownPosition = false;
        cg.alpha = 0;
    }

    public override void OnDrag(PointerEventData eventData)
    {
        if (GlobalDragGhostManager.Instance == null) return;
        if (!GlobalDragGhostManager.Instance.dragGhostImage.gameObject.activeSelf) return;
        
        // 🎯 使用 GlobalDragGhostManager 更新位置（自动处理坐标转换）
        GlobalDragGhostManager.Instance.UpdateGhostPosition(eventData.position);
        
        // 🎯 使用 UICamera 进行 Raycast 检测背包格子
        var uiCamera = GlobalDragGhostManager.Instance.UICamera;
        
        // 更新背包虚影显示
        var bestIndex = GetCurrentGridIndex(
            GlobalDragGhostManager.Instance.dragGhostImage.rectTransform, 
            data.width, 
            data.height,
            uiCamera
        );
        InventoryManager.Instance.UpdateShadow(bestIndex, data.width, data.height, data.artwork, data.shapeOffsets);
    }

    public override void OnEndDrag(PointerEventData eventData)
    {
        if (GlobalDragGhostManager.Instance == null) return;
        if (!GlobalDragGhostManager.Instance.dragGhostImage.gameObject.activeSelf) return;
        
        // 🎯 使用 UICamera 进行 Raycast 检测背包格子
        var uiCamera = GlobalDragGhostManager.Instance.UICamera;
        
        var bestIndex = GetCurrentGridIndex(
            GlobalDragGhostManager.Instance.dragGhostImage.rectTransform, 
            data.width, 
            data.height,
            uiCamera
        );
        
        // 隐藏虚影
        GlobalDragGhostManager.Instance.HideGhost();
        InventoryManager.Instance.HideShadow();

        if (bestIndex != -1)
        {
            TryBuyAndPlace(bestIndex);
        }
        
        // 恢复原物品显示
        cg.alpha = 1;    
    }

    private void TryBuyAndPlace(int targetIndex)
    {
        var price = data.price;

        // 双重检查：钱够不够
        if (MoneyManager.Instance.currentGold < price) return;
            
        // 检查该位置能否放下
        if (InventoryManager.Instance.CanPlaceItem(targetIndex, data.width, data.height, data.shapeOffsets))
        {
            // 生成真物品
            InventoryManager.Instance.CreateItemAt(shopItem.runtimeToSell, targetIndex);
                
            // 扣钱
            MoneyManager.Instance.SpendGold(price);
            Destroy(gameObject);
            Debug.Log("拖拽进货成功！");
        }
        else
        {
            Debug.Log("这里放不下！");
        }
    }
}
