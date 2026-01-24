// Assets/core/Cards/CardEffectsScript/DamageEffect.cs

using UnityEngine;

[CreateAssetMenu(menuName = "GeminiStone/Effects/Damage")]
public class DamageEffect : CardEffect
{
    [Header("数值配置")]
    public int value; // 基础伤害 (初始值，之后从快照读取)
    
    [Header("机制配置")]
    public StatsType statsType = StatsType.Physical; 
    public bool ignoreArmor = false; // 是否无视护甲

    public override float Execute(EffectContext ctx)
    {
        // 1. 必须有目标才能打伤害
        if (ctx.mainTarget != null)
        {
            // ✨ 从快照读取基础伤害值
            int baseValue = value;
            if (ctx.sourceRuntimeItem != null && !string.IsNullOrEmpty(ctx.snapshotKey))
            {
                if (ctx.sourceRuntimeItem.initialSnapshots.TryGetValue(ctx.snapshotKey, out var snap))
                {
                    baseValue = snap.GetInt("BaseValue", value);
                }
            }
            
            // 2. 获取修正后的伤害
            int finalDamage = GameManager.Instance.GetModifiedDamage(ctx.sourceRuntimeItem, baseValue);

            // 3. 构建伤害信息包
            var damageInfo = new DamageInfo(finalDamage, statsType, ctx.caster)
            {
                ignoreArmor = this.ignoreArmor
            };

            // ✨ 4. 使用 ActionManager 排队执行（如果可用）
            if (ActionManager.Instance != null)
            {
                ActionManager.Instance.AddToBottom(new DamageAction(ctx.mainTarget, damageInfo, animateDuration));
                return 0; // 返回 0，让 ActionManager 控制时序
            }
            else
            {
                // 回退：直接执行（兼容旧模式）
                ctx.mainTarget.TakeDamage(damageInfo);
            }
        }
        else
        {
            string cardName = ctx.sourceRuntimeItem != null ? ctx.sourceRuntimeItem.data.cardName : "未知卡牌";
            Debug.LogWarning($"DamageEffect 执行失败：没有目标。卡牌：{cardName}");
        }

        return animateDuration;
    }

    // ✨ UI 显示：计算加成后的伤害，存入快照副本返回
    public override EffectSnapshot GetDescriptionSnapshot(RuntimeItem item, EffectSnapshot snapshot)
    {
        // 1. 复制原始快照（或创建新的）
        var result = snapshot?.Clone() ?? new EffectSnapshot();
        
        // 2. 读取基础值
        int baseVal = result.GetInt("BaseValue", value);
        result.SetInt("BaseValue", baseVal); // 确保存在
        
        // 3. 计算加成后的值
        int finalVal = GameManager.Instance.GetModifiedDamage(item, baseVal);
        result.SetInt("FinalValue", finalVal);
        
        return result;
    }
    
    /// <summary>
    /// 提供快照数据：存储基础伤害值
    /// </summary>
    public override EffectSnapshot GetInitialSnapshot(RuntimeItem item)
    {
        var snap = new EffectSnapshot();
        snap.SetInt("BaseValue", value);
        return snap;
    }
}