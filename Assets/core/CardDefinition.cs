using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewCard", menuName = "GeminiStone/Card Definition")]
public class CardDefinition : ScriptableObject
{
    [Header("cardName")]
    public string cardName;
    [TextArea] public string description;
    public Sprite artwork;

    [Header("mana")]
    public int manaCost;

    [Header("核心类型")]
    public CardType cardType; // 法术还是随从？
    
    [Header("随从属性 (只有选Minion时才填)")]
    public GameObject minionPrefab; // 随从的模型/预制体
    public int attack;
    public int health;
    [Header("Card Effects")]
    
    
    public List<CardEffect> onPlayEffects;
    public List<CardEffect> onDeathEffects; 
    public List<CardEffect> onTurnStartEffects; 

    [Header("needsTarget")]

    public bool needsTarget;
}

public enum CardType {
    Spell,  // 法术：打出效果后，卡牌消失
    Minion  // 随从：打出效果后，卡牌变成场上的一个单位
}