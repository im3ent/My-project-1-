using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Effects/Apply Buff")]
public class ApplyBuffEffect : CardEffect 
{
    [Header("配置")]
    public StatusEffect buffData; // 拖入 ScriptableObject (比如 CostReductionBuff)
    public int stacks = 1;        // 允许在卡牌上配置层数 (默认为1)

    public EffectTargetType targetType = EffectTargetType.Self; // 给谁加？

    public override float Execute(EffectContext ctx) 
    {
        // 安全检查：防止配置错误导致崩溃
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
                if (ctx.LastCreatedUnit != null) 
                    targets.Add(ctx.LastCreatedUnit);
                break;

            case EffectTargetType.AllAllies:
                targets.AddRange(GameManager.Instance.Allies);
                break;

            case EffectTargetType.AllEnemies:
                targets.AddRange(GameManager.Instance.Enemies);
                break;
            case EffectTargetType.AllUnits:
                targets.AddRange(GameManager.Instance.AllUnits);
                break;
            default:  
                if (ctx.caster != null) targets.Add(ctx.caster);
                break;
        }

        // 2. 统一施加状态
        foreach (var t in targets)
        {
            var manager = t.stateManager;
            if (manager != null)
            {
                // ✨ 从 RuntimeItem 的字典里找对应的快照
                EffectSnapshot snap = null;
                
                // 1. 尝试使用精确 Key (ctx.snapshotKey)
                if (!string.IsNullOrEmpty(ctx.snapshotKey) && 
                    ctx.sourceRuntimeItem.initialSnapshots.ContainsKey(ctx.snapshotKey))
                {
                    snap = ctx.sourceRuntimeItem.initialSnapshots[ctx.snapshotKey];
                }
                // 2. 回退：使用 buffData.id 别名 (向后兼容)
                else if (!string.IsNullOrEmpty(buffData.id) && 
                    ctx.sourceRuntimeItem.initialSnapshots.ContainsKey(buffData.id))
                {
                    snap = ctx.sourceRuntimeItem.initialSnapshots[buffData.id];
                }
                
                // 回退
                if (snap == null) 
                {
                    snap = ctx.sourceRuntimeItem.Snapshot; 
                }
                
                // ✨ 使用 ActionManager 排队执行
                if (ActionManager.Instance != null)
                {
                    ActionManager.Instance.AddToBottom(new ApplyBuffAction(t, buffData, ctx.caster, snap, animateDuration));
                }
                else
                {
                    manager.ApplyStatus(buffData, ctx.caster, snap);
                }
            }
        }
        
        // 如果使用 ActionManager，返回 0 让它控制时序
        return ActionManager.Instance != null ? 0 : animateDuration;
    }
    
    // ✨ UI 显示：返回快照（可能带 FinalValue）
    public override EffectSnapshot GetDescriptionSnapshot(RuntimeItem item, EffectSnapshot snapshot)
    {
        var result = snapshot?.Clone() ?? new EffectSnapshot();
        
        // 确保 stacks 正确（优先用快照值，否则用配置值）
        if (result.stacks <= 0) result.stacks = stacks;
        
        // 如果有 BaseValue，计算 FinalValue
        if (result.ContainsKey("BaseValue"))
        {
            int baseVal = result.GetInt("BaseValue", 0);
            int finalVal = GameManager.Instance.GetModifiedDamage(item, baseVal);
            result.SetInt("FinalValue", finalVal);
        }
        
        return result;
    }
    
    /// <summary>
    /// 提供快照数据：从 buffData 获取初始快照并覆盖层数
    /// </summary>
    public override EffectSnapshot GetInitialSnapshot(RuntimeItem item)
    {
        // 先尝试从 buffData 获取
        var snap = buffData?.GetInitialSnapshot()?.Clone();
        
        // 如果没有，创建一个新的
        if (snap == null) snap = new EffectSnapshot();
        
        // 用卡牌配置的层数覆盖
        snap.stacks = stacks;
        
        return snap;
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