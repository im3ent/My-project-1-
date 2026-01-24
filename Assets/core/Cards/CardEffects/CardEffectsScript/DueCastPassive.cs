using UnityEngine;
[CreateAssetMenu(menuName = "Passives/Double Cast Passive")]
public class DueCastPassive : PassiveEffect
{
    public override void OnPlayCard(CharacterBase owner, RuntimeItem source, EffectContext ctx)
    {
        // 1. 只有“随从卡”才生效 (战吼)
        // 注意：原代码检查 Spell，可能命名是为了 Double Cast Spells
        if (ctx.SourceCard.cardType != CardType.Spell) return;
        
        // 2. 检查 Scope (比如只对友军生效)
        if (ShouldTrigger(owner, ctx.caster))
        {
            ctx.repeatCount += 1;
        }
        
        // 记得调用基类，如果有条件锁的话
        base.OnPlayCard(owner, source, ctx);
    }
}
