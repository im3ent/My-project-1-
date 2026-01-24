using UnityEngine;
[CreateAssetMenu(fileName = "ReduceCostBuff", menuName = "Buffs/ReduceCostBuff")]
public class ReduceCostBuff : StatusEffect
{
    public int costReduce;
    public override int ModifyCost(StatusInstance instance, RuntimeItem item, int currentCost) 
    { 
        
        return currentCost - costReduce; 
    }
    public override void OnPlayCard(StatusInstance instance, EffectContext ctx)
    {
        // 只有牌真的打出去了，才消耗这个减费 Buff
        instance.DecreaseStack(1);
    }
}
