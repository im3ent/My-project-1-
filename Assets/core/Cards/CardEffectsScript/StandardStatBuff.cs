using UnityEngine;

[CreateAssetMenu(menuName = "Status Effects/Standard Stat Buff (Additive)")]
public class StandardStatBuff : StatusEffect
{
    [Header("每层提供的数值")]
    public int attackBonus;      // 攻击力
    public int healthBonus;      // 生命上限
    public int spellPowerBonus;  // 法术强度

    // --- 重写加法钩子 ---

    public override int GetAttackAdditive(StatusInstance instance)
    {
        
        // 数值 = 单层加成 * 层数
        return attackBonus * instance.Stacks;
    }

    public override int GetHealthAdditive(StatusInstance instance)
    {
        return healthBonus * instance.Stacks;
    }

    public override int GetSpellDamageAdditive(StatusInstance instance)
    {
        return spellPowerBonus * instance.Stacks;
    }
}