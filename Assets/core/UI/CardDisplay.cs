using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardDisplay : MonoBehaviour
{
    [Header("数据源")]
    public RuntimeItem runtimeItem;

    [Header("UI 组件绑定")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public Image artworkImage;

    public TextMeshProUGUI costText;
    public TextMeshProUGUI attackText;
    public TextMeshProUGUI healthText;

    // 记录一下当前监听的管理器，方便解绑
    private CharacterStateManager _boundManager;

    // --- 1. 初始化绑定 (核心修改) ---
    public void Bind(RuntimeItem item)
    {
        // A. 安全解绑旧的 (防止对象池复用时出错)
        UnsubscribeEvents();

        runtimeItem = item;
        
        // B. 搬运静态数据
        var data = item.Data;
        nameText.text = data.cardName;
        descriptionText.text = data.description;
        artworkImage.sprite = data.artwork;
        
        // 简单处理攻防显示
        if (data.cardType == CardType.Minion)
        {
            attackText.text = data.attack.ToString();
            healthText.text = data.health.ToString();
            attackText.gameObject.SetActive(true);
            healthText.gameObject.SetActive(true);
        }
        else
        {
            attackText.gameObject.SetActive(false);
            healthText.gameObject.SetActive(false);
        }

        // C. 订阅新主人的事件
        // 只有当主人身上挂了 CharacterStateManager 时才订阅
        if (runtimeItem.Owner != null)
        {
            _boundManager = runtimeItem.Owner.GetComponent<CharacterStateManager>();
            if (_boundManager != null)
            {
                // ✨ 监听：只要主人的 Buff 变了，我就刷新描述和费用
                _boundManager.OnStateChanged += HandleStateChanged;
            }
        }

        // D. 监听全局法力值变化 (这个还在 GameManager 里，没变)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnManaChanged += HandleManaChanged;
        }
    
        // 第一次手动刷新
        UpdateDescription();
        UpdateCostUI();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    // 辅助：统一解绑逻辑
    private void UnsubscribeEvents()
    {
        // 解绑状态变化
        if (_boundManager != null)
        {
            _boundManager.OnStateChanged -= HandleStateChanged;
            _boundManager = null;
        }

        // 解绑法力变化
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnManaChanged -= HandleManaChanged;
        }
    }

    // --- 2. 事件响应 ---
    private void HandleStateChanged()
    {
        // 状态变了 (比如加了减费Buff，或者获得法强)，刷新所有
        UpdateDescription();
        UpdateCostUI();
    }

    private void HandleManaChanged()
    {
        // 法力值变了，只刷新费用颜色
        UpdateCostUI(); 
    }

    // --- 3. 费用刷新逻辑 (调用新接口) ---
    private void UpdateCostUI()
    {
        if (runtimeItem == null) return;

        // ✨ 关键修改：调用 GameManager 的新方法，传入 RuntimeCard
        // (GameManager 内部会去找 Owner 的 StateManager 计算费用)
        var currentCost = GameManager.Instance.GetModifiedCost(runtimeItem);

        // 更新文本
        costText.text = currentCost.ToString();

        // 变色逻辑
        if (currentCost < runtimeItem.Data.manaCost)
        {
            costText.color = Color.green; // 便宜了
        }
        else if (currentCost > runtimeItem.Data.manaCost)
        {
            costText.color = Color.red;   // 贵了
        }
        else
        {
            costText.color = Color.white; // 原价
        }
    }

    // --- 4. 描述刷新逻辑 (微调参数) ---
    private void UpdateDescription()
    {
        if (runtimeItem == null) return;
        
        var cardData = runtimeItem.Data;
        var caster = runtimeItem.Owner;
        var dynamicValues = new List<string>();

        // 连接所有效果列表
        var allEffects = Enumerable.Empty<CardEffect>()
            .Concat(cardData.onPlayEffects ?? Enumerable.Empty<CardEffect>())
            .Concat(cardData.onTurnStartEffects ?? Enumerable.Empty<CardEffect>())
            .Concat(cardData.onTurnEndEffects ?? Enumerable.Empty<CardEffect>());
        
        foreach (var effect in allEffects)
        {
            // ✨ 这里传入 caster (RuntimeCard.Owner)
            // 确保 ShieldSlamEffect 能拿到正确的护甲值
            if (effect.GetDescriptionValue(runtimeItem, out var baseVal, out var finalVal))
            {
                dynamicValues.Add(FormatValue(baseVal, finalVal));
            }
        }

        try
        {
            descriptionText.text = string.Format(cardData.description, dynamicValues.ToArray());
        }
        catch 
        {
            // 防止策划配表配错了 (比如写了 {0} 但没有效果提供数值)
            descriptionText.text = cardData.description;
        }
    }

    // 辅助：数值变色
    private string FormatValue(int baseVal, int finalVal)
    {
        if (finalVal > baseVal)
            return $"<color=green><b>{finalVal}</b></color>"; 
        else if (finalVal < baseVal)
            return $"<color=red><b>{finalVal}</b></color>";
        else
            return finalVal.ToString();
    }
}