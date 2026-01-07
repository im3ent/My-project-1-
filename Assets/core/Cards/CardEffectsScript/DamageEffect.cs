using UnityEngine;

[CreateAssetMenu(menuName = "GeminiStone/Effects/Damage")]
public class DamageEffect : CardEffect
{
    public int value; // 伤害数值
    public DamageType damageType = DamageType.Physical; 
    public bool ignoreArmor = false; // 特殊词条
    public override float Execute(EffectContext ctx)
    {
        if (ctx.mainTarget != null)
        {
            var finalDamage = GameManager.Instance.GetModifiedDamage(ctx.sourceCard, value);

            var damageInfo = new DamageInfo(finalDamage, damageType, ctx.caster);
            ctx.mainTarget.TakeDamage(damageInfo);
        }
        else
        {
        }
        return animateDuration;
    }
    public override bool GetDescriptionValue(CardDefinition card, CharacterBase owner, out int baseVal, out int finalVal)
    {
        baseVal = value;
        // 直接在这里调用 GM 的计算公式，把逻辑封装在效果内部
        finalVal = GameManager.Instance.GetModifiedDamage(card, value);
        return true; // 告诉 UI：我有数值，请填坑
    }
}