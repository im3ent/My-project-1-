using UnityEngine;

[CreateAssetMenu(menuName = "Status Effects/Multiplier Buff")]
public class MultiplierBuff : StatusEffect
{
    [Header("配置")]
    public float multiplier = 2.0f;
    
    [Header("要翻倍的属性类型")]
    public StatsType targetStatType;
    public override EffectSnapshot GetInitialSnapshot()
    {
        var snap = base.GetInitialSnapshot();
        // 不设置 stacks，由 ApplyBuffEffect.stacks 决定层数
        snap.SetFloat("BaseValue", multiplier);
        return snap;
    }

    public override float GetStatsMore(StatusInstance instance , StatsType type)
    {
        if (type == targetStatType)
        {
            return Mathf.Approximately(multiplier, 1.0f) ? 1.0f : Mathf.Pow(multiplier, instance.snapshot.stacks);
        }

        return 1;
    }
    
}