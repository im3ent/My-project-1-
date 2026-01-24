using UnityEngine;
using TMPro;

/// <summary>
/// 金币显示 UI (事件驱动)
/// 挂载在 PersistentUI 场景中显示金币的 Text 物体上
/// </summary>
public class MoneyDisplayUI : MonoBehaviour
{
    [Header("UI 引用")]
    public TextMeshProUGUI goldText;
    
    [Header("显示格式")]
    public string prefix = "💰 ";
    public string suffix = "";

    private void OnEnable()
    {
        // 订阅金币变化事件
        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.onGoldChanged.AddListener(OnGoldChanged);
            // 立即更新一次当前值
            OnGoldChanged(MoneyManager.Instance.CurrentGold);
        }
    }

    private void OnDisable()
    {
        // 取消订阅
        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.onGoldChanged.RemoveListener(OnGoldChanged);
        }
    }

    /// <summary>
    /// 金币变化回调
    /// </summary>
    private void OnGoldChanged(int newAmount)
    {
        if (goldText != null)
        {
            goldText.text = $"{prefix}{newAmount}{suffix}";
        }
    }
}
