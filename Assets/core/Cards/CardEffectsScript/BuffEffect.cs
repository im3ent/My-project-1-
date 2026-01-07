using UnityEngine;

[CreateAssetMenu(menuName = "Effects/Buff")]
public class BuffEffect : CardEffect 
{
    public int atkBuff;
    public int healthBuff;
    
    public EffectTargetType targetType; 

    public override float Execute(EffectContext ctx) 
    {
        // 1. 根据配置决定 Buff 谁
        var finalTarget = targetType switch
        {
            EffectTargetType.ManualTarget =>
                // Buff 玩家鼠标指着的那个怪 (比如：王者祝福)
                ctx.mainTarget,
            EffectTargetType.LastCreatedUnit =>
                // Buff 刚才召唤出来的那个怪 (比如：召唤并获得嘲讽)
                ctx.LastCreatedUnit,
            EffectTargetType.Self =>
                // Buff 施法者自己 (比如：英雄加攻)
                ctx.caster,
            _ => null
        };

        // 2. 执行 Buff
        if (finalTarget != null) 
        {
            finalTarget.ApplyBuff(atkBuff, healthBuff);
        }
        else 
        {
            Debug.LogWarning("Buff 效果找不到目标！");
        }

        return animateDuration;
    }
}

// 定义枚举
public enum EffectTargetType {
    ManualTarget,   // 鼠标选的
    LastCreatedUnit,// 刚才造的
    Self            // 自己
}