using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 拖拽层级管理器
/// 确保拖拽的物体始终显示在所有 UI 之上
/// </summary>
public class DragLayerFix : MonoBehaviour
{
    [Header("拖拽虚影")]
    [Tooltip("商店或背包的拖拽虚影，需要始终显示在顶层")]
    public RectTransform dragGhost;

    private Canvas topCanvas;
    private Transform originalParent;
    private int originalSiblingIndex;

    private void Awake()
    {
        // 查找最顶层的 Canvas（通常是 PersistentUI 或一个专用的 Overlay Canvas）
        FindTopCanvas();
    }

    /// <summary>
    /// 查找最顶层的 Canvas（URP：优先找 PersistentUI 场景的 Canvas）
    /// </summary>
    private void FindTopCanvas()
    {
        Canvas[] allCanvases = FindObjectsOfType<Canvas>(true);
        
        // 🎯 URP 架构：优先查找 PersistentUI 场景的 Canvas（由 Overlay Camera 渲染）
        foreach (var canvas in allCanvases)
        {
            // 检查 Canvas 所在场景名
            if (canvas.gameObject.scene.name == "PersistentUI")
            {
                topCanvas = canvas;
                Debug.Log($"[DragLayerFix] 找到 PersistentUI Canvas: {canvas.name}, Scene: {canvas.gameObject.scene.name}");
                return;
            }
        }

        // 备选方案：找 Sort Order 最高的
        int maxSortOrder = int.MinValue;
        foreach (var canvas in allCanvases)
        {
            if (canvas.sortingOrder > maxSortOrder)
            {
                maxSortOrder = canvas.sortingOrder;
                topCanvas = canvas;
            }
        }

        if (topCanvas != null)
        {
            Debug.Log($"[DragLayerFix] 找到顶层 Canvas (备选): {topCanvas.name}, Sort Order: {topCanvas.sortingOrder}");
        }
        else
        {
            Debug.LogWarning("[DragLayerFix] 未找到 Canvas！");
        }
    }

    /// <summary>
    /// 拖拽开始时调用 - 移动到顶层
    /// </summary>
    public void OnDragStart()
    {
        if (dragGhost == null || topCanvas == null) return;

        // 保存原始位置
        originalParent = dragGhost.parent;
        originalSiblingIndex = dragGhost.GetSiblingIndex();

        // 移动到顶层 Canvas
        dragGhost.SetParent(topCanvas.transform, true);
        dragGhost.SetAsLastSibling(); // 确保在该 Canvas 的最上层

        Debug.Log($"[DragLayerFix] 拖拽物体已移至顶层: {topCanvas.name}");
    }

    /// <summary>
    /// 拖拽结束时调用 - 恢复原位
    /// </summary>
    public void OnDragEnd()
    {
        if (dragGhost == null || originalParent == null) return;

        // 恢复到原始父物体
        dragGhost.SetParent(originalParent, true);
        dragGhost.SetSiblingIndex(originalSiblingIndex);

        Debug.Log("[DragLayerFix] 拖拽物体已恢复原位");
    }

    /// <summary>
    /// 简化版：直接设置到顶层（不恢复）
    /// 适合 globalDragGhost 这种本来就应该在顶层的物体
    /// </summary>
    public void EnsureTopLayer()
    {
        if (dragGhost == null) return;

        if (topCanvas == null)
        {
            FindTopCanvas();
        }

        if (topCanvas != null && dragGhost.parent != topCanvas.transform)
        {
            dragGhost.SetParent(topCanvas.transform, true);
            dragGhost.SetAsLastSibling();
            Debug.Log($"[DragLayerFix] globalDragGhost 已永久移至顶层: {topCanvas.name}");
        }
    }
}
