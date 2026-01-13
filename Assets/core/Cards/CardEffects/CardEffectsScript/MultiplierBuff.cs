using UnityEngine;

[CreateAssetMenu(menuName = "Status Effects/Multiplier Buff")]
public class MultiplierBuff : StatusEffect
{
    [Header("乘法倍率配置 (1.0 = 不变)")]

    public float Multiplier = 1.0f;


    // --- 重写乘法钩子 ---

    public override float GetStatsMore(StatusInstance instance , StatsType type)
    {
        if (type == StatsType.Health)
        {
            if (Mathf.Approximately(Multiplier, 1.0f)) return 1.0f;
            return Mathf.Pow(Multiplier, instance.Stacks);
        }

        return 1;
    }
    
}