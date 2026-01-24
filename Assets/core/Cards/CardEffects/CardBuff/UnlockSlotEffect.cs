using UnityEngine;

[CreateAssetMenu(menuName = "Passives/Unlock Slot Effect")]
public class UnlockSlotEffect : PassiveEffect
{
    // 这是一个标记类
    // 只要物品持有这个被动，InventoryManager 就会根据 Scope 解锁对应的格子
    
    // 你可以在这里加个 "UnlockCount" 比如解锁2层？暂时默认解锁1层

    // ✨ 重写：我是解锁器，我要解锁格子
    public override System.Collections.Generic.IEnumerable<int> GetUnlockedSlotIndices(InventoryItem sourceItem)
    {
        if (InventoryManager.Instance == null) return base.GetUnlockedSlotIndices(sourceItem);
        
        return InventoryManager.Instance.GetTargetIndicesByScope(sourceItem, this.scope);
    }
}
