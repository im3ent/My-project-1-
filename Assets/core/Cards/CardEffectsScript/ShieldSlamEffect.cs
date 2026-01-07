using UnityEngine;

[CreateAssetMenu(menuName = "GeminiStone/Effects/Special/Shield Slam")]
public class ShieldSlamEffect : CardEffect
{
    // 这张卡不需要配置 "value"，因为伤害是动态算的
    public DamageType damageType;
    public override float Execute(EffectContext ctx)
    {
        
        if (ctx.caster == null || ctx.mainTarget == null) return 0;

        // --- 核心逻辑：动态计算 ---
        // 伤害 = 施法者的当前护甲值
        var casterArmor = ctx.caster.currentArmor;

        var finalDamage = GameManager.Instance.GetModifiedDamage(ctx.sourceRuntimeCard, casterArmor);

        var damageInfo = new DamageInfo(finalDamage, damageType, ctx.caster);
        ctx.mainTarget.TakeDamage(damageInfo);
        return animateDuration;
    }
    public override bool GetDescriptionValue(RuntimeCard card, out int baseVal, out int finalVal)
    {
        // 1. 直接从 Context 里拿施法者 (预览时，施法者通常就是玩家)
        // 防御性编程：ctx.Caster 理论上不为空，但判一下更安全
        var armor = (card.Owner != null) ? card.Owner.currentArmor : 0;
        baseVal = armor;
        // 直接在这里调用 GM 的计算公式，把逻辑封装在效果内部
        finalVal = GameManager.Instance.GetModifiedDamage(card, baseVal);
        return true; // 告诉 UI：我有数值，请填坑
    }
}