using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Serialization; // 记得引用 TextMeshPro

public class CardDisplay : MonoBehaviour
{
    [Header("数据源")]
    public RuntimeCard runtimeCard;

    [Header("UI 组件绑定")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public Image artworkImage;

    public TextMeshProUGUI costText;
    public TextMeshProUGUI attackText;
    public TextMeshProUGUI healthText;

    public void Bind(RuntimeCard card)
    {
        runtimeCard = card;
        
        var data = card.Data;
        // 1. 搬运基础信息
        nameText.text = data.cardName;
        descriptionText.text = data.description;
        artworkImage.sprite = data.artwork;
        costText.text = data.manaCost.ToString();

        // 2. 搬运攻防 (如果是法术，可能需要隐藏攻防)
        // 这里简单处理，直接显示
        attackText.text = data.attack.ToString();
        healthText.text = data.health.ToString();
    
        // 第一次刷新
        UpdateDescription();
    }
    
    void Start()
    {
        // 1. 刚出生时，先手动刷新一次
        UpdateDescription();
        UpdateCostUI(); // 顺便把费用也优化了

        // 2. 订阅事件：以后只要 State 变了，我就自动刷新
        if (PlayerStateManager.Instance != null)
        {
            PlayerStateManager.Instance.OnStateChanged += HandleStateChanged;
        }
    }

    private void OnDestroy() // 或者 OnDisable
    {
        // 记得取消订阅！
        if (PlayerStateManager.Instance != null)
            PlayerStateManager.Instance.OnStateChanged -= HandleStateChanged;
        
        if (GameManager.Instance != null)
            GameManager.Instance.OnManaChanged -= HandleManaChanged;
    }
    // 4. 事件处理函数
    private void HandleStateChanged()
    {
        UpdateDescription();
        UpdateCostUI();
    }

    private void HandleManaChanged()
    {
        // 法力变了，主要涉及 颜色 变化 (白 -> 红)
        // 当然，直接调用 UpdateCostUI() 最省事
        UpdateCostUI(); 
    }

    private void UpdateCostUI()
    {
        if (runtimeCard == null) return;
        var cardData = runtimeCard.Data;
        // 1. 问 GM 要现在的价格
        var currentCost = GameManager.Instance.GetModifiedCost(cardData);

        // 2. 更新数字
        costText.text = currentCost.ToString();

        // 3. ✨ 变色逻辑
        if (currentCost < cardData.manaCost)
        {
            costText.color = Color.green; // 便宜了！显示绿色
        }
        else if (currentCost > cardData.manaCost)
        {
            costText.color = Color.red;   // 被加费了(如果你做了这功能)，显示红色
        }
        else
        {
            costText.color = Color.white; // 原价，显示白色
        }
    }

    private void UpdateDescription()
    {
        if (runtimeCard == null) return;
        var cardData = runtimeCard.Data;
        var caster = runtimeCard.Owner;
        var dynamicValues = new List<string>();

        // ✨ 1. 使用 Concat 优雅地连接所有列表 (不会产生额外的 List 内存分配)
        // 这里的顺序依然很重要：Play -> TurnStart -> TurnEnd
        // 注意：如果你有 auraEffects，它们类型不一样，可能需要单独处理或者让 Aura 也继承同样的接口
        var allEffects = Enumerable.Empty<CardEffect>()
            .Concat(cardData.onPlayEffects ?? Enumerable.Empty<CardEffect>())
            .Concat(cardData.onTurnStartEffects ?? Enumerable.Empty<CardEffect>())
            .Concat(cardData.onTurnEndEffects ?? Enumerable.Empty<CardEffect>());
        
        // ✨ 2. 遍历
        foreach (var effect in allEffects)
        {
            // 只需要这一句！UI 根本不在乎你是什么 Effect
            // 它只管问：你有数值吗？有就给我。
            if (effect.GetDescriptionValue(cardData,caster,out var baseVal, out var finalVal))
            {
                dynamicValues.Add(FormatValue(baseVal, finalVal));
            }
        }
        // 3. 格式化
        try
        {
            descriptionText.text = string.Format(cardData.description, dynamicValues.ToArray());
        }
        catch 
        {
            descriptionText.text = cardData.description;
        }

    }

    private string FormatValue(int baseVal, int finalVal)
    {
        if (finalVal > baseVal)
        {
            // 变强了 (比如法强 +1) -> 绿色加粗
            return $"<color=green><b>{finalVal}</b></color>"; 
            // 提示：如果你想要炉石那种亮绿色，可以使用 hex 颜色码：<color=#00FF00>
        }
        else if (finalVal < baseVal)
        {
            // 变弱了 -> 红色加粗
            return $"<color=red><b>{finalVal}</b></color>";
        }
        else
        {
            // 没变 -> 直接返回数字字符串，不带颜色标签
            return finalVal.ToString();
        }
    }

}