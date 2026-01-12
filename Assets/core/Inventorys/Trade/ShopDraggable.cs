using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShopDraggable :  Draggable
{
    public ShopItem shopItem;
    private Image ghostImage; // 拖拽时的临时图标
    private RectTransform ghostRect;
    private CardDefinition data;
    private CanvasGroup cg;
    private void Awake()
    {
        cg = GetComponent<CanvasGroup>();
        shopItem = GetComponent<ShopItem>();
        ghostImage = ShopManager.Instance.globalDragGhost;
        ghostRect = ghostImage.GetComponent<RectTransform>();
    }

    public override void OnBeginDrag(PointerEventData eventData)
    {
        data = shopItem.itemToSell;
        if (data == null && ghostRect == null) return;
        
        // 1. 检查钱够不够（不够连拖都不让拖）
        if (MoneyManager.Instance.currentGold < data.price)
        {
            Debug.Log("钱不够，拖不动！");
            return;
        }
        // 假设格子大小是 100，你可以从 InventoryManager 获取
        const float s = 100f;
        // 2. 生成一个临时的图标跟随鼠标
        ghostRect.sizeDelta = new Vector2(data.width * s, data.height * s);
        ghostRect.position = shopItem.iconImage.transform.position;
        ghostImage.sprite = shopItem.iconImage.sprite;
        ghostImage.gameObject.SetActive(true);
  
        cg.alpha = 0;             // 透明度设为 0 (看不见)

        // ✨✨✨ 1. 计算触点偏移 ✨✨✨
        // 算出鼠标当前位置，减去物品当前位置，得到偏移向量
        // 如果是 Screen Space Overlay，可以直接减
        // 如果是 Camera 模式，需要用 RectTransformUtility.ScreenPointToLocalPointInRectangle
        
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            ghostRect.parent as RectTransform, // 基于父物体坐标系
            eventData.position, 
            eventData.pressEventCamera, 
            out var localMousePos
        );
        // 记录：鼠标点在哪里？(相对于物品原本的 anchors position)
        touchOffset = localMousePos - ghostRect.anchoredPosition;

    }

    public override void OnDrag(PointerEventData eventData)
    {
        //if (ghostRect != null) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            ghostRect.parent as RectTransform, // 基于父物体坐标系
            eventData.position, 
            eventData.pressEventCamera, 
            out var localMousePos
        );
        ghostRect.anchoredPosition = localMousePos- touchOffset;
        
        var bestIndex = GetCurrentGridIndex(ghostRect, data.width, data.height);
        InventoryManager.Instance.UpdateShadow(bestIndex, data.width, data.height, data.artwork, data.shapeOffsets);
    }

    public override void OnEndDrag(PointerEventData eventData)
    {
        if (!ghostImage.gameObject.activeSelf) return;
        
        var bestIndex = GetCurrentGridIndex(ghostRect, data.width, data.height);
        
        ghostImage.gameObject.SetActive(false);
        InventoryManager.Instance.HideShadow();

        if (bestIndex != -1)
        {
            TryBuyAndPlace(bestIndex);
        }
        
        cg.alpha = 1;    
    }

    private void TryBuyAndPlace(int targetIndex)
    {
        var price = data.price;

        // 双重检查：钱够不够
        if (MoneyManager.Instance.currentGold < price) return;
        // 尝试直接放置到指定格子
        // 注意：我们需要 InventoryManager 提供一个 "TryPlaceAt" 方法
        // 或者我们可以偷懒，直接调用 AddItem，但那样不能指定位置
            
        // 为了实现"拖到哪放哪"，我们需要手动构建 RuntimeCard
        var newCard = new RuntimeItem(data, null);
            
        // 检查该位置能否放下
        if (InventoryManager.Instance.CanPlaceItem(targetIndex, data.width, data.height, data.shapeOffsets))
        {
            // 生成真物品
            InventoryManager.Instance.CreateItemAt(newCard, targetIndex);
                
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