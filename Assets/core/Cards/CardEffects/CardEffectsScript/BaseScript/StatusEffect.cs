using UnityEngine;

// 这是所有 Buff 的基类 (ScriptableObject)
// 它只定义“规则”，不存“层数”
public abstract class StatusEffect : ScriptableObject
{
    [Header("UI 显示")]
    public string id;            // 唯一ID，如 "DoubleCast"
    public string displayName; 
    
    public string descriptionConfig;// 显示名
    public Sprite icon;          // 图标

    /// <summary>
    /// 获取该状态在卡牌初始化时的默认快照数据
    /// </summary>
    public virtual EffectSnapshot GetInitialSnapshot()
    {
        // 不设置 stacks，由调用方 (ApplyBuffEffect) 决定层数
        return new EffectSnapshot();
    }

    protected virtual void OnEnable()
    {
        // 如果你在 Inspector 里没填 ID，我就默认用这个文件的名字
        if (string.IsNullOrEmpty(id))
        {
            id = this.name;  
        }
    }
    [Header("行为配置")]
    public bool isStackable = true; // 是否可堆叠 (比如中毒)
    public bool removeAtTurnEnd;    // 是否回合结束自动移除 (比如本回合法强+1)
    
    // 区间 1: 基础数值修正 (Flat)
    // 例如：法术伤害 +5
    public virtual float GetStatsFlat(StatusInstance instance, StatsType type) => 0;

    // 区间 2: 加法增伤 (Increased/Additive)
    // 例如：法术伤害 +10% (返回 0.1f)，多个 buff 是相加关系 (10% + 20% = 30%)
    public virtual float GetStatsIncreased(StatusInstance instance, StatsType type) => 0f;

    // 区间 3: 独立乘伤 (More/Multiplicative)
    // 例如：法术伤害翻倍 (返回 2.0f)，或者是 造成 50% 更多伤害 (1.5f)
    // 多个 buff 是相乘关系 (1.5 * 2.0 = 3.0)
    public virtual float GetStatsMore(StatusInstance instance, StatsType type) => 1.0f;


    // =================================================
    // 2. 流程事件钩子 (Hooks)
    // =================================================

    // 回合开始时 (如：中毒扣血)
    public virtual void OnTurnStart(StatusInstance instance) { }

    // 回合结束时 (如：移除临时Buff)
    public virtual void OnTurnEnd(StatusInstance instance) { }

    // 卡牌打出时 (如：双倍施法，或者每打一张牌受1点伤)
    // 你可以在这里修改 ctx (比如增加 repeatCount)
    public virtual void OnPlayCard(StatusInstance instance, EffectContext ctx) { }

    // =================================================
    // 3. 其他数值修改
    // =================================================
    // 修改卡牌费用 (如：下张法术减费)
    public virtual int ModifyCost(StatusInstance instance, RuntimeItem item, int currentCost) 
    { 
        return currentCost; 
    }

    // 修改造成的物理/技能最终伤害 (如：虚弱 - 造成伤害减少)
    // 注意：这是算完面板后的最终一步修正
    public virtual float ModifyOutgoingDamage(StatusInstance instance, float damage) => damage;

    // 修改受到的伤害 (如：脆弱 - 受到伤害加倍，格挡 - 抵消伤害)
    public virtual float FlatIncomingDamage(StatusInstance instance) => 0;
    public virtual float IncreasedIncomingDamage(StatusInstance instance) => 0;

    public virtual float MoreIncomingDamage(StatusInstance instance) => 1;
    //最后还可以传给旧的接口做特殊处理（比如圣盾直接变0）
    public virtual float ModifyIncomingDamage(StatusInstance instance, float damage) => damage;

}