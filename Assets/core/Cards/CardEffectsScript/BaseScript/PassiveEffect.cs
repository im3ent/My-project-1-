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


    // --- A. 攻击力 (Attack) ---
    public virtual int GetAttackAdditive(CharacterBase owner) => 0;      // 加多少
    public virtual float GetAttackMultiplier(CharacterBase owner) => 1.0f; // 乘多少

    // --- B. 生命上限 (Max Health) ---
    public virtual int GetHealthAdditive(CharacterBase owner) => 0;
    public virtual float GetHealthMultiplier(CharacterBase owner) => 1.0f;

    // --- C. 法术伤害 (Spell Damage) ---
    public virtual int GetSpellDamageAdditive(CharacterBase owner) => 0;
    public virtual float GetSpellDamageMultiplier(CharacterBase owner) => 1.0f;

    // =================================================
    // 3. 逻辑流程钩子
    // =================================================

    // 当某人打牌时
    // owner: 被动持有者
    // ctx: 打牌上下文 (包含 ctx.caster 施法者)
    public virtual void OnPlayCard(CharacterBase owner, EffectContext ctx) { }
}