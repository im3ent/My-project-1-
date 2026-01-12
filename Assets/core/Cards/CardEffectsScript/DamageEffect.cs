// Assets/core/Cards/CardEffectsScript/DamageEffect.cs

using UnityEngine;

[CreateAssetMenu(menuName = "GeminiStone/Effects/Damage")]
public class DamageEffect : CardEffect
{
    [Header("数值配置")]
    public int value; // 基础伤害
    
    [Header("机制配置")]
    public DamageType damageType = DamageType.Physical; 
    public bool ignoreArmor = false; // 是否无视护甲

    public override float Execute(EffectContext ctx)
    {
        // 1. 必须有目标才能打伤害
        if (ctx.mainTarget != null)
        {
            // 2. ✨ 核心计算：获取修正后的伤害
            // 这里传入 ctx.SourceRuntimeCard，确保能享受到：
            // - 施法者的力量/法强 (CharacterStateManager)
            // - 卡牌自己的动态Buff (RuntimeCard)
            int finalDamage = GameManager.Instance.GetModifiedDamage(ctx.sourceRuntimeItem, value);

            // 3. 构建伤害信息包
            // (假设 DamageInfo 有对应的构造函数或字段)
            var damageInfo = new DamageInfo(finalDamage, damageType, ctx.caster)
            {
                // 如果你的 DamageInfo 有 ignoreArmor 字段，记得赋值
                ignoreArmor = this.ignoreArmor
            };

            // 4. 执行扣血
            ctx.mainTarget.TakeDamage(damageInfo);
        }
        else
        {
            string cardName = ctx.sourceRuntimeItem != null ? ctx.sourceRuntimeItem.Data.cardName : "未知卡牌";
            Debug.LogWarning($"DamageEffect 执行失败：没有目标。卡牌：{cardName}");
        }

        return animateDuration;
    }

    // 保持之前的修改，用于 UI 显示
    public override bool GetDescriptionValue(RuntimeItem item, out int baseVal, out int finalVal)
    {
        baseVal = value;
        // 这里的计算逻辑和 Execute 里完全一致，保证了“所见即所得”
        finalVal = GameManager.Instance.GetModifiedDamage(item, value);
        return true; 
    }
}