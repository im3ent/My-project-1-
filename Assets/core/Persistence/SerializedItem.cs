using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 可序列化的物品数据 (用于存档)
/// 不包含任何场景引用，可以安全地 JSON 序列化
/// </summary>
[Serializable]
public class SerializedItem
{
    // 卡牌身份 (用于反序列化时重新查找 CardDefinition)
    public string cardDefinitionName;
    
    // 战斗数值 (可能在 Run 中被各种效果修改过)
    public int manaCost;
    public int attack;
    public int health;

    // 被动倍率 (被 DoublePassiveEffect 等效果修改过)
    public float passiveMultiplier = 1f;

    // 数据快照 (用于存储条件解锁计数器等)
    // 注意：Unity 的 JsonUtility 不支持 Dictionary，需要转换为 List
    public List<SnapshotEntry> snapshots = new List<SnapshotEntry>();

    // 附加的被动 ID 列表 (不包括卡牌自带的，只包括后天获得的)
    public List<string> additionalPassiveNames = new List<string>();

    // 背包位置 (可选，用于恢复背包布局)
    public int anchorSlotIndex = -1;

    /// <summary>
    /// 从 RuntimeItem 创建可序列化版本
    /// </summary>
    public static SerializedItem FromRuntime(RuntimeItem item, int slotIndex = -1)
    {
        if (item == null || item.data == null) return null;

        var serialized = new SerializedItem
        {
            cardDefinitionName = item.data.name,
            manaCost = item.manaCost,
            attack = item.attack,
            health = item.health,
            passiveMultiplier = item.passiveMultiplier,
            anchorSlotIndex = slotIndex
        };

        // 转换快照字典
        if (item.initialSnapshots != null)
        {
            foreach (var kvp in item.initialSnapshots)
            {
                serialized.snapshots.Add(new SnapshotEntry
                {
                    key = kvp.Key,
                    snapshot = kvp.Value
                });
            }
        }

        // 记录额外被动 (非卡牌自带的)
        // 注意：这里只记录 permanentPassives 里不属于 data.passives 的
        if (item.permanentPassives != null && item.data.passives != null)
        {
            foreach (var p in item.permanentPassives)
            {
                if (p != null && !item.data.passives.Contains(p))
                {
                    serialized.additionalPassiveNames.Add(p.name);
                }
            }
        }

        return serialized;
    }

    /// <summary>
    /// 从序列化数据恢复为 RuntimeItem
    /// 需要 CardDatabase 来查找 CardDefinition
    /// </summary>
    public RuntimeItem ToRuntime(CardDatabase cardDb, PassiveDatabase passiveDb, CharacterBase owner)
    {
        var cardDef = cardDb?.GetByName(cardDefinitionName);
        if (cardDef == null)
        {
            Debug.LogWarning($"[SerializedItem] Card not found: {cardDefinitionName}");
            return null;
        }

        // 创建 RuntimeItem (会自动从 cardDef 复制被动)
        var item = new RuntimeItem(cardDef, owner);

        // 覆盖战斗数值
        item.manaCost = manaCost;
        item.attack = attack;
        item.health = health;
        item.passiveMultiplier = passiveMultiplier;

        // 恢复快照
        if (snapshots != null)
        {
            foreach (var entry in snapshots)
            {
                item.initialSnapshots[entry.key] = entry.snapshot;
            }
        }

        // 恢复额外被动
        if (additionalPassiveNames != null && passiveDb != null)
        {
            foreach (var pName in additionalPassiveNames)
            {
                var passive = passiveDb.GetByName(pName);
                if (passive != null)
                {
                    item.AddPermanentPassive(passive);
                }
            }
        }

        return item;
    }
}

/// <summary>
/// 用于序列化 Dictionary<string, EffectSnapshot>
/// </summary>
[Serializable]
public class SnapshotEntry
{
    public string key;
    public EffectSnapshot snapshot;
}
