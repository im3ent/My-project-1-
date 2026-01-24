using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;

/// <summary>
/// 手牌管理器 (战斗场景)
/// 管理抽牌堆、弃牌堆、手牌
/// </summary>
public class HandManager : MonoBehaviour {
    public static HandManager Instance;

    [Header("配置")]
    public Transform handContainer;
    
    [Header("牌堆")]
    private List<RuntimeItem> _drawPile = new();      // 抽牌堆
  
    private List<RuntimeItem> _exhaustPile = new();   // 消耗堆 (不会洗回的卡，如"消耗"关键词)
    
    [Header("限制")]
    public int maxHandSize = 10; // 最多拿10张牌
    
    // =============================================
    // 🎯 事件
    // =============================================
    public event Action<RuntimeItem> OnCardDrawn;     // 抽牌时触发
    public event Action<RuntimeItem> OnCardDiscarded; // 弃牌时触发
    public event Action<RuntimeItem> OnCardExhausted; // 消耗时触发
    public event Action OnDeckReshuffled;             // 洗回弃牌堆时触发
    public event Action OnHandChanged;                // 手牌变化时触发
    
    // =============================================
    // 只读属性
    // =============================================
    public int DrawPileCount => _drawPile.Count;
    public int ExhaustPileCount => _exhaustPile.Count;
    public int HandCount => handContainer != null ? handContainer.childCount : 0;
    
    private void Awake() {
        // 单例保护（场景本地，不需要 DontDestroyOnLoad）
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start() {
        // 游戏开始时，初始化牌堆
        InitializeDeck();
    }

    // =============================================
    // 初始化
    // =============================================
    /// <summary>
    /// 初始化牌库 (战斗开始时调用)
    /// </summary>
    public void InitializeDeck() {
        _drawPile.Clear();
        _exhaustPile.Clear();
        
        _drawPile = DeckManager.Instance.GetShuffledDeckCopy();
        Debug.Log($"[HandManager] Deck initialized. DrawPile: {_drawPile.Count}");
    }

    /// <summary>
    /// 洗牌 (Fisher-Yates)
    /// </summary>
    private void ShuffleDeck() {
        for (var i = 0; i < _drawPile.Count; i++) {
            var temp = _drawPile[i];
            var randomIndex = UnityEngine.Random.Range(i, _drawPile.Count);
            _drawPile[i] = _drawPile[randomIndex];
            _drawPile[randomIndex] = temp;
        }
    }

    // =============================================
    // 🎯 抽牌
    // =============================================
    
    /// <summary>
    /// 抽一张牌
    /// </summary>
    public void DrawCard(CharacterBase caster) {
        // 如果抽牌堆空了，尝试洗回弃牌堆
        if (_drawPile.Count == 0) {
            //ShuffleReCardPileIntoDraw();
            return;
        }
        
        // 手牌满了，烧牌
        if (handContainer.childCount >= maxHandSize) {
            var burnedCard = _drawPile[0];
            _drawPile.RemoveAt(0);
            Debug.Log($"[HandManager] Hand full! Burned card: {burnedCard.data?.cardName}");
            return; 
        }
        
        // 1. 从牌堆拿第一张
        var nextCard = _drawPile[0];
        _drawPile.RemoveAt(0);

        // 2. 生成卡牌实体
        var display = CardPoolManager.Instance.SpawnCard(handContainer);
        nextCard.owner = caster;
        display.Bind(nextCard);
        
        // 3. 抽牌动画
        display.transform.localScale = Vector3.zero;
        display.transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack);
        
        // 4. 触发事件
        OnCardDrawn?.Invoke(nextCard);
        OnHandChanged?.Invoke();
        GameEvents.TriggerCardDrawn(nextCard, caster);
        GameEvents.TriggerHandChanged();
        
        Debug.Log($"[HandManager] Drew card: {nextCard.data?.cardName}. DrawPile: {_drawPile.Count}");
    }
    
    /// <summary>
    /// 抽多张牌
    /// </summary>
    public void DrawCards(CharacterBase caster, int count) {
        for (int i = 0; i < count; i++) {
            DrawCard(caster);
        }
    }
    
    /// <summary>
    /// ✨ 战斗开始时抽起始手牌（固有牌优先）
    /// 固有 (Innate) 牌一定会出现在初始手牌中
    /// </summary>
    public void DrawStartingHand(CharacterBase caster, int handSize = 1) {
        // 1. 找出所有固有牌
        var innateCards = 
            _drawPile.Where(c => c.data != null && c.data.isInnate).ToList();
        
        // 2. 从抽牌堆移除固有牌
        foreach (var card in innateCards) {
            _drawPile.Remove(card);
        }
        
        // 3. 洗剩余的普通牌
        ShuffleDeck();
        
        // 4. 把固有牌插入牌堆顶部
        _drawPile.InsertRange(0, innateCards);
        
        // 5. 抽起始手牌
        DrawCards(caster, handSize);
        
        Debug.Log($"[HandManager] Drew starting hand. Innate cards: {innateCards.Count}");
    }

    // =============================================
    // 🎯 弃牌 / 消耗
    // =============================================
    
    /// <summary>
    ///  (加入 ReCardPile，可以洗回)
    /// </summary>
    public void Recard(RuntimeItem card) {
        if (card == null) return;
        
        _drawPile.Add(card);
        ShuffleDeck();
        OnCardDiscarded?.Invoke(card);
        OnHandChanged?.Invoke();
        GameEvents.TriggerCardDiscarded(card);
        GameEvents.TriggerHandChanged();
        
    }
    
    /// <summary>
    /// 消耗卡牌 (加入 ExhaustPile，不会洗回)
    /// </summary>
    public void ExhaustCard(RuntimeItem card) {
        if (card == null) return;
        
        _exhaustPile.Add(card);
        OnCardExhausted?.Invoke(card);
        OnHandChanged?.Invoke();
        GameEvents.TriggerCardExhausted(card);
        GameEvents.TriggerHandChanged();
        
        Debug.Log($"[HandManager] Exhausted card: {card.data?.cardName}. ExhaustPile: {_exhaustPile.Count}");
    }
    
    /// <summary>
    /// 打出卡牌后的处理 (根据卡牌属性决定去弃牌堆还是消耗堆)
    /// </summary>
    public void OnCardPlayed(RuntimeItem card) {
        if (card == null) return;
        
        // 🎯 无限手牌逻辑
        if (card.data != null && card.data.returnToHand)
        {
            // 不进弃牌堆，不进消耗堆
            // 仅仅触发手牌变化事件 (刷新UI)
            OnHandChanged?.Invoke();
            return;
        }

        // 🎯 根据卡牌的 CardDefinition 判断是否消耗
        // 如果卡牌有 "exhaust" 标记，则加入消耗堆
        if (card.data != null && card.data.exhaust)
        {
            ExhaustCard(card);
        }
        else
        {
            Recard(card);
        }
    }
    
    
    // =============================================
    // 🎯 查询
    // =============================================
    
    /// <summary>
    /// 获取当前手牌列表
    /// </summary>
    public List<RuntimeItem> GetHandCards() {
        var handCards = new List<RuntimeItem>();
        if (handContainer == null) return handCards;
        
        foreach (Transform child in handContainer) {
            var display = child.GetComponent<CardDisplay>();
            if (display != null && display.runtimeItem != null) {
                handCards.Add(display.runtimeItem);
            }
        }
        return handCards;
    }
}