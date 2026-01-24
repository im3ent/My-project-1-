using UnityEngine;

public class PassiveContext : IStatModifier
{
    public PassiveEffect effect;   // 效果数据 (ScriptableObject)
    public RuntimeItem source;     // 提供这个被动的物品/技能
    
    public PassiveContext(PassiveEffect effect, RuntimeItem source)
    {
        this.effect = effect;
        this.source = source;
    }

    // ==========================================================
    // ✨ 实现 IStatModifier 接口
    // ==========================================================
    public string SourceName => effect != null ? effect.name : "Unknown Passive";

    // 1. 基础属性修正 (给 passive 持有者加属性)
    public float GetStatsFlat(StatsType type)
    {
        if (effect == null || source?.owner == null) return 0;
        // ✨ 应用倍率 (Flat 直接乘)
        return effect.GetStatsFlat(source.owner, type) * source.passiveMultiplier;
    }

    public float GetStatsIncreased(StatsType type)
    {
        if (effect == null || source?.owner == null) return 0;
        // ✨ 应用倍率 (百分比加成直接乘，比如 +50% * 2 = +100%)
        return effect.GetStatsIncreased(source.owner, type) * source.passiveMultiplier;
    }

    public float GetStatsMore(StatsType type)
    {
        if (effect == null || source?.owner == null) return 1f;
        
        float baseMore = effect.GetStatsMore(source.owner, type);
        // ✨ 应用倍率 (独立乘区需要用指数，模拟“触发多次”)
        // 比如 1.5 倍伤害，触发 2 次 = 1.5 * 1.5 = 2.25
        return Mathf.Pow(baseMore, source.passiveMultiplier);
    }

    // 2. 受击修正 (当 passive 持有者挨打时)
    public float GetIncomingFlat(CharacterBase attacker)
    {
        if (effect == null || source?.owner == null) return 0;
        return effect.GetIncomingFlat(attacker, source.owner) * source.passiveMultiplier;
    }

    public float GetIncomingIncreased(CharacterBase attacker)
    {
        if (effect == null || source?.owner == null) return 0;
        return effect.GetIncomingIncreased(attacker, source.owner) * source.passiveMultiplier;
    }

    public float GetIncomingMore(CharacterBase attacker)
    {
        if (effect == null || source?.owner == null) return 1f;
        float baseMore = effect.GetIncomingMore(attacker, source.owner);
        return Mathf.Pow(baseMore, source.passiveMultiplier);
    }
}
