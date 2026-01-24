using UnityEngine;
[CreateAssetMenu(menuName = "Buffs/SpellPowerBuff")]
public class SpellPowerBuff : StatusEffect
{
    public float amountPerStack = 1;


    public override float GetStatsFlat(StatusInstance instance, StatsType type)
    {
        // 假设只有 Magic/Spell 类型生效，或者简单粗暴全生效（取决于策划配置）
        // 这里为了安全，先全生效，或者你可以加个 public StatsType targetType;
        if (type == StatsType.Magical) 
             return amountPerStack * instance.snapshot.stacks;
        
        return 0;
    }

}
