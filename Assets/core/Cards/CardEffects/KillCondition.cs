using UnityEngine;

[CreateAssetMenu(fileName = "New KillCondition", menuName = "Conditions/KillCondition")]
public class KillCondition : BaseCondition
{
    public int killThreshold = 5;
    public string storageKey = "KillCounter";

    public override bool IsMet(RuntimeItem source)
    {
        var snap = source.GetOrCreateSnapshot(storageKey);
        return snap.GetInt(storageKey) >= killThreshold;
    }

    public override void OnUnitKilled(CharacterBase owner, RuntimeItem source, CharacterBase victim)
    {
        if (victim.isEnemy == owner.isEnemy) return;

        var snap = source.GetOrCreateSnapshot(storageKey);
        int current = snap.GetInt(storageKey);

        if (current < killThreshold)
        {
            current++;
            snap.SetInt(storageKey, current);
            Debug.Log($"[{source.data.cardName}] 条件进度: {current}/{killThreshold}");
            
            // 当达成时触发一次全背包刷新
            if (current == killThreshold)
            {
                InventoryManager.Instance.RefreshInventoryPassives();
            }
        }
    }
}
