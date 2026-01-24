using UnityEngine;

[CreateAssetMenu(menuName = "GeminiStone/Effects/Heal")]
public class HealEffect : CardEffect
{
    public int value; // 治疗数值 (初始值，之后从快照读取)

    public override float Execute(EffectContext ctx)
    {
        if (ctx.mainTarget != null)
        {
            // ✨ 从快照读取基础治疗值 (精确查找)
            int baseValue = value;
            if (ctx.sourceRuntimeItem != null && !string.IsNullOrEmpty(ctx.snapshotKey))
            {
                if (ctx.sourceRuntimeItem.initialSnapshots.TryGetValue(ctx.snapshotKey, out var snap))
                {
                    baseValue = snap.GetInt("BaseValue", value);
                }
            }
            
            // ✨ 使用 ActionManager 排队执行
            if (ActionManager.Instance != null)
            {
                ActionManager.Instance.AddToBottom(new HealAction(ctx.mainTarget, baseValue, animateDuration));
                return 0;
            }
            else
            {
                ctx.mainTarget.Heal(baseValue);
            }
        }
        return animateDuration;
    }
    
    // ✨ UI 显示：治疗没有加成，直接返回原始快照
    public override EffectSnapshot GetDescriptionSnapshot(RuntimeItem item, EffectSnapshot snapshot)
    {
        // 复制或创建
        var result = snapshot?.Clone() ?? new EffectSnapshot();
        
        // 确保 BaseValue 存在
        int baseVal = result.GetInt("BaseValue", value);
        result.SetInt("BaseValue", baseVal);
        
        // 治疗目前没有加成，FinalValue = BaseValue
        result.SetInt("FinalValue", baseVal);
        
        return result;
    }
    
    /// <summary>
    /// 提供快照数据：存储基础治疗值
    /// </summary>
    public override EffectSnapshot GetInitialSnapshot(RuntimeItem item)
    {
        var snap = new EffectSnapshot();
        snap.SetInt("BaseValue", value);
        return snap;
    }
}