using UnityEngine;
using UnityEngine.Events; // 用于UI更新事件
using TMPro; // 如果你用TextMeshPro

public class MoneyManager : MonoBehaviour
{
    public static MoneyManager Instance;
    
    public int currentGold = 1000;
    public TextMeshProUGUI goldText; // 拖入显示金币的UI Text

    private void Awake()
    {
        Instance = this;
        UpdateUI();
    }

    public bool SpendGold(int amount)
    {
        if (currentGold >= amount)
        {
            currentGold -= amount;
            UpdateUI();
            return true; // 支付成功
        }
        return false; // 余额不足
    }

    public void AddGold(int amount)
    {
        currentGold += amount;
        UpdateUI();
    }

    void UpdateUI()
    {
        if(goldText != null) goldText.text = currentGold.ToString();
    }
}