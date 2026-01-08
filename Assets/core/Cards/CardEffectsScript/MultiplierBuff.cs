using UnityEngine;

[CreateAssetMenu(menuName = "Status Effects/Multiplier Buff")]
public class MultiplierBuff : StatusEffect
{
    [Header("倍率配置 (1.0 = 不变)")]
    public float attackMultiplier = 1.0f;
    public float healthMultiplier = 1.0f;
    public float spellDamageMultiplier = 1.0f;

    // --- 重写乘法钩子 ---

    public override float GetAttackMultiplier(StatusInstance instance)
    {
        // 逻辑：如果是翻倍(2.0)，叠2层就是 2*2=4倍
        // 也就意味着是用 Power (指数) 计算s
        if (attackMultiplier == 1.0f) return 1.0f;
        return Mathf.Pow(attackMultiplier, instance.Stacks);
    }

    public override float GetHealthMultiplier(StatusInstance instance)
    {
        if (healthMultiplier == 1.0f) return 1.0f;
        return Mathf.Pow(healthMultiplier, instance.Stacks);
    }

    public override float GetSpellDamageMultiplier(StatusInstance instance)
    {
        if (spellDamageMultiplier == 1.0f) return 1.0f;
        return Mathf.Pow(spellDamageMultiplier, instance.Stacks);
    }
}