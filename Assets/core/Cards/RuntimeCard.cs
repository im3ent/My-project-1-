[System.Serializable]
public class RuntimeCard
{
    // 1. 身份证 (永远指向原始数据，用于查图片、查描述、查原始费用)
    public CardDefinition Data { get; private set; }

    // 2. 归属者 (谁拿着这张牌？)
    public CharacterBase Owner { get; private set; }

    // =========================================================
    // ✨ 3. “合并”进来的动态数据 (Mutable Stats)
    // 这些是从 Data 里抄过来的，但允许在战斗中随意修改
    // =========================================================
    
    public int manaCost;  // 当前费用
    public int attack;    // 当前攻击
    public int health;    // 当前血量

    // 甚至可以有：是否保密、是否被冻结...
    // public bool isRevealed; 

    // 构造函数：出生时，把 Data 里的数据“抄”过来
    public  RuntimeCard(CardDefinition data, CharacterBase owner)
    {
        this.Data = data;
        this.Owner = owner;

        // ✨ 克隆数据 (关键一步！)
        this.manaCost = data.manaCost;
        this.attack = data.attack;
        this.health = data.health;
    }
}