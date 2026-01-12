using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "NewRandomDamage", menuName = "CardEffects/Random Damage")]
public class RandomDamageEffect : CardEffect {
    
    [Header("配置")]
    public int value = 1;   // 每一发多少伤害
    public int repeatCount = 3;    // 随机打几次 (比如奥术飞弹是3次)
    public float delayBetweenHits = 0.2f; // (可选) 如果你想做连发动画，这里需要协程，目前先瞬间造成

    [Header("属性")]
    public DamageType damageType = DamageType.Magical;

    public override float Execute(EffectContext ctx) {
        // 1. ✨ 先计算最终伤害 (应用法强、Buff)
        // 注意：只算一次就行，不需要在循环里算
        int finalDamage = GameManager.Instance.GetModifiedDamage(ctx.sourceRuntimeItem, value);

        for (var i = 0; i < repeatCount; i++) {
            var randomEnemy = GameManager.Instance.GetRandomEnemy();
            if (randomEnemy == null) continue;

            // 2. ✨ 使用计算后的 finalDamage
            var info = new DamageInfo(finalDamage, damageType, ctx.caster);
            randomEnemy.TakeDamage(info);
        }
        return animateDuration;
    }
    
    public override bool GetDescriptionValue(RuntimeItem item, out int baseVal, out int finalVal)
    {
        baseVal = value;
        // 直接在这里调用 GM 的计算公式，把逻辑封装在效果内部
        finalVal = GameManager.Instance.GetModifiedDamage(item, value);
        return true; // 告诉 UI：我有数值，请填坑
    }
}