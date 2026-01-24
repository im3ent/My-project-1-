using UnityEngine;

[CreateAssetMenu(menuName = "Status Effects/Standard Stat Buff (Additive)")]
public class StatsFlatBuff : StatusEffect
{
    [Header("基础数值修正,一次性")]
    public int baseValue;  

    [Header("要修正类型")]
    // ✨ 让策划在编辑器里选：是翻倍血量，还是翻倍物理伤害？
    public StatsType targetStatType;
    
    public override EffectSnapshot GetInitialSnapshot()
    {
        var snap = base.GetInitialSnapshot();
        // 不设置 stacks，由 ApplyBuffEffect.stacks 决定层数
        snap.SetFloat("BaseValue", baseValue);
        return snap;
    }
    
    public override float GetStatsFlat(StatusInstance instance, StatsType type) 
    {
        if (type == targetStatType)
        {
            return baseValue * instance.snapshot.stacks;
        }
        
        return 0; // 其他伤害不加成
    }
}