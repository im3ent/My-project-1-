using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 全局拖拽虚影管理器（放在 PersistentUI 场景）
/// 提供跨场景的拖拽虚影，确保始终显示在最顶层
/// 层级结构：TooltipPanelCanvas => Ghost（空物体）=> globalDragGhost
/// </summary>
public class GlobalDragGhostManager : MonoBehaviour
{
    public static GlobalDragGhostManager Instance;

    [Header("拖拽虚影")]
    public Image dragGhostImage;
    
    [Header("Canvas 引用")]
    [Tooltip("PersistentUI 的 Canvas（用于坐标转换）")]
    public Canvas targetCanvas;

    // 缓存 RectTransform
    private RectTransform canvasRectTransform;
    private RectTransform parentRect; // 🎯 缓存父物体 RectTransform
    private Camera uiCamera;
    
    // 🎯 公开 UI Camera 供外部使用（如 RaycastForSlot）
    public Camera UICamera => uiCamera;
    
    // 🎯 触点偏移（记录鼠标点击位置与虚影中心的差值）
    private Vector2 touchOffset;

    private void Awake()
    {
        // 单例保护（在 PersistentUI 中，通过 Additive 保留）
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // 初始状态隐藏
        if (dragGhostImage != null)
        {
            dragGhostImage.gameObject.SetActive(false);
        }
        
        // 缓存 Canvas 引用
        CacheCanvasReferences();
        
        // 🎯 确保虚影的 Pivot 是 (0.5, 0.5)
        if (dragGhostImage != null)
        {
            dragGhostImage.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            parentRect = dragGhostImage.transform.parent as RectTransform;
            if (parentRect == null) parentRect = canvasRectTransform;
        }
    }

    /// <summary>
    /// 缓存 Canvas 和相机引用
    /// </summary>
    private void CacheCanvasReferences()
    {
        if (targetCanvas == null && dragGhostImage != null)
        {
            targetCanvas = dragGhostImage.canvas;
        }
        
        if (targetCanvas != null)
        {
            canvasRectTransform = targetCanvas.transform as RectTransform;
            
            // 🎯 关键：获取正确的相机
            // Screen Space - Camera 模式需要用 worldCamera
            // Screen Space - Overlay 模式 worldCamera 为 null
            uiCamera = targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay 
                ? null 
                : targetCanvas.worldCamera;
        }
    }
    
    /// <summary>
    /// 显示拖拽虚影（简单版，无偏移）
    /// </summary>
    public void ShowGhost(Sprite sprite, Vector2 size)
    {
        if (dragGhostImage == null) return;

        dragGhostImage.sprite = sprite;
        dragGhostImage.rectTransform.sizeDelta = size;
        dragGhostImage.preserveAspect = true;
        dragGhostImage.gameObject.SetActive(true);
        
        // 重置偏移
        touchOffset = Vector2.zero;
    }
    
    /// <summary>
    /// 显示虚影并记录初始偏移（保持鼠标与虚影的相对位置）
    /// </summary>
    public void ShowGhostWithOffset(Sprite sprite, Vector2 size, Vector2 mouseScreenPosition, Vector2 originalScreenPosition)
    {
        if (dragGhostImage == null) return;

        // 🎯 确保虚影的 Anchor 和 Pivot 都是居中的
        dragGhostImage.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        dragGhostImage.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        dragGhostImage.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        
        dragGhostImage.sprite = sprite;
        dragGhostImage.rectTransform.sizeDelta = size;
        dragGhostImage.preserveAspect = true;
        dragGhostImage.gameObject.SetActive(true);
        
        // 🎯 在 PersistentUI 坐标系中计算偏移
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            mouseScreenPosition,
            uiCamera,
            out Vector2 localMousePos
        );
        
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            originalScreenPosition,
            uiCamera,
            out Vector2 localOriginalPos
        );
        
        // 计算本地坐标系的偏移
        touchOffset = localMousePos - localOriginalPos;
        
        // 🎯 使用 anchoredPosition（相对于锚点的位置）
        dragGhostImage.rectTransform.anchoredPosition = localOriginalPos;
    }

    /// <summary>
    /// 隐藏拖拽虚影
    /// </summary>
    public void HideGhost()
    {
        if (dragGhostImage != null)
        {
            dragGhostImage.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 更新虚影位置（通常在拖拽时每帧调用）
    /// </summary>
    public void UpdateGhostPosition(Vector2 screenPosition)
    {
        if (dragGhostImage == null || parentRect == null) return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            screenPosition,
            uiCamera,
            out Vector2 localMousePos))
        {
            // 🎯 使用 anchoredPosition
            dragGhostImage.rectTransform.anchoredPosition = localMousePos - touchOffset;
        }
    }

    /// <summary>
    /// 设置虚影透明度
    /// </summary>
    public void SetGhostAlpha(float alpha)
    {
        if (dragGhostImage != null)
        {
            var color = dragGhostImage.color;
            color.a = alpha;
            dragGhostImage.color = color;
        }
    }

    /// <summary>
    /// 快捷方法：显示并跟随鼠标
    /// </summary>
    public void ShowAndFollowMouse(Sprite sprite, Vector2 size, float alpha = 0.7f)
    {
        ShowGhost(sprite, size);
        SetGhostAlpha(alpha);
        UpdateGhostPosition(Input.mousePosition);
    }
}
