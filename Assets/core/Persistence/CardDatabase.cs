using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 卡牌数据库 (用于通过名称查找 CardDefinition)
/// 在加载存档时需要用到
/// </summary>
[CreateAssetMenu(fileName = "CardDatabase", menuName = "Database/Card Database")]
public class CardDatabase : ScriptableObject
{
    [Header("所有卡牌定义")]
    public List<CardDefinition> allCards = new List<CardDefinition>();

    // 运行时缓存 (name -> CardDefinition)
    private Dictionary<string, CardDefinition> _cache;

    /// <summary>
    /// 通过名称查找卡牌
    /// </summary>
    public CardDefinition GetByName(string cardName)
    {
        if (string.IsNullOrEmpty(cardName)) return null;

        // 懒加载缓存
        if (_cache == null)
        {
            BuildCache();
        }

        return _cache.TryGetValue(cardName, out var card) ? card : null;
    }

    /// <summary>
    /// 构建缓存
    /// </summary>
    private void BuildCache()
    {
        _cache = new Dictionary<string, CardDefinition>();
        foreach (var card in allCards)
        {
            if (card != null && !string.IsNullOrEmpty(card.name))
            {
                if (!_cache.ContainsKey(card.name))
                {
                    _cache.Add(card.name, card);
                }
                else
                {
                    Debug.LogWarning($"[CardDatabase] Duplicate card name: {card.name}");
                }
            }
        }
    }

    /// <summary>
    /// 编辑器下刷新缓存
    /// </summary>
    private void OnValidate()
    {
        _cache = null; // 强制下次访问时重建
    }

    // --- 👑 编辑器自动化工具 ---
#if UNITY_EDITOR
    [ContextMenu("自动扫描并填充所有卡牌")]
    public void AutoFillCards()
    {
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:CardDefinition");
        allCards.Clear();
        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var card = UnityEditor.AssetDatabase.LoadAssetAtPath<CardDefinition>(path);
            if (card != null) allCards.Add(card);
        }
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"[CardDatabase] Auto-filled {allCards.Count} cards.");
    }
#endif
}
