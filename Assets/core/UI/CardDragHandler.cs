using System;
using core.UI;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems; // 必须引用这个！
using UnityEngine.InputSystem; // 必须加这个！
// 这一行告诉 Unity：这个脚本要处理拖拽事件
public class CardDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public float triggerLine = 0.3f;   //只要鼠标超过这条线，就视为“想打牌”
    private bool isLockedMode = false;
    private CanvasGroup _canvasGroup;
    private Transform _originalParent; // 记住原来的家（手牌区域）
    private int _originalIndex;        // 记住原来的位置
    private CardDisplay _cardDisplay;  // 为了获取卡牌数据 同是挂载在预制体上的CardDisplay
    

    
    [HideInInspector] public bool isDragging = false; // 新增标记
    // 我们需要找到 Canvas（画布），因为拖拽时要相对于画布移动
    private Canvas _mainCanvas;
    private Camera _mainCamera; // ✨ 缓存摄像机引用，避免每帧调用 Camera.main

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _cardDisplay = GetComponent<CardDisplay>();
        // 找场景里的 Canvas (通常是根 Canvas)
       
    }

    private void Start()
    {
        _mainCanvas = GetComponentInParent<Canvas>();
        _mainCamera = Camera.main; // ✨ 只在 Start 时获取一次
    }

    // --- 1. 开始拖拽 ---
    public void OnBeginDrag(PointerEventData eventData)
    {
        // 如果不是玩家回合，禁止拖拽！
        if (!GameManager.Instance.isPlayerTurn) {
            return; 
        }
        isDragging = true; // ✅ 标记开始
        
        
        // 强制重置悬停状态 (防止卡牌变大着被拖走)
        GetComponent<CardHoverHandler>()?.ResetHover();
        
        // 记下原来的家，万一打不出去还得回来
        _originalParent = transform.parent;
        _originalIndex = transform.GetSiblingIndex();

        // 关键操作 A：脱离手牌布局
        // 我们把它临时挂到 Canvas 根节点下，这样它就不受 LayoutGroup 束缚，可以自由飞翔了
        transform.SetParent(_mainCanvas.transform, true);

        // 关键操作 B：让鼠标射线能穿透这张卡
        // 这样如果你把它拖到敌人头上，鼠标能识别到敌人，而不是被卡牌挡住
        _canvasGroup.blocksRaycasts = false;
    }

    // --- 2. 拖拽中 ---
    public void OnDrag(PointerEventData eventData)
    {
        if (_originalParent == null) return;
        // A. 卡牌跟随鼠标 (原有逻辑)
        if (!RectTransformUtility.ScreenPointToWorldPointInRectangle(
                (RectTransform)transform.parent,
                eventData.position,
                eventData.pressEventCamera,
                out var globalMousePos)) return;
        globalMousePos.z = 0; 
        
        if (_cardDisplay.runtimeItem.data.needsTarget)
        {
            if (Mouse.current.position.ReadValue().y > Screen.height * triggerLine)
            {
                if (!isLockedMode)
                {
                    isLockedMode = true;
                    transform.DOMove(transform.position + Vector3.down * .5f, 0.5f);
                }
                // 旋转指向：让卡牌的"头"对着鼠标
                var direction = globalMousePos - transform.position;
                var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                // 注意：UI默认向上是90度，所以这里可能需要 -90 或根据你的资源调整
                transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
                DragArrow.Instance.UpdateArrow(transform.position, globalMousePos);
            }
            else
            {
                if (isLockedMode) {
                    isLockedMode = false;
                    transform.DOKill(); // 停止平滑移动动画
                }
                DragArrow.Instance.Hide();
                // 卡牌跟随鼠标 (标准逻辑)
                transform.position = globalMousePos;
                // 旋转归零
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.identity, Time.deltaTime * 10f);
            }
        } //如果需要目标则返回
        else
        {
            transform.position = globalMousePos;
        }

    }
    
    /// <summary>
    /// 🎯 检测鼠标是否在屏幕外（左右或下方，上方允许因为那是打牌区）
    /// </summary>
    private bool IsOutOfScreen(Vector2 screenPos)
    {
        // 留一点边距防止误触
        float margin = 10f;
        
        // 左右边界
        if (screenPos.x < margin || screenPos.x > Screen.width - margin)
            return true;
        
        // 下边界（上边界不限制，那是打牌区）
        if (screenPos.y < margin)
            return true;
        
        return false;
    }

    // --- 3. 拖拽结束 (松手) ---
    public void OnEndDrag(PointerEventData eventData)
    {
        if (_originalParent == null) return;
        _canvasGroup.blocksRaycasts = true;
        
        // 🎯 如果鼠标在屏幕外（左右或下方），自动归位
        var mousePos = Mouse.current.position.ReadValue();
        if (IsOutOfScreen(mousePos))
        {
            ReturnToHand();
            isDragging = false;
            isLockedMode = false;
            transform.DOKill();
            transform.rotation = Quaternion.identity;
            DragArrow.Instance.Hide();
            return;
        }

        // --- 逻辑分支 ---
        // 1. 获取卡牌信息
        var card = _cardDisplay.runtimeItem.data;
        // 2. 如果不需要目标 (AOE、抽牌) -> 只要拖到上半区就打出
        if (!card.needsTarget)
        {
            //把卡牌当前的世界坐标(比如 Y = 3.5) 转成屏幕像素坐标(比如 Y = 800)
            if (mousePos.y > Screen.height * triggerLine)
            {
                TryPlayCard(null);
            }
            else
            {
                ReturnToHand();
            }
        }
        // 3. 如果需要目标 (火球术、治疗术) -> 必须拖到角色头上
        else
        {
            var target = GetTargetUnderMouse();

            if (target != null)
            {
                // 找到了目标！打出卡牌
                TryPlayCard(target);
            }
            else
            {
                // 没打中任何人，或者是打到了空地上
                // 以后这里可以加个提示 "需要选择目标"
                ReturnToHand();
            }
        }
        // --- 隐藏箭头 ---
        DragArrow.Instance.Hide();
        isDragging = false; // ✅ 标记结束
        isLockedMode = false;
        transform.DOKill();
        transform.rotation = Quaternion.identity;
    }

    private CharacterBase GetTargetUnderMouse()
    {
        // 旧写法 (报错原因):
        // Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        //  新写法 (使用新系统):
        // Mouse.current.position.ReadValue() 等同于旧的 Input.mousePosition
        var screenPosition = Mouse.current.position.ReadValue();
        Vector2 worldPos = _mainCamera.ScreenToWorldPoint(screenPosition);

        // 射线检测逻辑
        var hit = Physics2D.Raycast(worldPos, Vector2.zero);

        if (hit.collider == null) return null;
        var target = hit.collider.GetComponent<CharacterBase>();
        return target;

    }

    private void TryPlayCard(CharacterBase target)
    {
        var card = _cardDisplay.runtimeItem;
        if (GameManager.Instance.PlayCard(card, target))
        {
            // 🎯 无限手牌逻辑
            if (card.data != null && card.data.returnToHand)
            {
                // 打出成功，但不销毁，而是飞回手牌
                ReturnToHand();
            }
            else
            {
                // 正常打出，销毁 UI 对象
                CardPoolManager.Instance.RecycleCard(_cardDisplay);
            }
        } else {
            // 没钱，回家
            ReturnToHand();
        }
    }
    
    private void ReturnToHand()
    {
        // 回到手牌区
        transform.SetParent(_originalParent);
        transform.SetSiblingIndex(_originalIndex); // 回到原来的排序位置
        
        // 🎯 重置位置和旋转（让 LayoutGroup 重新控制位置）
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;
        
        // 🎯 强制刷新布局
        if (_originalParent != null)
        {
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(_originalParent as RectTransform);
        }
    }
}