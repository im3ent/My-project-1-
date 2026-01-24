using UnityEngine;
[CreateAssetMenu(menuName = "PassiveEffect/ ItemGrantGuardPassive")]
public class ItemGrantGuardPassive : PassiveEffect
{
    public StatusEffect  guardBuff;

    public override EffectSnapshot GetInitialSnapshot()
    {
        if (guardBuff != null)
        {
            return guardBuff.GetInitialSnapshot();
        }
        return base.GetInitialSnapshot();
    }

    public override void OnTurnStart(CharacterBase owner, RuntimeItem sourceItem)
    {
        // 1. 动态计算数值 (比如 0.4f)
        var snap = sourceItem.Snapshot;
        if (snap == null) return;

        // "Ratio" 需要在 TransferDamage Buff 的 GetInitialSnapshot 里写入
        float ratio = snap.GetFloat("Ratio", 0f); 
        
        var calculatedRatio = Mathf.Min(1.0f, ratio);
        snap.SetFloat("Ratio", calculatedRatio);

        // 2. 获取状态管理器
        var stateManager = owner.stateManager;
        if (stateManager == null) return;
        // 3. ✨ 调用修改后的 ApplyStatus，并接收返回值
        stateManager.ApplyStatus(guardBuff, owner, snap);

        Debug.Log($"[OnTurnStart ItemGrantGuardPassive 同步完成]");
    }

        
    
}
