using UnityEngine;

[CreateAssetMenu(menuName = "GeminiStone/Effects/Draw Card")]
public class DrawCardEffect : CardEffect
{
    public int amount;

    public override float Execute(EffectContext ctx)
    {
        // 抽牌通常是对“使用者自己”生效
        ctx.caster.DrawCard(amount);
        return animateDuration;
    }
}