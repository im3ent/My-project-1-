using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// 卡组面板 UI
/// 挂载在 PersistentUI 场景中，用于显示当前卡组内容
/// </summary>
public class DeckPanelUI : MonoBehaviour
{
    [Header("引用")]
    public Transform cardListContainer; // 卡牌条目的父物体
    public GameObject cardEntryPrefab;  // 单条卡牌的预制体 (Text 或 带图标的)
    public TextMeshProUGUI deckCountText; // 显示"卡组: 20张"

    private void OnEnable()
    {
        RefreshUI();
        
        // 订阅卡组变化事件
        if (DeckManager.Instance != null)
        {
            DeckManager.Instance.OnDeckChanged += RefreshUI;
        }
    }

    private void OnDisable()
    {
        // 取消订阅
        if (DeckManager.Instance != null)
        {
            DeckManager.Instance.OnDeckChanged -= RefreshUI;
        }
    }

    /// <summary>
    /// 刷新卡组显示
    /// </summary>
    public void RefreshUI()
    {
        // 1. 清空旧条目
        if (cardListContainer != null)
        {
            foreach (Transform child in cardListContainer)
            {
                Destroy(child.gameObject);
            }
        }

        // 2. 获取当前卡组
        if (DeckManager.Instance == null) return;
        var deck = DeckManager.Instance.GetDeck();

        // 3. 更新计数
        if (deckCountText != null)
        {
            deckCountText.text = $"卡组: {deck.Count} 张";
        }

        // 4. 生成卡牌条目
        if (cardListContainer != null && cardEntryPrefab != null)
        {
            // 按卡牌名称分组统计数量
            Dictionary<string, int> cardCounts = new Dictionary<string, int>();
            foreach (var item in deck)
            {
                if (item?.data == null) continue;
                string cardName = item.data.name;
                if (!cardCounts.ContainsKey(cardName))
                    cardCounts[cardName] = 0;
                cardCounts[cardName]++;
            }

            // 生成 UI 条目
            foreach (var kvp in cardCounts)
            {
                var entry = Instantiate(cardEntryPrefab, cardListContainer);
                var text = entry.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    text.text = kvp.Value > 1 ? $"{kvp.Key} x{kvp.Value}" : kvp.Key;
                }
            }
        }
    }
}
