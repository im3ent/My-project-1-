using System.Collections.Generic;
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
        // ✨ 安全检查：防止配置错误导致崩溃
        if (buffData == null)
        {
            Debug.LogError($"[ApplyBuffEffect] buffData is null! Please assign a StatusEffect in the Inspector.");
            return animateDuration;
        }
        // 1. 确定我们要 Buff 的目标列表
        var targets = new List<CharacterBase>();

        switch (targetType)
        {
            case EffectTargetType.ManualTarget:
                if (ctx.mainTarget != null) targets.Add(ctx.mainTarget);
                break;

            case EffectTargetType.Self:
                if (ctx.caster != null) targets.Add(ctx.caster);
                break;

            case EffectTargetType.LastSpawned:
                // ✨ 关键点：从 Context 中获取上一个召唤的随从
                if (ctx.LastCreatedUnit != null) 
                    targets.Add(ctx.LastCreatedUnit);
                break;

            case EffectTargetType.AllAllies:
                targets.AddRange(GameManager.Instance.allies);
                break;

            case EffectTargetType.AllEnemies:
                targets.AddRange(GameManager.Instance.enemies);
                break;
            case EffectTargetType.AllUnits:
                targets.AddRange(GameManager.Instance.allUnits);
                break;
        }

        // 2. 统一施加状态
        foreach (var t in targets)
        {
            var manager = t.GetComponent<CharacterStateManager>();
            if (manager != null)
            {
                manager.ApplyStatus(buffData,ctx.caster, stacks);
            }
        }
        return animateDuration;
    }
}
public enum EffectTargetType 
{
    ManualTarget,
    LastSpawned,
    Self,
    AllEnemies,
    AllAllies,
    AllUnits
}