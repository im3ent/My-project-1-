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
    public GameObject minionPrefab;
    
    [Header("mana")]
    public int manaCost;
    public int attack;
    public int health;
    
    public CardType cardType;
    
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
    public bool hasPassive;
    [Header("Innate Statuses (自带状态)")]
    [Tooltip("随从出生时自动获得的状态 (如: 嘲讽, 圣盾)")]
    //public List<StatusConfig> initialStatuses; 
    [Header("needsTarget")]
    public bool needsTarget;
    
    [Header("打出限制条件")]
    // 这张卡特有的条件，比如"目标必须受伤"
    public List<PlayRule> customRequirements;
}

public enum CardType { Minion, Spell, Weapon }
[System.Serializable]
public class StatusConfig
{
    public StatusEffect status; // 拖入你的 StatusEffect 资源 (如 TauntBuff)
    public int stacks = 1;      // 层数 (默认1)
}