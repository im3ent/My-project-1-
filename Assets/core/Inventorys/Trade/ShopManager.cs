using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.Serialization;
using UnityEngine.UI;

// using TMPro; // 如果需要刷新费用的文本

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance;
    public Image globalDragGhost; // 这是一个放在 Canvas 顶层、默认隐藏的物体
    [Header("配置")]
 
    public GameObject shopItemPrefab; // 商品图标预制体
    
    [Header("UI 引用")]
    // ✨ 关键：直接拖入场景里的 3 个或 4 个空物体（作为货架）
    // 比如 ShopCanvas/Container/Slot1, Slot2, Slot3
    public Transform[] shopSlots;
    public PassiveDatabase passiveDb;
    [Header("进货渠道")]
    public List<CardDefinition> allItem; // ✨ 在这里拖入所有可能的商品
    public int shopSize = 5; // 每次刷出几个？
    public int refreshCost = 50; // 刷新一次多少钱？
    public int copiesPerCard = 2;     // ✨ 新增：
    
    [Header("刷新设置")]
    public int maxFreeRerolls = 3;    // 每局/每天最大免费次数
    private int currentFreeRerolls;   // 当前剩余免费次数
    public TextMeshProUGUI rerollCostText;
    // 当前的卡池
    [SerializeField] private List<CardDefinition> currentPool = new();
    private void Awake() { Instance = this; }

    private void Start()
    {
        currentFreeRerolls = maxFreeRerolls;
        InitializePool();
        OnRerollButtonClicked(); // 开局先自动刷一次
    }

    // --- 1. 初始化卡池 ---
    private void InitializePool()
    {
        currentPool.Clear();
        // 遍历所有卡牌定义
        foreach (var card in allItem)
        {
            // 每张卡放入指定数量 (比如2张)
            for (int i = 0; i < copiesPerCard; i++)
            {
                currentPool.Add(card);
            }
        }
        Debug.Log($"卡池初始化完成，共有 {currentPool.Count} 张卡牌。");
    }

    // --- 2. 刷新商店 (核心修改) ---
    private void RerollShop()
    {   
        // 1. 回收旧货 (遍历所有货架)
        foreach (var slot in shopSlots)
        {
            if (slot.childCount <= 0) continue;
            var item = slot.GetChild(0).GetComponent<ShopItem>();
            if (item != null) ReturnCardToPool(item.itemToSell);
            // 销毁旧 UI
            Destroy(slot.GetChild(0).gameObject);
        }

        // 2. 进新货
        foreach (var slot in shopSlots)
        {
            var card = DrawCardFromPool();
            if (card == null) break;

            var newItemObj = Instantiate(shopItemPrefab, slot);
            newItemObj.transform.localPosition = Vector3.zero;

            var shopItemScript = newItemObj.GetComponent<ShopItem>(); 
            if(shopItemScript != null)
            {
                shopItemScript.Setup(card);
            }
        }
       

    }
    public void OnRerollButtonClicked()
    {
        if (currentFreeRerolls > 0)
        {
            currentFreeRerolls--;
            Debug.Log($"使用免费刷新，剩余: {currentFreeRerolls}");
            RerollShop();
            UpdateShopUI(currentFreeRerolls > 0 ? 0.ToString() : refreshCost.ToString());
        }
        else
        {
            // 尝试付费刷新
            if (MoneyManager.Instance.SpendGold(refreshCost))
            {
                Debug.Log("付费刷新成功");
                RerollShop();
                
            }
            else
            {
                
                Debug.Log("钱不够，且没有免费次数了！");
            }
        }
    
        // 更新 UI 显示（如果有次数文本）
        //UpdateShopUI();
        
    }
    
    // --- 辅助：从池子里拿一张卡 ---
    private CardDefinition DrawCardFromPool()
    {
        if (currentPool.Count == 0) return null;

        // 随机选一个索引
        int randomIndex = Random.Range(0, currentPool.Count);
        CardDefinition selected = currentPool[randomIndex];

        // ✨ 关键：从池子里移除它！
        currentPool.RemoveAt(randomIndex);

        return selected;
    }

    
    public void ReturnCardToPool(CardDefinition card)
    {
        if (card == null) return;
        currentPool.Add(card);
        Debug.Log($"物品 {card.cardName} 已退回卡池。当前卡池总量: {currentPool.Count}");
    }

    private void UpdateShopUI(string rerollCost)
    {
        rerollCostText.text = rerollCost;
    }
    public RuntimeItem CreateCardWithRandomAffix(CardDefinition def)
    {
        var newItem = new RuntimeItem(def, null);

        // 假设你有 30% 几率获得一个随机词条
        if (Random.value < 0.5f)
        {
            // 从你的被动池子里随机选一个
            var randomPassive = passiveDb.GetRandomPassive();
        
            // ✨ 加到永久列表里
            newItem.AddPermanentPassive(randomPassive);
        }

        return newItem;
    }

}