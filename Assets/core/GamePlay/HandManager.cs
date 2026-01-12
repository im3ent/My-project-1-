using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

public class HandManager : MonoBehaviour {
    public static HandManager Instance;

    [Header("配置")]
    public GameObject cardPrefab;
    public Transform handContainer;

    [Header("牌库数据")]
    public List<CardDefinition> startingDeck; // 配置表里的初始卡组
    private List<CardDefinition> _drawPile = new(); // 游戏运行时的抽牌堆
    
    [Header("限制")]
    public int maxHandSize = 10; // 最多拿10张牌
    private void Awake() {
        Instance = this;
    }

    private void Start() {
        // 游戏开始时，初始化牌堆
        InitializeDeck();
    }

    // --- 初始化牌库 ---
    private void InitializeDeck() {
        _drawPile.Clear();
        // 把配置的卡组复制到运行时的牌堆里
        foreach (var card in startingDeck) {
            _drawPile.Add(card);
        }
        
        // 洗牌逻辑 (Fisher-Yates Shuffle) - 高手必备细节
        ShuffleDeck();
    }

    private void ShuffleDeck() {
        for (var i = 0; i < _drawPile.Count; i++) {
            var temp = _drawPile[i];
            var randomIndex = Random.Range(i, _drawPile.Count);
            _drawPile[i] = _drawPile[randomIndex];
            _drawPile[randomIndex] = temp;
        }
    }

    // --- 核心：抽一张牌 ---
    public void DrawCard(CharacterBase caster) {
        if (_drawPile.Count == 0) {
            return;
        }
        if (handContainer.childCount >= maxHandSize) {
            // 按照炉石规则，牌库还是要减一张的（被烧了）
            _drawPile.RemoveAt(0);
            return; 
        }
        // 1. 从牌堆拿第一张
        var nextCard = _drawPile[0];
        _drawPile.RemoveAt(0);

        // 2. 生成卡牌实体
        var newCardObj = Instantiate(cardPrefab, handContainer);
        var display = newCardObj.GetComponent<CardDisplay>();
        if (display != null) {
            display.Bind(new RuntimeItem(nextCard,caster));
        }
        
        // A. 先把卡牌缩小成 0 (看不见)
        newCardObj.transform.localScale = Vector3.zero;

        // B. 用 0.5秒 的时间变大到 1
        // SetEase(Ease.OutBack) 是精华：它会让卡牌稍微放大一点点再弹回来，像果冻一样
        newCardObj.transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack);
    }
}