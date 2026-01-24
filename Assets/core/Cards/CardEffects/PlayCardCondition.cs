using UnityEngine;

[CreateAssetMenu(fileName = "New PlayCardCondition", menuName = "Conditions/PlayCardCondition")]
public class PlayCardCondition : BaseCondition
{
    [Header("目标计数")]
    public int targetCount = 3;
    public string storageKey = "PlayCount";

    [Header("筛选条件 (全空=任意牌)")]
    public bool checkCardType = false;
    public CardType requiredType;

    public bool checkStatsType = false;
    public StatsType requiredStatsType;

    public string requiredNameKeyword = ""; // 比如 "Fire", "Ice"

    public override bool IsMet(RuntimeItem source)
    {
        var snap = source.GetOrCreateSnapshot(storageKey);
        return snap.GetInt(storageKey) >= targetCount;
    }

    public override void OnPlayCard(CharacterBase owner, RuntimeItem source, EffectContext ctx)
    {
        var card = ctx.SourceCard;
        if (card == null) return;

        // 1. 检查类型
        if (checkCardType && card.cardType != requiredType) return;
        
        // 2. 检查属性
        if (checkStatsType && card.type != requiredStatsType) return;

        // 3. 检查名字/关键字 (简单模糊匹配)
        // 如果卡牌名字包含 "Ice"，或者 Keyword 列表里有叫 "Freeze" 的
        if (!string.IsNullOrEmpty(requiredNameKeyword))
        {
            bool nameMatch = card.cardName.Contains(requiredNameKeyword);
            bool keywordMatch = false;
            if (card.keywords != null)
            {
                foreach (var k in card.keywords)
                {
                    if (k.keywordName.Contains(requiredNameKeyword)) 
                    {
                        keywordMatch = true;
                        break;
                    }
                }
            }
            if (!nameMatch && !keywordMatch) return;
        }

        // 4. 计数增加
        var snap = source.GetOrCreateSnapshot(storageKey);
        int current = snap.GetInt(storageKey);

        if (current < targetCount)
        {
            current++;
            snap.SetInt(storageKey, current);
            Debug.Log($"[{source.data.cardName}] 打牌任务进度: {current}/{targetCount}");

            if (current == targetCount)
            {
                InventoryManager.Instance.RefreshInventoryPassives();
            }
        }
    }
}
