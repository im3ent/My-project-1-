using UnityEngine;

[CreateAssetMenu(menuName = "Buffs/Double Cast")]
public class DoubleCastBuff : StatusEffect
{
    private void OnEnable() { id = "DoubleCast"; } // 确保 ID 唯一

    public override void OnPlayCard(StatusInstance instance, EffectContext ctx)
    {
        // 只有法术才生效
        if (ctx.SourceCard.cardType == CardType.Spell)
        {
            // ✨ 修改上下文：告诉系统多放一次
            // (你需要去 EffectContext 里加一个 public int repeatCount = 1;)
            ctx.repeatCount += instance.Stacks; // 如果有2层，就多放2次

            // 消耗掉 Buff
            instance.DecreaseStack(instance.Stacks); // 全部消耗
        }
    }
}