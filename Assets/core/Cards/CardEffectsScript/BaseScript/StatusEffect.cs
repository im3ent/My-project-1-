using UnityEngine;

// 这是所有 Buff 的基类 (ScriptableObject)
// 它只定义“规则”，不存“层数”
public abstract class StatusEffect : ScriptableObject
{
    [Header("UI 显示")]
    public string id;            // 唯一ID，如 "DoubleCast"
    public string displayName;   // 显示名
    public Sprite icon;          // 图标
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

    // =================================================
    // 1. 属性计算钩子 (分层计算：先加后乘)
    // =================================================

    // --- A. 攻击力 (Attack) ---
    // 加法层：返回具体加多少数值 (比如 +2)
    public virtual int GetAttackAdditive(StatusInstance instance) => 0;
    // 乘法层：返回倍率 (比如 2.0f 代表翻倍，默认 1.0f)
    public virtual float GetAttackMultiplier(StatusInstance instance) => 1.0f;

    // --- B. 生命上限 (Max Health) ---
    public virtual int GetHealthAdditive(StatusInstance instance) => 0;
    public virtual float GetHealthMultiplier(StatusInstance instance) => 1.0f;

    // --- C. 法术伤害 (Spell Damage) ---
    // 加法层：法术强度 (Spell Power)
    public virtual int GetSpellDamageAdditive(StatusInstance instance) => 0;
    // 乘法层：法伤翻倍 (Spell Damage Multiplier)
    public virtual float GetSpellDamageMultiplier(StatusInstance instance) => 1.0f;

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
    public virtual int ModifyCost(StatusInstance instance, RuntimeCard card, int currentCost) 
    { 
        return currentCost; 
    }

    // 修改造成的物理/技能最终伤害 (如：虚弱 - 造成伤害减少)
    // 注意：这是算完面板后的最终一步修正
    public virtual int ModifyOutgoingDamage(StatusInstance instance, int damage) => damage;

    // 修改受到的伤害 (如：脆弱 - 受到伤害加倍，格挡 - 抵消伤害)
    public virtual int ModifyIncomingDamage(StatusInstance instance, int damage) => damage;
}