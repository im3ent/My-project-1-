using UnityEngine;
[CreateAssetMenu(menuName = "Passives/Double Cast Passive")]
public class DueCastPassive : PassiveEffect
{
    public override void OnPlayCard(CharacterBase source, EffectContext ctx)
    {
        // 1. 只有“随从卡”才生效 (战吼)
        if (ctx.SourceCard.cardType != CardType.Spell) return;
        if (ShouldTrigger(source, ctx.caster) && ctx.SourceCard.cardType == CardType.Spell)
        {
            ctx.repeatCount += 1;
        }
            
        // 如果你想做特效，可以在这里让 source (铜须) 闪一下
        // source.transform.DOPunchScale(...)
    }
}
