using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopItem : MonoBehaviour
{
    public CardDefinition itemToSell; // 这个格子卖什么？
    
    [Header("UI 组件")]
    public Image iconImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI priceText;
    public Button buyButton;

    private void Start()
    {
        if (itemToSell != null) Setup(itemToSell);
        buyButton.onClick.AddListener(OnBuyClicked);
    }

    public void Setup(CardDefinition data)
    {
        itemToSell = data;

        
        // if(iconImage != null) iconImage.sprite = data.icon;
        if(nameText != null) nameText.text = data.cardName;
        if(priceText != null) priceText.text = $"${data.price}";
        

        // 普通商品，显示原价
        if(priceText != null) priceText.text = $"${data.price}";
        
    }

    private void OnBuyClicked()
    {
        if (itemToSell == null) return;
        
        // 1. 检查钱够不够
        if (MoneyManager.Instance.currentGold >= itemToSell.price)
        {
            // 2. 尝试往背包里加 (利用你之前写的 AddItem 返回的 bool !)
            // 注意：我们需要把它包装成 RuntimeCard
            var newCard = new RuntimeCard(itemToSell, null);

            var addSuccess = InventoryManager.Instance.AddItem(newCard);

            if (addSuccess)
            {
                // 3. 加进去成功了，才扣钱
                MoneyManager.Instance.SpendGold(itemToSell.price);
                Debug.Log("购买成功！");

            }
            else
            {
                Debug.Log("背包满啦，买不了！");
                // 这里可以弹个提示窗
            }
        }
        else
        {
            Debug.Log("穷鬼，买不起！");
        }
    }
}