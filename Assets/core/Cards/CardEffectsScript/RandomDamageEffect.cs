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
        // 注意：这里的 target 参数通常是 null，因为这种卡不需要拖拽瞄准
        
        for (var i = 0; i < repeatCount; i++) {
            // 1. 每一次攻击前，都重新找一个活着的随机敌人 (这样如果第一发把怪打死了，第二发就不会鞭尸，或者转向其他怪)
            var randomEnemy = GameManager.Instance.GetRandomEnemy();

            if (randomEnemy == null) continue;
            // 2. 打包伤害
            var info = new DamageInfo(value, damageType, ctx.caster);
                
            // 3. 造成伤害
            randomEnemy.TakeDamage(info);
        }
        return animateDuration;
    }
    
    public override bool GetDescriptionValue(CardDefinition card, CharacterBase owner, out int baseVal, out int finalVal)
    {
        baseVal = value;
        // 直接在这里调用 GM 的计算公式，把逻辑封装在效果内部
        finalVal = GameManager.Instance.GetModifiedDamage(card, value);
        return true; // 告诉 UI：我有数值，请填坑
    }
}