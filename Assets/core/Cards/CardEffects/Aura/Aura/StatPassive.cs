using UnityEngine;

[CreateAssetMenu(menuName = "Passives/Stat Bonus (Aura)")]
public class StatPassive : PassiveEffect
{
    [Header("数值配置")]
    public int value;   // 加成

    // 重写加法层
    public override int GetStatsFlat(CharacterBase owner, StatsType type)
    {
        if (type == StatsType.Physical)
        {
            return value;
        }

        return 0;
    }


}