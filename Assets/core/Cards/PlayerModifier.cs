// PlayerModifier.cs

public enum ModifierType 
{
    DoubleCast,       // 双倍施法
    CostReduction,    // 减费
    SpellDamage,      // 法术强度
    Immune            // 免疫
}

[System.Serializable]
public class PlayerModifier 
{
    public ModifierType type;
    public int value;      // 比如：2 (倍), -3 (费), +1 (伤)
    public bool consumeOnUse; // 是否是用完即焚？(比如减费是用一次没，还是本回合都在)

    public PlayerModifier(ModifierType type, int value, bool consumeOnUse = true)
    {
        this.type = type;
        this.value = value;
        this.consumeOnUse = consumeOnUse;
    }
}