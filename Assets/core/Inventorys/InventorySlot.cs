using UnityEngine;
using UnityEngine.EventSystems;

public class InventorySlot : MonoBehaviour, IDropHandler
{
    public int slotIndex;

    // 这个方法必须保留以实现 IDropHandler 接口，但内容可以留空
    // 因为实际的检测逻辑已经移交给了 Draggable.OnEndDrag 的射线检测
    public void OnDrop(PointerEventData eventData)
    {
        // 留空！不要在这里写逻辑！
        // 让 Draggable 自己去算位置，这样就不会出现“抓右下角导致位置偏右”的问题。
    }
}