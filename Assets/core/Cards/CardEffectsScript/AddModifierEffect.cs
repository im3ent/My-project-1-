using UnityEngine;

[CreateAssetMenu(menuName = "Effects/Add Player Modifier")]
public class AddModifierEffect : CardEffect 
{
    [Header("额外效果")]
    public ModifierType modifierType; // 在面板里选：DoubleCast
    [Header("额外效果的值，比如：2 (倍), -3 (费), +1 (伤)")]
    public int value;                 // 在面板里填：2
    public bool consumeOnUse = true;  // 用完即焚

    public override float Execute(EffectContext ctx) 
    {
        PlayerStateManager.Instance.AddModifier(modifierType, value, consumeOnUse);
        return animateDuration;
    }
}