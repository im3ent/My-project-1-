using UnityEngine;

[CreateAssetMenu(menuName = "CardEffects/New Aura Effect")]
public class AuraEffect : ScriptableObject
{
    [Header("光环数值")]
    public int attackBuff = 0; // 加多少攻
    public int healthBuff = 0; // 加多少血上限

    [Header("作用范围")]
    public AuraTargetScope targetScope; 

    // 判断这个光环是否应该套在 target 身上
    public bool IsApplicable(CharacterBase source, CharacterBase target)
    {
        // 1. 自己通常不享受自己的光环 (例如：暴风城勇士)
        if (source == target) return false;

        return targetScope switch
        {
            AuraTargetScope.AllFriendlyMinions =>
                // 只有同阵营才生效
                source.isEnemy == target.isEnemy,
            AuraTargetScope.AllEnemyMinions =>
                // 只有敌对阵营才生效
                source.isEnemy != target.isEnemy,
            AuraTargetScope.AllMinions =>
                // 全场生效
                true,
            _ => false
        };
    }
}

// 定义枚举：光环能罩着谁
public enum AuraTargetScope
{
    AllFriendlyMinions, // 全体友军
    AllEnemyMinions,    // 全体敌军
    AllMinions          // 全场随从
}