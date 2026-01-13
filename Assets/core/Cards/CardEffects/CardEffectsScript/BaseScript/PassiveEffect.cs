using UnityEngine;

public enum PassiveScope
{
    Global,        // 全局生效 (敌我都行，极少用)
    AllAllies,    // 全体友军
    AllEnemies,   // 全体敌人
    SelfOnly,     // 仅限持有者
    OtherAllies,   // 其他友军
    
    Adjacent,     // 仅限邻居 (随从：站位相邻；物品：背包格子相邻)
    SameRow,      // 同一行
    SameColumn,  // 同一列
    TopNeighbor,
    LeftNeighbor,
}

public abstract class PassiveEffect : ScriptableObject
{
    [Header("通用配置")]
    [TextArea] public string description;
    public PassiveScope scope = PassiveScope.Global;
    
    // --- RuntimeItem 对 CharacterBase 发出 Passive---
    public bool ShouldTrigger(CharacterBase owner, CharacterBase target)
    {
        if (owner == null || target == null) return false;
        return scope switch
        {
            PassiveScope.SelfOnly => owner == target,
            PassiveScope.AllAllies => owner.isEnemy == target.isEnemy,
            PassiveScope.AllEnemies => owner.isEnemy != target.isEnemy,
            PassiveScope.OtherAllies => owner != target && owner.isEnemy == target.isEnemy,
            PassiveScope.Global => true,
            _ => false
        };
    }
    
    
    // 区间 1: 基础数值修正 (Flat)
    // 例如：法术伤害 +5
    public virtual int GetSpellDamageFlat(CharacterBase owner, StatsType type) => 0;

    // 区间 2: 加法增伤 (Increased/Additive)
    // 例如：法术伤害 +10% (返回 0.1f)，多个 buff 是相加关系 (10% + 20% = 30%)
    public virtual float GetSpellDamageIncreased(CharacterBase owner, StatsType type) => 0f;

    // 区间 3: 独立乘伤 (More/Multiplicative)
    // 例如：法术伤害翻倍 (返回 2.0f)，或者是 造成 50% 更多伤害 (1.5f)
    // 多个 buff 是相乘关系 (1.5 * 2.0 = 3.0)
    public virtual float GetSpellDamageMore(CharacterBase owner, StatsType type) => 1.0f;


    // =================================================
    // 3. 逻辑流程钩子
    // =================================================

    // 当某人打牌时
    // owner: 被动持有者
    // ctx: 打牌上下文 (包含 ctx.caster 施法者)
    public virtual void OnPlayCard(CharacterBase owner, EffectContext ctx) { }
}