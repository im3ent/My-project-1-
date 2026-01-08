using UnityEngine;
using UnityEngine.EventSystems; // 必须引用：用于检测鼠标进入/离开
using DG.Tweening; // 必须引用：动画

public class CardHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("悬停配置")]
    public float hoverScale = 1.2f;       // 放大倍数
    public float hoverMoveY = 30f;        // 上浮距离 (像素/单位)
    public float animDuration = 0.2f;     // 动画时间

    private Vector3 originalScale;
    private Vector3 originalPosition;
    private int originalSortingOrder;
    
    // 用来改变层级，让卡牌盖住别人
    private Canvas cardCanvas; 
    private bool isHovering;
    private float originalLocalY;
    
    // 引用拖拽脚本，防止拖拽时触发悬停
    private CardDragHandler dragHandler;

    void Awake()
    {
        dragHandler = GetComponent<CardDragHandler>();
        
        // 自动添加 Canvas 组件 (如果预制体上没有)
        // 这是为了能动态修改 sortingOrder
        cardCanvas = GetComponent<Canvas>();
        if (cardCanvas == null) {
            cardCanvas = gameObject.AddComponent<Canvas>();
        }
        
        // 必须勾选 overrideSorting 才能单独控制这张牌的层级
        cardCanvas.overrideSorting = true; 
        
        // 这里需要配合 GraphicRaycaster 才能接收鼠标事件
        if (GetComponent<UnityEngine.UI.GraphicRaycaster>() == null) {
            gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }
        
    }

    void Start()
    {
        // --- 修复问题 1：层级遮挡 ---
        // 动态添加的 Canvas 默认 Order 是 0，很容易被背景遮住。
        // 我们给它一个基础值，比如 10 (确保比背景大)
        if (cardCanvas != null) {
            cardCanvas.sortingOrder = 10; 
        }
        originalSortingOrder = 10; // 记住这个基础值是 10，而不是 0

        // --- 修复问题 2：出生动画冲突 ---
        // 强行认定标准大小是 1，不管现在是不是正在播放 0->1 的动画
        originalScale = Vector3.one;
        
        // ✅ 记录原始 Y 坐标 (通常是 0)
        originalLocalY = transform.localPosition.y;
        
    }

    // --- 鼠标进入 (OnPointerEnter) ---
    public void OnPointerEnter(PointerEventData eventData)
    {
        // 如果正在拖拽，就不要触发悬停动画
        if (dragHandler != null && dragHandler.isDragging) return;
        
        isHovering = true;
        
        
        // 1. 杀掉旧动画 (防止快速进出导致动画错乱)
        transform.DOKill();

        // 2. 调整层级 (最重要！让我在最上面！)
        cardCanvas.sortingOrder = 100; // 设个大数

        // 3. 播放动画
        // 变大
        transform.DOScale(originalScale * hoverScale, animDuration).SetEase(Ease.OutBack);
        // 上浮 (注意：如果用了 LayoutGroup，直接改 Y 可能会抖动，这里用 LocalMove 尝试一下)
        transform.DOLocalMoveY(originalLocalY + hoverMoveY, animDuration);
    }

    // --- 鼠标离开 (OnPointerExit) ---
    public void OnPointerExit(PointerEventData eventData)
    {
        // 如果正在拖拽，也不处理离开逻辑 (交给拖拽脚本处理)
        if (dragHandler != null && dragHandler.isDragging) return;

        isHovering = false;

        // 1. 杀掉旧动画
        transform.DOKill();

        // 2. 恢复层级
        cardCanvas.sortingOrder = originalSortingOrder;

        // 3. 恢复状态
        transform.DOScale(originalScale, animDuration);
        // 恢复位置 (回退刚才上浮的距离)
        transform.DOLocalMoveY(originalLocalY, animDuration);
    }
    
    // 如果拖拽开始，强行重置状态 (供 CardDragHandler 调用)
    public void ResetHover() {
        if (!isHovering) return;
        isHovering = false;
        transform.DOKill();
        cardCanvas.sortingOrder = originalSortingOrder;
        transform.localScale = originalScale;
    }
}