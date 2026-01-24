using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopItem : MonoBehaviour
{
    public CardDefinition itemToSell; // 这个格子卖什么？
    public RuntimeItem runtimeToSell;

    [Header("UI 组件")]
    public Image iconImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI priceText;
    public Button buyButton;

    private void Start()
    {
        if (itemToSell != null) Setup(itemToSell);
        
        // 🎯 防止重复添加监听器（可能 Inspector 中也绑定了）
        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(OnBuyClicked);
    }

    public void Setup(CardDefinition data)
    {
        itemToSell = data;
        runtimeToSell = ShopManager.Instance.CreateCardWithRandomAffix(data);
        iconImage.sprite = data.artwork;
        nameText.text = data.cardName;
         priceText.text = $"${data.price}";
         
        // ✨ 修复：还原商品大小 (按照 1格=100 的标准)
        iconImage.preserveAspect = true;
        // 注意：如果不希望商店里的物品太大挡住别人，可以整体缩放，比如 * 0.8f
        // 但用户说 "还原大小"，应该是指匹配背包里的显示逻辑
        iconImage.rectTransform.sizeDelta = new Vector2(data.width * 100, data.height * 100);
    }

    public void OnBuyClicked()
    {
        if (itemToSell == null) return;
        
        // 1. 检查钱够不够
        if (MoneyManager.Instance.currentGold >= itemToSell.price)
        {
            var addSuccess = InventoryManager.Instance.AddItem(runtimeToSell);

            if (addSuccess)
            {
                // 3. 加进去成功了，才扣钱
                MoneyManager.Instance.SpendGold(itemToSell.price);
                Destroy(gameObject); // ✨ 修复：买完就销毁 (扣库存)

            }
            else
            {
                // 这里可以弹个提示窗
            }
        }
        else
        {
            // Money not enough feedback
        }
    }


}