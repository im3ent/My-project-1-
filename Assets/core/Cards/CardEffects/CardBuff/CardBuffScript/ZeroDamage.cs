using UnityEngine;

[CreateAssetMenu(menuName = "Status Effects/Vulnerability ( ZeroDamage)")]
public class ZeroDamage : StatusEffect
{
    //直接修改最终结果
    [Header("圣盾")]
    public float value = 1f; 

    public override float ModifyIncomingDamage(StatusInstance instance, float damage)
    {
        return 0; // 取较小值
    }
}