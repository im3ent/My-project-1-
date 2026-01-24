using UnityEngine;

[CreateAssetMenu(menuName = "Status Effects/Vulnerability ( TransferDamage)")]
public class TransferDamage : StatusEffect
{

    [Header("转移伤害配置")]
    [Range(0f, 1f)]
    public float defaultRatio = 0.5f; // 默认转移 50%
    public int defaultScope = 0;      // 默认随机单位

    public override EffectSnapshot GetInitialSnapshot()
    {
        var snap = base.GetInitialSnapshot();
        snap.stacks = 1;
        snap.SetFloat("Ratio", defaultRatio);
        snap.SetFloat("Scope", defaultScope);
        return snap;
    }

    public override float ModifyIncomingDamage(StatusInstance instance, float damage)
    {
        // 从快照读取数据
        var ratio = instance.snapshot.GetFloat("Ratio", defaultRatio);
        var scope = instance.snapshot.GetFloat("Scope", defaultScope);
        
        if (ratio <= 0f) return damage;
        
        // 2. 计算转移伤害
        var transferAmount = damage * ratio;
        var remainingDamage = damage - transferAmount;
        var info = new DamageInfo(transferAmount, StatsType.None, null);
        
        if (transferAmount <= 0f) return remainingDamage;
        switch ((int)scope)
        {
            case 0:
                GameManager.Instance.GetRandomUnit()?.TakeDamage(info);
                break;
            case 1:
                GameManager.Instance.GetRandomAllies()?.TakeDamage(info);
                break;
            case 2:
                GameManager.Instance.GetRandomEnemy()?.TakeDamage(info);
                break;
        }
        // 不需要每次扣层数？通常这种是永久光环，或者按回合扣
        // instance.DecreaseStack(1); 

        return remainingDamage;
    }
}
