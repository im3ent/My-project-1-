using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单局游戏的存档数据 (Roguelike Run)
/// 包含一整局游戏的所有持久化状态
/// </summary>
[Serializable]
public class RunData
{
    // =============================================
    // 玩家状态
    // =============================================
    public int playerCurrentHealth;
    public int playerMaxHealth;
    public int gold;

    // =============================================
    // 局内进度
    // =============================================
    public int currentFloor = 1;
    public int encountersCompleted = 0;
    public string currentSceneName;

    // =============================================
    // 全局计数器 (永恒骑士、投球手等)
    // Unity JsonUtility 不支持 Dictionary，转为 List
    // =============================================
    public List<CounterEntry> globalCounters = new List<CounterEntry>();

    // =============================================
    // 背包物品
    // =============================================
    public List<SerializedItem> inventoryItems = new List<SerializedItem>();

    // =============================================
    // 卡组数据
    // =============================================
    public List<SerializedItem> deckItems = new List<SerializedItem>();

    // =============================================
    // 地图数据
    // =============================================
    public MapData mapData = new MapData();

    // =============================================
    // 元数据
    // =============================================
    public string saveTime;
    public int runSeed; // 可用于复现随机事件

    /// <summary>
    /// 创建一个新的空 Run
    /// </summary>
    public static RunData CreateNew(int startHealth = 30, int startGold = 100)
    {
        return new RunData
        {
            playerCurrentHealth = startHealth,
            playerMaxHealth = startHealth,
            gold = startGold,
            currentFloor = 1,
            encountersCompleted = 0,
            runSeed = UnityEngine.Random.Range(0, int.MaxValue),
            saveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };
    }

    /// <summary>
    /// 从 GameManager 和 InventoryManager 收集当前状态
    /// </summary>
    public void GatherFromGame()
    {
        // 1. 收集玩家状态
        if (GameManager.Instance?.player != null)
        {
            var player = GameManager.Instance.player;
            playerCurrentHealth = player.currentHealth;
            playerMaxHealth = player.currentMaxHealth;
        }

        // 2. 收集金币
        if (MoneyManager.Instance != null)
        {
            gold = MoneyManager.Instance.CurrentGold;
        }

        // 3. 收集全局计数器
        globalCounters.Clear();
        if (GameManager.Instance?.globalCounters != null)
        {
            foreach (var kvp in GameManager.Instance.globalCounters)
            {
                globalCounters.Add(new CounterEntry { key = kvp.Key, value = kvp.Value });
            }
        }

        // 4. 收集背包
        inventoryItems.Clear();
        if (InventoryManager.Instance != null)
        {
            foreach (var item in InventoryManager.Instance.GetAllItems())
            {
                if (item?.runtimeItem != null)
                {
                    var serialized = SerializedItem.FromRuntime(item.runtimeItem, item.anchorSlotIndex);
                    if (serialized != null)
                    {
                        inventoryItems.Add(serialized);
                    }
                }
            }
        }

        // 5. 收集卡组
        deckItems.Clear();
        if (DeckManager.Instance != null)
        {
            deckItems = DeckManager.Instance.SerializeDeck();
        }

        // 6. 更新存档时间
        saveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    /// <summary>
    /// 将存档数据应用回游戏
    /// </summary>
    public void ApplyToGame(CardDatabase cardDb, PassiveDatabase passiveDb)
    {
        // 1. 恢复玩家状态
        if (GameManager.Instance?.player != null)
        {
            var player = GameManager.Instance.player;
            player.baseMaxHealth = playerMaxHealth;
            player.currentMaxHealth = playerMaxHealth;
            player.currentHealth = playerCurrentHealth;
        }

        // 2. 恢复金币
        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.SetGold(gold);
        }

        // 3. 恢复全局计数器
        if (GameManager.Instance != null)
        {
            GameManager.Instance.globalCounters.Clear();
            foreach (var entry in globalCounters)
            {
                GameManager.Instance.globalCounters[entry.key] = entry.value;
            }
        }

        // 4. 恢复背包 (需要 InventoryManager 提供接口)
        if (InventoryManager.Instance != null && cardDb != null)
        {
            var owner = GameManager.Instance?.player;
            foreach (var serializedItem in inventoryItems)
            {
                var runtimeItem = serializedItem.ToRuntime(cardDb, passiveDb, owner);
                if (runtimeItem != null)
                {
                    InventoryManager.Instance.AddItemFromSave(runtimeItem, serializedItem.anchorSlotIndex);
                }
            }
        }

        // 5. 恢复卡组
        if (DeckManager.Instance != null && cardDb != null)
        {
            var owner = GameManager.Instance?.player;
            DeckManager.Instance.DeserializeDeck(deckItems, cardDb, passiveDb, owner);
        }
    }
}

/// <summary>
/// 用于序列化 Dictionary<string, int>
/// </summary>
[Serializable]
public class CounterEntry
{
    public string key;
    public int value;
}
