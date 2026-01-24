using UnityEngine;

[CreateAssetMenu(menuName = "Status Effects/Vulnerability ( OneDamage)")]
public class OneDamage : StatusEffect
{
    //直接修改最终结果
    [Header("最终最大伤害")]
    public float value = 1f; 

    public override float ModifyIncomingDamage(StatusInstance instance, float damage)
    {
        return value; // 取较小值
    }
}