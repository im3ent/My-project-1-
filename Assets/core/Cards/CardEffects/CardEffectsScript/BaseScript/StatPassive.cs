using UnityEngine;

[CreateAssetMenu(menuName = "Passives/Stat Bonus (Aura)")]
public class StatPassive : PassiveEffect
{
    [Header("数值配置")]
    public int attackBonus;   // 攻击力加成
    public int healthBonus;   // 血量加成
    public int spellPower;    // 法强加成

    // 重写加法层
    public override int GetSpellDamageFlat(CharacterBase owner, StatsType type)
    {
        if (type == StatsType.Physical)
        {
            return 0;
        }
        return base.GetSpellDamageFlat(owner, type);
    }


}