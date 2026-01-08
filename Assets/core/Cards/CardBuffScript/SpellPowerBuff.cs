using UnityEngine;
[CreateAssetMenu(menuName = "Buffs/SpellPowerBuff")]
public class SpellPowerBuff : StatusEffect
{
    public int amountPerStack = 1;
    private void OnEnable() { id = "SpellPowerBuff"; } // 确保 ID 唯一

    public override int GetSpellDamageAdditive(StatusInstance instance)
    {
        // 最终增加量 = 单层数值 * 当前层数
        // 例子：Buff配置是+1，玩家身上叠了 5 层，结果就是 +5 法强
        return amountPerStack * instance.Stacks;
    }

}
