using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "NewRandomDamage", menuName = "CardEffects/Random Damage")]
public class RandomDamageEffect : CardEffect {
    
    [Header("配置")]
    public int value = 1;   // 每一发多少伤害 (初始值，之后从快照读取)
    public int repeatCount = 3;    // 随机打几次
    public float delayBetweenHits = 0.2f;

    [Header("属性")]
    public StatsType statsType = StatsType.Magical;

    public override float Execute(EffectContext ctx) {
        // ✨ 从快照读取基础伤害值和重复次数
        int baseValue = value;
        int count = repeatCount;
        
        if (ctx.sourceRuntimeItem != null && !string.IsNullOrEmpty(ctx.snapshotKey))
        {
            // ✨ 精确查找
            if (ctx.sourceRuntimeItem.initialSnapshots.TryGetValue(ctx.snapshotKey, out var snap))
            {
                baseValue = snap.GetInt("BaseValue", value);
                count = snap.GetInt("RepeatCount", repeatCount);
            }
        }
        
        int finalDamage = GameManager.Instance.GetModifiedDamage(ctx.sourceRuntimeItem, baseValue);

        // ✨ 使用 ActionManager 排队执行每一次伤害
        if (ActionManager.Instance != null)
        {
            for (var i = 0; i < count; i++) 
            {
                var randomEnemy = GameManager.Instance.GetRandomEnemy();
                if (randomEnemy == null) continue;

                var info = new DamageInfo(finalDamage, statsType, ctx.caster);
                ActionManager.Instance.AddToBottom(new DamageAction(randomEnemy, info, delayBetweenHits));
            }
            return 0; // ActionManager 控制时序
        }
        else
        {
            // 回退：直接执行
            for (var i = 0; i < count; i++) 
            {
                var randomEnemy = GameManager.Instance.GetRandomEnemy();
                if (randomEnemy == null) continue;

                var info = new DamageInfo(finalDamage, statsType, ctx.caster);
                randomEnemy.TakeDamage(info);
            }
        }
        return animateDuration;
    }
    
    // ✨ UI 显示：计算伤害加成，并用 stacks 存储次数
    public override EffectSnapshot GetDescriptionSnapshot(RuntimeItem item, EffectSnapshot snapshot)
    {
        var result = snapshot?.Clone() ?? new EffectSnapshot();
        
        // 用 stacks 存储次数 (Converter 用 {0})
        result.stacks = result.GetInt("RepeatCount", repeatCount);
        
        // BaseValue
        int baseVal = result.GetInt("BaseValue", value);
        result.SetInt("BaseValue", baseVal);
        
        // FinalValue (伤害加成)
        int finalVal = GameManager.Instance.GetModifiedDamage(item, baseVal);
        result.SetInt("FinalValue", finalVal);
        
        return result;
    }
    
    /// <summary>
    /// 提供快照数据：存储基础伤害值和重复次数
    /// </summary>
    public override EffectSnapshot GetInitialSnapshot(RuntimeItem item)
    {
        var snap = new EffectSnapshot();
        snap.SetInt("BaseValue", value);
        snap.SetInt("RepeatCount", repeatCount);
        return snap;
    }
}