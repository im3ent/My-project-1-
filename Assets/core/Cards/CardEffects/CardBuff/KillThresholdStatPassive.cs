using UnityEngine;

[CreateAssetMenu(fileName = "New KillThresholdStatPassive", menuName = "CardEffects/Passive/KillThresholdStatPassive")]
public class KillThresholdStatPassive : PassiveEffect
{
    [Header("解锁条件")]
    public int killThreshold = 10;
    
    [Header("解锁后的属性加成")]
    public StatsType targetStat = StatsType.Physical;
    public int flatBonus;
    public float increasedBonus; // 0.1 表示 +10%

    // 1. 击杀监听 (复用同一套计数器 "KillCounter")
    public override void OnUnitKilled(CharacterBase owner, RuntimeItem source, CharacterBase victim)
    {
        if (victim.isEnemy == owner.isEnemy) return;

        var snap = source.GetOrCreateSnapshot("KillCounter");
        int currentKills = snap.GetInt("KillCounter");
        
        if (currentKills < killThreshold)
        {
            currentKills++;
            snap.SetInt("KillCounter", currentKills);
            Debug.Log($"[{source.data.cardName}] 技能进度: {currentKills}/{killThreshold}");
            
            // 关键：当刚好达到阈值时，强制刷新一次属性
            if (currentKills == killThreshold)
            {
                InventoryManager.Instance.RefreshInventoryPassives();
            }
        }
    }

    // 2. 条件判定
    public override bool IsConditionMet(RuntimeItem source)
    {
        var snap = source.GetOrCreateSnapshot("KillCounter");
        return snap.GetInt("KillCounter") >= killThreshold;
    }

    // 3. 属性提供 (只有 IsConditionMet 为 true 时，InventoryManager 才会计算这些值)
    public override int GetStatsFlat(CharacterBase owner, StatsType type)
    {
        if (type == targetStat) return flatBonus;
        return 0;
    }

    public override float GetStatsIncreased(CharacterBase owner, StatsType type)
    {
        if (type == targetStat) return increasedBonus;
        return 0;
    }
}
