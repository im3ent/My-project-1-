// Assets/core/UI/Draggable.cs

using UnityEngine;
using UnityEngine.EventSystems;

public class Draggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private CanvasGroup canvasGroup;
    public bool isDroppedSuccessfully = false; // 成功标志位
    private Transform originalParent; // 备用：只有失败时才用它回滚

    private void Awake() {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData) {
        isDroppedSuccessfully = false; // 每次拖拽开始重置
        originalParent = transform.parent; // 记住老家，万一没人要就回这里
        
        // 临时提到最上层展示
        transform.SetParent(transform.root); 
        canvasGroup.blocksRaycasts = false; 
    }

    public void OnDrag(PointerEventData eventData) {
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData) {
        canvasGroup.blocksRaycasts = true;

        // ✨✨✨ 逻辑核心 ✨✨✨
        // 如果 InventorySlot 已经调用了 OnDropSuccess()，说明物品已经被 Manager 接管并放好了。
        // 这时候 Draggable 绝对不能再碰 transform！
        if (isDroppedSuccessfully) 
        {
            return; // 直接退出，啥也不干
        }

        // 只有失败了（比如拖到空地），才执行回滚
        Debug.Log("放置失败，回滚原处");
        transform.SetParent(originalParent);
        transform.localPosition = Vector3.zero; // 归零回滚
    }

    public void OnDropSuccess() {
        isDroppedSuccessfully = true;
    }
}