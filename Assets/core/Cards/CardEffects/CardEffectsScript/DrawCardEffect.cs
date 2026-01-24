using UnityEngine;

[CreateAssetMenu(menuName = "GeminiStone/Effects/Draw Card")]
public class DrawCardEffect : CardEffect
{
    public int amount;

    public override float Execute(EffectContext ctx)
    {
        // ✨ 使用 ActionManager 排队执行
        if (ActionManager.Instance != null)
        {
            ActionManager.Instance.AddToBottom(new DrawCardAction(ctx.caster, amount, animateDuration));
            return 0;
        }
        else
        {
            for (var i = 0; i < amount; i++)
            {
                HandManager.Instance.DrawCard(ctx.caster);
            }
        }
        return animateDuration;
    }
}