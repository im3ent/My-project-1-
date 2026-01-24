using System.Collections.Generic;
using UnityEngine;

public class CardPoolManager : MonoBehaviour
{
    public static CardPoolManager Instance; // 单例方便调用

    [Header("设置")]
    public CardDisplay cardPrefab; // ✨ 直接引用脚本类型，别用 GameObject
    public Transform poolContainer; // 闲置卡牌存放的父物体 (隐藏在屏幕外)
    public int initialSize = 50;    // 初始生成多少个

    // 真正的“池子”
    private Queue<CardDisplay> _pool = new Queue<CardDisplay>();

    void Awake()
    {
        // 单例保护（场景本地，不需要 DontDestroyOnLoad）
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        InitializePool();
    }

    // 1. 初始化：造一堆空盘子
    void InitializePool()
    {
        for (int i = 0; i < initialSize; i++)
        {
            CreateNewCard();
        }
    }

    // 辅助：造新卡并入池
    CardDisplay CreateNewCard()
    {
        CardDisplay card = Instantiate(cardPrefab, poolContainer);
        card.ResetCard(); // 刚出生先洗一遍
        _pool.Enqueue(card);
        return card;
    }

    // 2. 取卡：从柜子里拿出来
    public CardDisplay SpawnCard(Transform parent)
    {
        CardDisplay card;

        // 如果池子空了，就临时造一个新的 (扩容)
        if (_pool.Count == 0)
        {
            card = Instantiate(cardPrefab, parent);
        }
        else
        {
            card = _pool.Dequeue();
        }

        // 设置父物体 (比如放到手牌区)
        card.transform.SetParent(parent, false); 
        // 此时它还是隐藏的，由调用者负责 Init
        return card;
    }

    // 3. 还卡：放回柜子里
    public void RecycleCard(CardDisplay card)
    {
        // 先洗干净
        card.ResetCard();
        // 移回池子容器 (防止干扰 UI 布局)
        card.transform.SetParent(poolContainer, false);
        // 入队
        _pool.Enqueue(card);
    }
}