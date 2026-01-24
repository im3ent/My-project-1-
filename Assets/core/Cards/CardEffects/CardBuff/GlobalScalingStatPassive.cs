using UnityEngine;

[CreateAssetMenu(fileName = "New GlobalScalingPassive", menuName = "Passives/Global Scaling Passive (Eternal Knight)")]
public class GlobalScalingStatPassive : PassiveEffect
{
    [Header("全局计数器配置")]
    public string globalKey = "EternalKnightDeaths";
    public int bonusPerStack = 1; // 每层 +1/+1
    public StatsType statType = StatsType.Physical;

    [Header("触发条件")]
    public bool incrementOnDeath = true; // 如果是永恒骑士，自己死的时候加计数
    // 如果是冰雪投球手，这里选 false，靠 OnSell 触发

    // 1. 提供属性 (无论在哪，只要持有这个被动，就生效)
    public override int GetStatsFlat(CharacterBase owner, StatsType type)
    {
        if (type == statType)
        {
            int stacks = GameManager.Instance.GetGlobalCounter(globalKey);
            // Debug.Log($"[GlobalScaling] {owner.characterName} GetStats: key={globalKey}, stacks={stacks}, bonus={stacks * bonusPerStack}");
            return stacks * bonusPerStack;
        }
        return 0;
    }

    // 2. 死亡时增加计数 (永恒骑士逻辑)
    // 注意：这个逻辑只有当单位在场上被打死时才会触发
    public override void OnUnitKilled(CharacterBase owner, RuntimeItem source, CharacterBase victim)
    {
        Debug.Log($"[GlobalScaling] OnUnitKilled called. Owner: {owner?.characterName}, Victim: {victim?.characterName}, Key: {globalKey}");
        
        if (!incrementOnDeath) 
        {
            Debug.Log("[GlobalScaling] incrementOnDeath is FALSE. Ignoring.");
            return;
        }

        // 如果死者是持有者本人 (或者是同名的队友？炉石描述是“友方永恒骑士”)
        // 简单起见：如果持有者死了，就加计数。
        // 因为每个永恒骑士都带这个被动，所以每个确定的骑士死了都会触发自己的被动
        if (victim == owner)
        {
            Debug.Log($"[GlobalScaling] Victim IS Owner. Incrementing counter {globalKey}.");
            GameManager.Instance.ModifyGlobalCounter(globalKey, 1);
        }
        else
        {
             Debug.Log($"[GlobalScaling] Victim ({victim?.characterName}) is NOT Owner ({owner?.characterName}). Ignoring.");
        }
    }
}
