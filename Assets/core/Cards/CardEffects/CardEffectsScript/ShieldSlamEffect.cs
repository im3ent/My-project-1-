using UnityEngine;

[CreateAssetMenu(menuName = "GeminiStone/Effects/Special/Shield Slam")]
public class ShieldSlamEffect : CardEffect
{
    // 这张卡不需要配置 "value"，因为伤害是动态算的
    public StatsType statsType;
    
    public override float Execute(EffectContext ctx)
    {
        if (ctx.caster == null || ctx.mainTarget == null) return 0;

        // --- 核心逻辑：动态计算 ---
        // 伤害 = 施法者的当前护甲值 (实时读取)
        var casterArmor = ctx.caster.currentArmor;

        var finalDamage = GameManager.Instance.GetModifiedDamage(ctx.sourceRuntimeItem, casterArmor);

        var damageInfo = new DamageInfo(finalDamage, statsType, ctx.caster);
        ctx.mainTarget.TakeDamage(damageInfo);
        return animateDuration;
    }
    
    // ✨ UI 显示：ShieldSlam 基于实时护甲，不从快照读
    public override EffectSnapshot GetDescriptionSnapshot(RuntimeItem item, EffectSnapshot snapshot)
    {
        var result = snapshot?.Clone() ?? new EffectSnapshot();
        
        // 直接读取当前护甲值作为 BaseValue
        int armor = (item.owner != null) ? item.owner.currentArmor : 0;
        result.SetInt("BaseValue", armor);
        
        // 计算 FinalValue
        int finalVal = GameManager.Instance.GetModifiedDamage(item, armor);
        result.SetInt("FinalValue", finalVal);
        
        return result;
    }
    
    /// <summary>
    /// ShieldSlam 不需要静态快照，伤害基于实时护甲
    /// 但仍返回一个快照以保持一致性
    /// </summary>
    public override EffectSnapshot GetInitialSnapshot(RuntimeItem item)
    {
        var snap = new EffectSnapshot();
        snap.SetInt("BaseValue", item.owner?.currentArmor ?? 0);
        return snap;
    }
}