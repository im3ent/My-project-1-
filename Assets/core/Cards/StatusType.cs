// 定义所有的 Buff/Debuff 类型
public enum StatusType
{
    None,
    Vulnerable, // 易伤：受到伤害 x2
    Weak,       // 虚弱：造成伤害 -2
    Stunned,    // 眩晕：不能行动
    Poison      // 中毒：回合结束扣血
}

// 定义一个状态实例（比如“易伤，持续2回合”）
[System.Serializable]
public class StatusEffectInstance
{
    public StatusType type;
    public int duration; // 持续回合数
    public int value;    // 强度（例如中毒层数，或者护盾值）

    public StatusEffectInstance(StatusType type, int duration, int value = 0)
    {
        this.type = type;
        this.duration = duration;
        this.value = value;
    }
}