

// 1. 定义伤害类型 (枚举)
public enum StatsType {
    None,
    Health,
    Physical, // 物理 (计算护甲)
    Magical,  // 魔法 (计算魔抗/或其他)
    True,   // 真实 (无视所有减免)
    Fire,
}

// 2. 定义伤害包裹
[System.Serializable]
public class DamageInfo {
    public float amount;          // 基础伤害值
    public CharacterBase source; // 谁打的？(用于反伤、击杀统计)
    public StatsType type;     // 伤害类型
    
    // 特殊标记位 (按需添加)
    public bool ignoreArmor = false;      // 强制无视护甲 (即使是物理伤害)
    public bool ignoreVulnerable = false; // 强制无视易伤加成

    // --- 构造函数 (方便快速创建) ---
    public DamageInfo(float amount, StatsType type, CharacterBase source = null) {
        this.amount = amount;
        this.type = type;
        this.source = source;
    }
}