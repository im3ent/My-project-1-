using UnityEngine;

[CreateAssetMenu(menuName = "GeminiStone/Effects/Heal")]
public class HealEffect : CardEffect
{
    public int value; // 治疗数值

    public override float Execute(EffectContext ctx)
    {
        if (ctx.mainTarget != null)
        {
            ctx.mainTarget.Heal(value);
            
        }
        return animateDuration;
    }
    public override bool GetDescriptionValue(RuntimeItem item, out int baseVal, out int finalVal)
    {
        baseVal = value;
        // 直接在这里调用 GM 的计算公式，把逻辑封装在效果内部
        finalVal = 1;//GameManager.Instance.GetModifiedDamage(card, value);
        return true; // 告诉 UI：我有数值，请填坑
    }
}