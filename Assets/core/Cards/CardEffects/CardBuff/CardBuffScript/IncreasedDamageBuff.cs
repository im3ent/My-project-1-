using UnityEngine;

[CreateAssetMenu(menuName = "Status Effects/Increased Damage Buff")]
public class IncreasedDamageBuff : StatusEffect
{
    [Header("每层提供的百分比 (0.1 = 10%)")]
    public float percentPerStack = 0.1f;
    public StatsType targetType;

    // 重写这个钩子
    public override float GetStatsIncreased(StatusInstance instance, StatsType type) 
    {
        // 只有当计算的伤害类型匹配时才生效
        if (type == targetType)
        {
            // 返回：百分比 * 层数
            return percentPerStack * instance.snapshot.stacks;
        }
        
        return 0f;
    }
}
