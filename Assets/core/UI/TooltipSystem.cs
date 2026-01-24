using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class TooltipSystem : MonoBehaviour
{
    public static TooltipSystem Instance;

    public RectTransform tooltipRect;
    public CanvasGroup canvasGroup;
    public TextMeshProUGUI headerText;
    public TextMeshProUGUI contentText;

    public Camera uiCamera; 
    public Canvas rootCanvas;

    [Header("二级悬浮设置")]
    public Transform subContainer;     // 预制体里的一个 VerticalLayoutGroup 容器
    public GameObject subTooltipPrefab; // 上面创建的子项预制体
    
    public float smoothTime = 0.05f;
    // 基础偏移量（用于计算，代码内部会自动处理正负）
    public Vector2 baseOffset = new Vector2(20, 20);

    [Header("延时设置")]
    public float showDelay = 0.5f; 
    public float fadeDuration = 0.2f; 
    public float hideDelay = 0.2f; 
    
    private Tween _delayTween; 
    private Vector2 _currentVelocity;
    private RectTransform _parentRect;
    private bool _isHiding = false; 
    private List<SubTooltipItem> _activeSubTooltips = new();
    private Queue<SubTooltipItem> pool = new();
    private List<SubTooltipItem> activeSubTooltips = new();
    // 缓存当前的动态偏移
    private Vector2 _dynamicOffset;

    void Awake()
    {
        // 单例模式 (支持 PersistentUI 场景)
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // 如果不在 PersistentUI 场景中，可能需要这行
        // DontDestroyOnLoad(gameObject); 
        
        if (rootCanvas == null) rootCanvas = GetComponentInParent<Canvas>();
        // 注意：如果是 Screen Space - Overlay，worldCamera 会是 null，这是正常的
        if (uiCamera == null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            uiCamera = rootCanvas.worldCamera ?? Camera.main;
        }
        
        _parentRect = tooltipRect.parent as RectTransform; 
        
        // 初始状态
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false; // 确保不遮挡射线
    }

    public void Show(ITooltipProvider provider)
    {
        _isHiding = false;
        _delayTween?.Kill();
        canvasGroup.DOKill();
        // 1. 处理主内容
        var main = provider.GetTooltipData();
        headerText.text = main.title;
        contentText.text = main.content;
        headerText.gameObject.SetActive(!string.IsNullOrEmpty(main.title));
        
        ClearActiveSubs();
        var subs = provider.GetSubEntries();
        if (subs is { Count: > 0 })
        {
            subContainer.gameObject.SetActive(true);
            foreach (var entry in subs)
            {
                SubTooltipItem item = GetNextAvailableSubItem();
                item.Setup(entry.title, entry.content);
                activeSubTooltips.Add(item);
            }
        }
        else
        {
            subContainer.gameObject.SetActive(false);
        }
        // 必须立即刷新布局以获取正确的宽高进行边界判定
        LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRect);
        RefreshLayout();
        // 初始位置计算
        var targetPos = CalculateSmartPosition();
        tooltipRect.anchoredPosition = targetPos;

        _delayTween = DOVirtual.DelayedCall(showDelay, () => {
            canvasGroup.DOFade(1, fadeDuration).SetUpdate(true);
        }).SetUpdate(true);
    }

    public void Hide()
    {
        _isHiding = true;
        _delayTween?.Kill();
        canvasGroup.DOKill();

        _delayTween = DOVirtual.DelayedCall(hideDelay, () => {
            canvasGroup.DOFade(0, fadeDuration).OnComplete(ClearActiveSubs).SetUpdate(true);
        }).SetUpdate(true);
    }

    void LateUpdate()
    {
        // 如果正在隐藏且已经完全透明，或者完全不可见，则停止位置计算
        if (canvasGroup.alpha == 0 && _isHiding) return;

        // 实时平滑跟随
        var targetPos = CalculateSmartPosition();
        
        tooltipRect.anchoredPosition = Vector2.SmoothDamp(
            tooltipRect.anchoredPosition, 
            targetPos, 
            ref _currentVelocity, 
            smoothTime
        );

        // Z轴归零
        if (tooltipRect.localPosition.z != 0)
        {
            tooltipRect.localPosition = new Vector3(tooltipRect.localPosition.x, tooltipRect.localPosition.y, 0);
        }
    }

    /// <summary>
    /// 核心逻辑：智能计算位置、Pivot 和 Offset
    /// </summary>
    private Vector2 CalculateSmartPosition()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        
        // 1. 获取鼠标在屏幕上的百分比位置 (0到1)
        float normalizedX = mousePos.x / Screen.width;
        float normalizedY = mousePos.y / Screen.height;

        // 2. 智能判定轴心点 (Pivot)
        // 如果鼠标在右半屏，UI 轴心设在右侧(1)，使其向左伸展
        // 如果鼠标在上半屏，UI 轴心设在上侧(1)，使其向下伸展
        float pivotX = normalizedX > 0.5f ? 1f : 0f;
        float pivotY = normalizedY > 0.5f ? 1f : 0f;
        tooltipRect.pivot = new Vector2(pivotX, pivotY);

        // 3. 智能计算偏移方向
        // 如果 Pivot 是 1 (右/上)，Offset 应该是负的，防止遮挡鼠标
        float offsetX = pivotX == 1f ? -baseOffset.x : baseOffset.x;
        float offsetY = pivotY == 1f ? -baseOffset.y : baseOffset.y;
        Vector2 currentOffset = new Vector2(offsetX, offsetY);

        // 4. 坐标转换
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _parentRect, 
            mousePos, 
            uiCamera, 
            out var localPoint
        );

        return localPoint + currentOffset;
    }

// 从对象池获取或创建
    private SubTooltipItem GetNextAvailableSubItem()
    {
        SubTooltipItem item;
        if (pool.Count > 0)
        {
            item = pool.Dequeue();
        }
        else
        {
            var go = Instantiate(subTooltipPrefab, subContainer);
            item = go.GetComponent<SubTooltipItem>();
        }
        item.gameObject.SetActive(true);
        // 重新设置到容器末尾，保证排序正确
        item.transform.SetAsLastSibling(); 
        return item;
    }
    // 回收所有活动的子项到池中
    private void ClearActiveSubs()
    {
        foreach (var item in activeSubTooltips)
        {
            item.gameObject.SetActive(false);
            pool.Enqueue(item);
        }
        activeSubTooltips.Clear();
    }
    private void RefreshLayout()
    {
        Canvas.ForceUpdateCanvases();
        // 必须自下而上刷新
        LayoutRebuilder.ForceRebuildLayoutImmediate(subContainer as RectTransform);
        LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRect);
    }
}