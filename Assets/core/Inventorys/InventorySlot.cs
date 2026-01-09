// 文件路径：Assets/core/Inventory/InventorySlot.cs
using UnityEngine;
using UnityEngine.EventSystems;

public class InventorySlot : MonoBehaviour, IDropHandler
{
    // 这个格子的索引 ID (0, 1, 2...)
    public int slotIndex;

    // Assets/core/Inventory/InventorySlot.cs

    public void OnDrop(PointerEventData eventData)
    {
        var item = eventData.pointerDrag?.GetComponent<InventoryItem>();
    
        if (item != null)
        {
            // 👇👇👇 必须加这两行！通知 Draggable 放置成功 👇👇👇
            var draggable = item.GetComponent<Draggable>();
            if (draggable != null) draggable.OnDropSuccess(); 
            // 👆👆👆 缺了这句，物品就会以为失败，自动弹回屏幕中间
        
            // 然后才是管理器的逻辑
            InventoryManager.Instance.OnItemDropped(item, this.slotIndex);
        }
    }
}