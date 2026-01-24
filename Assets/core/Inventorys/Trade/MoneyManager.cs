using UnityEngine;
using UnityEngine.Events; // 用于UI更新事件
using TMPro; // 如果你用TextMeshPro

public class MoneyManager : MonoBehaviour
{
    public static MoneyManager Instance;
    
    public int currentGold = 1000;
    public TextMeshProUGUI goldText; // 可选：拖入显示金币的UI Text（用于快速测试）

    // 🎯 金币变化事件 - UI 订阅此事件即可
    public UnityEvent<int> onGoldChanged = new UnityEvent<int>();

    private void Awake()
    {
        // 单例模式 + DontDestroyOnLoad
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        onGoldChanged?.Invoke(currentGold);
        UpdateUI();
    }

    public bool SpendGold(int amount)
    {
        if (currentGold >= amount)
        {
            currentGold -= amount;
            onGoldChanged?.Invoke(currentGold);
            UpdateUI();
            return true; // 支付成功
        }
        return false; // 余额不足
    }

    public void AddGold(int amount)
    {
        currentGold += amount;
        onGoldChanged?.Invoke(currentGold);
        UpdateUI();
    }

    /// <summary>
    /// 直接设置金币数量 (用于存档恢复)
    /// </summary>
    public void SetGold(int amount)
    {
        currentGold = amount;
        onGoldChanged?.Invoke(currentGold);
        UpdateUI();
    }

    /// <summary>
    /// 当前金币只读属性
    /// </summary>
    public int CurrentGold => currentGold;

    void UpdateUI()
    {
        if(goldText != null) goldText.text = currentGold.ToString();
    }
}