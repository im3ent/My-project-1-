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
        gameObject.SetActive(true);
        // A. 安全解绑旧的 (防止对象池复用时出错)
        UnsubscribeEvents();

        runtimeItem = item;
        // B. 搬运静态数据
        var data = item.data;
        nameText.text = data.cardName;
        descriptionText.text = data.description;
        artworkImage.sprite = data.artwork;
        
        // 简单处理攻防显示
        /*if (data.cardType == CardType.Minion)
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
        }*/

        // C. 订阅新主人的事件
        // 只有当主人身上挂了 CharacterStateManager 时才订阅
        if (runtimeItem.owner != null)
        {
            _boundManager = runtimeItem.owner.stateManager;
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
        
        // 🎯 E. 监听卡牌修改事件 (升级、添加词缀等)
        if (DeckManager.Instance != null)
        {
            DeckManager.Instance.OnCardModified += HandleCardModified;
        }
    
        // 第一次手动刷新
        UpdateDescription();
        UpdateCostUI();
    }
    
// ✨ 清理方法：还回池子时重置
    public void ResetCard()
    {
        runtimeItem = null;
        gameObject.SetActive(false);
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
        
        // 🎯 解绑卡牌修改事件
        if (DeckManager.Instance != null)
        {
            DeckManager.Instance.OnCardModified -= HandleCardModified;
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
    
    // 🎯 卡牌被修改时 (升级、添加词缀等)
    private void HandleCardModified(RuntimeItem modifiedItem)
    {
        // 只刷新自己绑定的卡牌
        if (modifiedItem == runtimeItem)
        {
            UpdateDescription();
            UpdateCostUI();
        }
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
        if (currentCost < runtimeItem.data.manaCost)
        {
            costText.color = Color.green; // 便宜了
        }
        else if (currentCost > runtimeItem.data.manaCost)
        {
            costText.color = Color.red;   // 贵了
        }
        else
        {
            costText.color = Color.white; // 原价
        }
    }

    // --- 4. 描述刷新逻辑 ---
    // --- 4. 描述刷新逻辑 ---
    private void UpdateDescription()
    {
        if (runtimeItem == null) return;
        
        var cardData = runtimeItem.data;
        var dynamicValues = new List<object>();

        // ✨ 遍历效果，按顺序收集快照的值
        void ProcessEffectList(System.Collections.Generic.List<CardEffect> effects, string listPrefix)
        {
            if (effects == null) return;
            
            for (int i = 0; i < effects.Count; i++)
            {
                var effect = effects[i];
                if (effect == null) continue;
                
                string key = $"{listPrefix}_{effect.GetType().Name}_{i}";
                
                // 尝试获取原始快照 (可能为 null)
                runtimeItem.initialSnapshots.TryGetValue(key, out var rawSnapshot);
                
                // ✨ 核心：让 Effect 返回可能被修改后的快照
                var modifiedSnapshot = effect.GetDescriptionSnapshot(runtimeItem, rawSnapshot);
                
                // ✨ 统一使用 Converter 提取值
                var args = Converter(modifiedSnapshot);
                dynamicValues.AddRange(args);
            }
        }
        
        ProcessEffectList(cardData.onPlayEffects, "OnPlay");
        ProcessEffectList(cardData.onTurnStartEffects, "OnTurnStart");
        ProcessEffectList(cardData.onTurnEndEffects, "OnTurnEnd");

        try
        {
            string formattedDesc = string.Format(cardData.description, dynamicValues.ToArray());
            // ✨ 关键词高亮处理
            descriptionText.text = KeywordLibrary.Parse(formattedDesc);
        }
        catch 
        {
            // ✨ 即使格式化失败，也尝试处理关键词
            descriptionText.text = KeywordLibrary.Parse(cardData.description);
        }
    }
    
    /// <summary>
    /// 统一转换快照为显示参数
    /// 约定: {0} = stacks, {1} = BaseValue, {2} = FinalValue (如果有)
    /// </summary>
    private object[] Converter(EffectSnapshot snap)
    {
        if (snap == null) return new object[] { 0, 0 };
        
        var args = new List<object>();
        
        // {0} = 层数/次数
        args.Add(snap.stacks);
        
        // {1} = BaseValue (原始)
        int baseVal = snap.GetInt("BaseValue", 0);
        args.Add(baseVal);
        
        // {2} = FinalValue (如果存在，这是子类计算后写入的)
        // 如果没有 FinalValue，用 BaseValue
        int finalVal = snap.GetInt("FinalValue", baseVal);
        
        // 带变色的最终值
        args.Add(FormatValue(baseVal, finalVal));
        
        return args.ToArray();
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