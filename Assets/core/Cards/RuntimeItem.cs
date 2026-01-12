using System.Collections.Generic;
[System.Serializable]

public class RuntimeItem :IPassiveContainer
{
    // 身份证 (永远指向原始数据，用于查图片、查描述、查原始费用)
    public CardDefinition Data { get; private set; }
    // 归属者 (谁拿着这张牌？)
    public CharacterBase Owner { get; private set; }

    // 这些是从 Data 里抄过来的，但允许在战斗中随意修改
    public int manaCost;  // 当前费用
    public int attack;    // 当前攻击
    public int health;    // 当前血量
    public  RuntimeItem(CardDefinition data, CharacterBase owner)
    {
        Data = data;
        Owner = owner;
        manaCost = data.manaCost;
        attack = data.attack;
        health = data.health;
    }
    
    // 甚至可以有：是否保密、是否被冻结...
    public bool isPassiveActive = false; 
    public float passiveMultiplier = 1f;
    public bool isScopeUpgrade = false;
    public List<PassiveEffect> permanentPassives = new ();  //如果这里要 Ctx，改 AddPermanentPassive
    // 动态列表 (专门存光环、附魔给的临时被动)
    private List<PassiveEffect> temporaryPassives = new();
    public void AddTemporaryPassive(PassiveEffect effect, RuntimeItem source =  null)
    {
        // 允许叠加 (比如两个磨刀石都在旁边，就加两次)
        temporaryPassives.Add(effect);
    }
    public void AddPermanentPassive(PassiveEffect effect)
    {
        permanentPassives.Add(effect);
    }

    public void RemovePassive(PassiveEffect effect)
    {
        if (temporaryPassives.Contains(effect))
        {
            temporaryPassives.Remove(effect);
        }
    }
    
    public void ClearTemporaryPassives()
    {
        temporaryPassives.Clear();
        isPassiveActive = false; 
        passiveMultiplier = 1.0f;
    }
    // ✨ 合并逻辑：先返回天生的，再返回后天的
    public IEnumerable<PassiveContext> GetSourcePassives()
    {
        if (Data != null && Data.passives != null)
        {
            foreach (var p in Data.passives)
            {
                // 将 SO 包装成上下文，Source 就是自己
                yield return new PassiveContext(p, this);
            }
        }

        // 返回永久随机词条
        foreach (var p in permanentPassives)
        {
            yield return new PassiveContext(p, this);
        }
        
        // 3. (可选) 临时获得的被动能不能再次传导？
        // 比如 A 给 B 加了光环，B 因此又能给 C 加光环？
        // 如果允许“传导”，这里也要 yield return temporaryPassives
        // 如果不允许，就到此为止
    }
    public IEnumerable<PassiveContext> GetActivePassives()
    {  
        // 第一部分：所有源被动（天生+永久）
        foreach (var sourcePassive in GetSourcePassives())
        {
            yield return sourcePassive;
        }

        // 第二部分：别人给我的临时光环
        foreach (var tempPassive in temporaryPassives)
        {
            yield return new PassiveContext(tempPassive, this);
        }
    }
}


public interface IPassiveContainer
{
    // 动态添加一个被动（比如光环给的，或者药水给的）
    void AddTemporaryPassive(PassiveEffect effect, RuntimeItem source = null);

    // 移除一个被动
    void RemovePassive(PassiveEffect effect);

    // ✨ 核心：获取当前所有生效的被动
    // 使用 IEnumerable 是为了能用 foreach 遍历，同时保护内部 List 不被直接修改
    IEnumerable<PassiveContext> GetSourcePassives();
    IEnumerable<PassiveContext> GetActivePassives();
}