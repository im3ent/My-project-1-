using UnityEngine;
[CreateAssetMenu(menuName = "Buffs/BurnDotDamageBuff")]
public class BurnDotDamageBuff : StatusEffect
{
    [Header("燃烧配置")]
    public int baseValue = 5; // 显式定义，不再用 customValues[1]
    // 每一层的基础伤害
    public StatsType statsType = StatsType.True;
    
    public override EffectSnapshot GetInitialSnapshot()
    {
        var snap = base.GetInitialSnapshot();
        // 不设置 stacks，由 ApplyBuffEffect.stacks 决定层数
        snap.SetFloat("BaseValue", baseValue);
        return snap;
    }

    public override void OnTurnStart(StatusInstance instance)
    {
        // 1. 获取基础总伤 (层数 * 单层伤害)
        // 从快照里读 "BaseValue"
        float baseDmg = instance.snapshot.GetFloat("BaseValue", baseValue);
        var rawDamage = baseDmg * instance.snapshot.stacks;
        var finalDamage = rawDamage;
        
        // 2. 获取施法者 (是谁下的毒？)
        // 注意：如果你之前没在 StatusInstance 里存 Caster，这里只能用 instance.Owner (那就是受击者自己烫自己)
        // 建议使用 Caster，因为通常我的法强越高，我给你挂的燃烧就越痛
        var attacker = instance.Caster; // 假设你在 StatusInstance 加上了 Caster 字段
        
        if (attacker != null)
        {
            var attackerState = attacker.stateManager;
            if (attackerState != null)
            {
                // ✨ 关键调用：把基础伤害扔进施法者的计算器里滚一圈
                finalDamage = attackerState.GetModifiedStats(rawDamage, statsType);
            }
        }
        else
        {
            // 如果找不到施法者（比如环境火），就直接用基础值，或者走受击者的防御公式
            // finalDamage = instance.Owner.GetModifiedIncomingDamage(rawDamage);
        }

        // 3. 造成伤害
        // 燃烧通常是真实伤害(True)或者元素伤害
        instance.Owner.ownerCharacter.TakeDamage(
            new DamageInfo(finalDamage, statsType, attacker)
        );

        // 4. 结算层数
        instance.DecreaseStack(1);
    }
}
