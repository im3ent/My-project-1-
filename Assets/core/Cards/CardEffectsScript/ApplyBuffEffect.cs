using UnityEngine;

[CreateAssetMenu(menuName = "Effects/Apply Buff")]
public class ApplyBuffEffect : CardEffect 
{
    [Header("配置")]
    public StatusEffect buffData; // 拖入 ScriptableObject (比如 CostReductionBuff)
    public int stacks = 1;        // 层数
    public EffectTargetType targetType = EffectTargetType.Self; // 给谁加？

    public override float Execute(EffectContext ctx) 
    {
        CharacterBase target = null;
        
        // 简单的目标选择逻辑
        if (targetType == EffectTargetType.Self) target = ctx.caster;
        else if (targetType == EffectTargetType.ManualTarget) target = ctx.mainTarget;

        if (target != null)
        {
            var manager = target.GetComponent<CharacterStateManager>();
            if (manager != null)
            {
                manager.ApplyStatus(buffData, stacks);
            }
        }
        return animateDuration;
    }
}