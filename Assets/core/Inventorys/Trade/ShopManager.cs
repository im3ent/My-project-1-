using UnityEngine;
using System.Collections.Generic;
// using TMPro; // 如果需要刷新费用的文本

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance;

    [Header("配置")]
    public Transform shopContainer;
    public GameObject shopSlotPrefab;
    
    [Header("进货渠道")]
    public List<CardDefinition> allItemPool; // ✨ 在这里拖入所有可能的商品
    public int shopSize = 5; // 每次刷出几个？
    public int refreshCost = 50; // 刷新一次多少钱？

    private void Awake() { Instance = this; }

    private void Start()
    {
        RefreshShop(); // 开局先自动刷一次
    }

    // ✨ 核心功能：刷新商店
    public void RefreshShop()
    {
        // 1. 先清空货架
        foreach (Transform child in shopContainer) Destroy(child.gameObject);

        // 2. 随机进货
        for (int i = 0; i < shopSize; i++)
        {
            if (allItemPool.Count == 0) break;

            // 简单随机（可能会重复）
            CardDefinition randomItem = allItemPool[Random.Range(0, allItemPool.Count)];
            
            // 生成格子
            GameObject newSlot = Instantiate(shopSlotPrefab, shopContainer);
            newSlot.GetComponent<ShopSlot>().Setup(randomItem);
        }
    }

    // ✨ 按钮绑定的方法：花钱刷新
    public void OnRerollClicked()
    {
        if (MoneyManager.Instance.SpendGold(refreshCost))
        {
            RefreshShop();
            Debug.Log("刷新成功！");
        }
        else
        {
            Debug.Log("刷新没钱了！");
        }
    }
    

}