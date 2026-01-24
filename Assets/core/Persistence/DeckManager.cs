using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 卡组管理器 (Singleton)
/// 负责管理玩家当前 Run 的卡组
/// 存储 RuntimeItem 以保留卡牌的升级/强化状态
/// </summary>
public class DeckManager : MonoBehaviour
{
    public static DeckManager Instance { get; private set; }

    [Header("当前卡组 (运行时)")]
    [SerializeField] private List<RuntimeItem> deckCards = new List<RuntimeItem>();

    [Header("初始卡组 (新游戏时使用)")]
    public List<CardDefinition> starterDeck;

    // 事件：卡组变化时触发 (用于 UI 刷新)
    public event Action OnDeckChanged;
    
    // 🎯 事件：单张卡牌被修改时触发 (升级、添加词缀等)
    public event Action<RuntimeItem> OnCardModified;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    // =============================================
    // 🎯 卡牌修改 API
    // =============================================
    
    /// <summary>
    /// 升级卡牌 (增加等级，可自定义升级效果)
    /// </summary>
    public void UpgradeCard(RuntimeItem item, int costReduction = 1)
    {
        if (item == null) return;
        
        item.Upgrade(); // level++
        item.manaCost = Mathf.Max(0, item.manaCost - costReduction);
        
        NotifyCardModified(item);
        Debug.Log($"[DeckManager] Upgraded card: {item.data?.cardName} to level {item.level}");
    }
    
    /// <summary>
    /// 修改卡牌的 Snapshot 数值 (用于强化 Buff 层数/伤害等)
    /// </summary>
    public void ModifyCardSnapshot(RuntimeItem item, string snapshotKey, string valueKey, float delta)
    {
        if (item == null) return;
        
        var snapshot = item.GetOrCreateSnapshot(snapshotKey);
        float current = snapshot.GetFloat(valueKey);
        snapshot.SetFloat(valueKey, current + delta);
        
        NotifyCardModified(item);
        Debug.Log($"[DeckManager] Modified {item.data?.cardName} [{snapshotKey}].{valueKey} += {delta}");
    }
    
    /// <summary>
    /// 修改卡牌 Snapshot 的层数
    /// </summary>
    public void ModifyCardSnapshotStacks(RuntimeItem item, string snapshotKey, int delta)
    {
        if (item == null) return;
        
        var snapshot = item.GetOrCreateSnapshot(snapshotKey);
        snapshot.stacks = Mathf.Max(0, snapshot.stacks + delta);
        
        NotifyCardModified(item);
        Debug.Log($"[DeckManager] Modified {item.data?.cardName} [{snapshotKey}].stacks += {delta}");
    }
    
    /// <summary>
    /// 通知卡牌被修改 (触发事件)
    /// </summary>
    public void NotifyCardModified(RuntimeItem item)
    {
        OnCardModified?.Invoke(item);
        OnDeckChanged?.Invoke();
        GameEvents.TriggerCardModified(item);
        GameEvents.TriggerDeckChanged();
    }

    // =============================================
    // 公共 API
    // =============================================

    /// <summary>
    /// 获取当前卡组副本 (只读)
    /// </summary>
    public IReadOnlyList<RuntimeItem> GetDeck() => deckCards;

    /// <summary>
    /// 获取卡组数量
    /// </summary>
    public int DeckCount => deckCards.Count;

    /// <summary>
    /// 初始化为初始卡组 (新游戏时调用)
    /// </summary>
    public void ResetToStarterDeck(CharacterBase owner = null)
    {
        deckCards.Clear();
        if (starterDeck != null)
        {
            foreach (var cardDef in starterDeck)
            {
                if (cardDef != null)
                {
                    // 从 CardDefinition 创建 RuntimeItem
                    var runtimeItem = new RuntimeItem(cardDef, owner);
                    deckCards.Add(runtimeItem);
                }
            }
        }
        OnDeckChanged?.Invoke();
        Debug.Log($"[DeckManager] Reset to starter deck. Count: {deckCards.Count}");
    }

