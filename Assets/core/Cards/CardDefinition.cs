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


    public CardType cardType; 
    

    public GameObject minionPrefab; 
    public int attack;
    public int health;
    
    [Header("Card Effects")]
    public List<CardEffect> onPlayEffects;
    public List<CardEffect> onTurnStartEffects; 
    public List<CardEffect> onTurnEndEffects;
    public List<AuraEffect> auraEffects;
    
    [Header("needsTarget")]
    public bool needsTarget;
}

public enum CardType { Minion, Spell, Weapon }