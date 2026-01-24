using UnityEngine;

/// <summary>
/// 关键词定义 - 每个关键词是一个 ScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "New Keyword", menuName = "Data/Tooltip/Keyword")]
public class KeywordData : ScriptableObject
{
    [Header("基础信息")]
    public string keywordName; // 关键字名称：如"战吼"
    
    [TextArea] 
    public string description; // 解释：如"从手牌打出时触发..."
    
    [Header("视觉效果")]
    public Color themeColor = Color.yellow; // 每个关键字一个颜色
    public Sprite icon; // 关键字左边的小图标
    public bool isBold = true; // 是否加粗
    
    /// <summary>
    /// 获取格式化后的关键词文本（带颜色和样式）
    /// </summary>
    public string GetFormattedText()
    {
        string colorHex = ColorUtility.ToHtmlStringRGB(themeColor);
        string text = keywordName;
        
        if (isBold)
        {
            text = $"<b>{text}</b>";
        }
        
        return $"<color=#{colorHex}>{text}</color>";
    }
    
    /// <summary>
    /// 获取用于 Tooltip 的完整信息
    /// </summary>
    public string GetTooltipContent()
    {
        return $"<b>{keywordName}</b>\n{description}";
    }
}