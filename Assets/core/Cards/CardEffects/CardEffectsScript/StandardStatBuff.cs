using UnityEngine;

[CreateAssetMenu(menuName = "Status Effects/Standard Stat Buff (Additive)")]
public class StandardStatBuff : StatusEffect
{
    [Header("每层提供的数值")]
    public int attackBonus;      // 攻击力
    public int healthBonus;      // 生命上限
    public int spellPowerBonus;  // 法术强度

    // --- 重写加法钩子 ---
    

    public override int GetStatsFlat(StatusInstance instance, StatsType type) 
    {
        if (type == StatsType.True)
        {
            return spellPowerBonus * instance.Stacks;
        }
        
        return 0; // 其他伤害不加成
    }
}