using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "NewCard", menuName = "GeminiStone/Card Definition")]
public class CardDefinition: ScriptableObject
{
    [Header("cardName")]
    public string cardName;
    [TextArea] public string description;
    public Sprite artwork;
    
    [Header("mana")]
    public int manaCost;
    public int attack;
    public int health;
    
    public CardType cardType;
    public StatsType type;
    public List<KeywordData> keywords;
    [Header("背包占位设置")]
    [Range(1, 4)] public int width = 1;
    [Range(1, 4)] public int height = 1;
    // 如果列表为空，代码会自动按照 width * height 生成标准矩形
    public List<Vector2Int> shapeOffsets = new();
    
    [Header("经济数据")]
    public int price = 100; // 买入价格
    
    [Header("Card Effects")]
    public List<CardEffect> onPlayEffects;
    public List<CardEffect> onTurnStartEffects; 
    public List<CardEffect> onTurnEndEffects;

    [Header("Passive Effects (光环逻辑)")]
    [Tooltip("随从在场时持续生效的逻辑效果 (如: 战吼双倍, 法术减费)")]
    public List<PassiveEffect> passives;

    [Header("needsTarget")]
    public bool needsTarget;
    
    [Header("消耗 (使用后移除，不会洗回牌库)")]
    [Tooltip("如果勾选，这张卡打出后会进入消耗堆，而不是弃牌堆")]
    public bool exhaust = true;
    
    [Header("固有 (Innate)")]
    [Tooltip("战斗开始时，固有牌一定会出现在初始手牌中")]
    public bool isInnate = false;
    
    [Header("无限手牌 (Return To Hand)")]
    [Tooltip("如果勾选，这张卡打出后会立即回到手牌，而不是进入弃牌堆。只要法力足够可以无限使用。")]
    public bool returnToHand = false;
    [Header("打出限制条件")]
    // 这张卡特有的条件，比如"目标必须受伤"
    public List<PlayRule> customRequirements;
}

public enum CardType { Minion, Spell, Weapon }

