/// <summary>
/// 统一战斗属性修正接口
/// 被 StatusInstance (Buff) 和 PassiveContext (光环/被动) 共同实现
/// </summary>
public interface IStatModifier
{
    // 来源 (方便调试日志)
    string SourceName { get; }

    // 1. 基础属性修正 (Damage Calculation)
    float GetStatsFlat(StatsType type);
    float GetStatsIncreased(StatsType type);
    float GetStatsMore(StatsType type);

    // 2. 受击修正 (Incoming Damage)
    // 需要知道是谁打的 (source) 才能决定是否生效 (比如 "来自亡灵的伤害减半")
    float GetIncomingFlat(CharacterBase attacker);
    float GetIncomingIncreased(CharacterBase attacker);
    float GetIncomingMore(CharacterBase attacker);
}