    /// <summary>
    /// 添加 RuntimeItem 到卡组
    /// </summary>
    public void AddCard(RuntimeItem item)
    {
        if (item == null) return;
        deckCards.Add(item);
        OnDeckChanged?.Invoke();
        Debug.Log($"[DeckManager] Added card: {item.data?.name}. Total: {deckCards.Count}");
    }

    /// <summary>
    /// 快捷方法：从 CardDefinition 创建并添加
    /// </summary>
    public RuntimeItem AddCard(CardDefinition cardDef, CharacterBase owner = null)
    {
        if (cardDef == null) return null;
        var item = new RuntimeItem(cardDef, owner);
        AddCard(item);
        return item;
    }

    /// <summary>
    /// 从卡组移除一张卡牌
    /// </summary>
    public bool RemoveCard(RuntimeItem item)
    {
        if (item == null) return false;
        bool removed = deckCards.Remove(item);
        if (removed)
        {
            OnDeckChanged?.Invoke();
            Debug.Log($"[DeckManager] Removed card: {item.data?.name}. Total: {deckCards.Count}");
        }
        return removed;
    }

    /// <summary>
    /// 移除指定索引的卡牌
    /// </summary>
    public RuntimeItem RemoveCardAt(int index)
    {
        if (index < 0 || index >= deckCards.Count) return null;
        var card = deckCards[index];
        deckCards.RemoveAt(index);
        OnDeckChanged?.Invoke();
        Debug.Log($"[DeckManager] Removed card at {index}: {card?.data?.name}. Total: {deckCards.Count}");
        return card;
    }

    /// <summary>
    /// 获取指定索引的卡牌
    /// </summary>
    public RuntimeItem GetCardAt(int index)
    {
        if (index < 0 || index >= deckCards.Count) return null;
        return deckCards[index];
    }

    // =============================================
    // 存档集成
    // =============================================

    /// <summary>
    /// 序列化卡组 (用于存档)
    /// </summary>
    public List<SerializedItem> SerializeDeck()
    {
        var serializedList = new List<SerializedItem>();
        foreach (var item in deckCards)
        {
            if (item != null)
            {
                var serialized = SerializedItem.FromRuntime(item);
                if (serialized != null) serializedList.Add(serialized);
            }
        }
        return serializedList;
    }

    /// <summary>
    /// 从序列化数据反序列化 (用于读档)
    /// </summary>
    public void DeserializeDeck(List<SerializedItem> serializedItems, CardDatabase cardDb, PassiveDatabase passiveDb, CharacterBase owner = null)
    {
        deckCards.Clear();
        if (serializedItems == null || cardDb == null) return;

        foreach (var serialized in serializedItems)
        {
            var item = serialized.ToRuntime(cardDb, passiveDb, owner);
            if (item != null)
            {
                deckCards.Add(item);
            }
        }
        OnDeckChanged?.Invoke();
        Debug.Log($"[DeckManager] Deserialized deck. Count: {deckCards.Count}");
    }

    /// <summary>
    /// 提供给 HandManager 使用的牌组副本 (洗牌后)
    /// </summary>
    public List<RuntimeItem> GetShuffledDeckCopy()
    {
        var copy = new List<RuntimeItem>(deckCards);
        // Fisher-Yates Shuffle
        for (int i = 0; i < copy.Count; i++)
        {
            var temp = copy[i];
            var randomIndex = UnityEngine.Random.Range(i, copy.Count);
            copy[i] = copy[randomIndex];
            copy[randomIndex] = temp;
        }
        return copy;
    }

    /// <summary>
    /// 获取卡组中所有 CardDefinition (用于 UI 显示)
    /// </summary>
    public List<CardDefinition> GetDeckDefinitions()
    {
        var defs = new List<CardDefinition>();
        foreach (var item in deckCards)
        {
            if (item?.data != null) defs.Add(item.data);
        }
        return defs;
    }
}
