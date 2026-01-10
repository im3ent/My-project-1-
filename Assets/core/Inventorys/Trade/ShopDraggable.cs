using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShopDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private ShopSlot shopSlot;
    private GameObject dragIcon; // 拖拽时的临时图标
    private Canvas canvas;

    private void Awake()
    {
        shopSlot = GetComponent<ShopSlot>();
        canvas = GetComponentInParent<Canvas>(); // 获取商店的 Canvas
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (shopSlot.itemToSell == null) return;
        
        // 1. 检查钱够不够（不够连拖都不让拖）
        if (MoneyManager.Instance.currentGold < shopSlot.itemToSell.price)
        {
            Debug.Log("钱不够，拖不动！");
            return;
        }

        // 2. 生成一个临时的图标跟随鼠标
        dragIcon = new GameObject("ShopDragIcon");
        dragIcon.transform.SetParent(canvas.transform); // 放在最上层
        dragIcon.AddComponent<Canvas>().overrideSorting = true;
        dragIcon.GetComponent<Canvas>().sortingOrder = 999; // 确保置顶

        // 复制图标
        Image img = dragIcon.AddComponent<Image>();
        img.sprite = shopSlot.iconImage.sprite;
        img.raycastTarget = false; // 必须 false，否则挡住下面的射线
        
        // 设置大小
        RectTransform rect = dragIcon.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(100, 100); // 或者读取配置的格子大小
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragIcon != null)
        {
            dragIcon.transform.position = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragIcon != null) Destroy(dragIcon);

        // 3. 核心：检测鼠标松开位置下面是不是背包
        // 我们利用 InventoryManager 现有的射线检测逻辑吗？
        // 不，这里我们直接检测有没有碰到 InventorySlot
        
        InventorySlot hitSlot = RaycastForInventorySlot(eventData);

        if (hitSlot != null)
        {
            TryBuyAndPlace(hitSlot.slotIndex);
        }
    }

    private void TryBuyAndPlace(int targetIndex)
    {
        CardDefinition data = shopSlot.itemToSell;
        int price = data.price;

        // 双重检查：钱够不够
        if (MoneyManager.Instance.currentGold >= price)
        {
            // 尝试直接放置到指定格子
            // 注意：我们需要 InventoryManager 提供一个 "TryPlaceAt" 方法
            // 或者我们可以偷懒，直接调用 AddItem，但那样不能指定位置
            
            // 为了实现"拖到哪放哪"，我们需要手动构建 RuntimeCard
            RuntimeCard newCard = new RuntimeCard(data, null);
            
            // 检查该位置能否放下
            if (InventoryManager.Instance.CanPlaceItem(targetIndex, data.width, data.height))
            {
                // 生成真物品
                InventoryManager.Instance.CreateItemAt(newCard, targetIndex);
                
                // 扣钱
                MoneyManager.Instance.SpendGold(price);
                
                Debug.Log("拖拽进货成功！");
            }
            else
            {
                Debug.Log("这里放不下！");
            }
        }
    }

    // 简单的射线检测找格子
    private InventorySlot RaycastForInventorySlot(PointerEventData eventData)
    {
        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            var slot = result.gameObject.GetComponent<InventorySlot>();
            if (slot != null) return slot;
        }
        return null;
    }
}