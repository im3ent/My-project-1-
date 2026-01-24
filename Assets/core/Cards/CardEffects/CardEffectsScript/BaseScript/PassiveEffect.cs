using UnityEngine;
using System.Collections.Generic;

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
    
    [Header("额外解锁条件 (可选)")]
    public BaseCondition unlockCondition;

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
    public virtual int GetStatsFlat(CharacterBase owner, StatsType type) => 0;

    // 区间 2: 加法增伤 (Increased/Additive)
    // 例如：法术伤害 +10% (返回 0.1f)，多个 buff 是相加关系 (10% + 20% = 30%)
    public virtual float GetStatsIncreased(CharacterBase owner, StatsType type) => 0f;

    // 区间 3: 独立乘伤 (More/Multiplicative)
    // 例如：法术伤害翻倍 (返回 2.0f)，或者是 造成 50% 更多伤害 (1.5f)
    // 多个 buff 是相乘关系 (1.5 * 2.0 = 3.0)
    public virtual float GetStatsMore(CharacterBase owner, StatsType type) => 1.0f;
    public virtual float GetIncomingFlat(CharacterBase source, CharacterBase target) => 0f;
    public virtual float GetIncomingIncreased(CharacterBase source, CharacterBase target) => 0f;
    public virtual float GetIncomingMore(CharacterBase source, CharacterBase target) => 1.0f;


    // =================================================
    // 3. 逻辑流程钩子
    // =================================================

    // 当某人打牌时
    // owner: 被动持有者
    // source: 提供被动的物品 (Context)
    // ctx: 打牌上下文 (包含 ctx.caster 施法者)
    public virtual void OnPlayCard(CharacterBase owner, RuntimeItem source, EffectContext ctx) 
    {
         if (unlockCondition != null) unlockCondition.OnPlayCard(owner, source, ctx);
    }
    // 4. 数据初始化钩子
    public virtual EffectSnapshot GetInitialSnapshot()
    {
        return null; 
    }

    public virtual void OnTurnStart(CharacterBase owner, RuntimeItem sourceItem) { }
    public virtual void OnTurnOver(CharacterBase owner, RuntimeItem sourceItem) { }

    // ✨ 多态重构：让 Effect 自己决定怎么把自己“贴”到目标物品上
    // 默认行为：单纯地把 Effect 加到目标的临时被动列表里
    public virtual void ApplyToInventoryItem(RuntimeItem target, RuntimeItem source)
    {
        target.AddTemporaryPassive(this, source);
    }

    // ✨ 多态重构：让 Effect 自己决定解锁哪些格子
    public virtual IEnumerable<int> GetUnlockedSlotIndices(InventoryItem sourceItem) 
    {
        return System.Linq.Enumerable.Empty<int>();
    }

    // ✨ 条件系统：只有满足条件时，这个被动才生效
    // 比如：击杀数 > 10，或者 周围有3个火属性物品
    public virtual bool IsConditionMet(RuntimeItem source)
    {
        // 如果资产里配置了条件，则询问条件对象；否则默认 true
        if (unlockCondition != null) return unlockCondition.IsMet(source);
        return true; 
    }

    // ✨ 杀敌事件钩子
    // owner: 被动持有者 (通常是玩家)
    // source: 哪个物品提供的这个被动 (用来存数据到 source.Snapshot)
    // victim: 被杀死的单位
    public virtual void OnUnitKilled(CharacterBase owner, RuntimeItem source, CharacterBase victim) 
    {
        if (unlockCondition != null) unlockCondition.OnUnitKilled(owner, source, victim);
    }

    // ✨ 售卖被动钩子 (托球手机制)
    // source: 正在被卖掉的这个物品
    public virtual void OnSell(RuntimeItem source) { }
}