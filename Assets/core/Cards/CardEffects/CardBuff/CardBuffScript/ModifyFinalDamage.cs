using UnityEngine;

[CreateAssetMenu(menuName = "Status Effects/Vulnerability ( Damage Cap)")]
public class ModifyFinalDamage : StatusEffect
{
    //直接修改最终结果
    [Header("最大伤害生命%")]
    public float bonus = .1f; 

    public override float ModifyIncomingDamage(StatusInstance instance, float damage)
    {
        int cap = Mathf.FloorToInt(instance.Owner.ownerCharacter.currentHealth * bonus);
        return Mathf.Min(damage, cap); // 取较小值
    }
}



    

