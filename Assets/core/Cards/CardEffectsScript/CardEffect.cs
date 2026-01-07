
using UnityEngine;

// 这是一个“模版”，不能直接用，只能被继承
// 它的作用是规定所有技能必须长什么样
public abstract class CardEffect : ScriptableObject
{
    [Header("描述 (给策划看的)")]
    public string effectName;
    public float animateDuration;
    
    // 真正的游戏里，技能执行需要两个核心信息：
    // 1. user: 谁放的技能？
    // 2. target: 对谁放？(如果不需要目标，这个就是 null)
    public abstract float Execute(EffectContext ctx);

    /// <summary>
    /// 使用上下文来计算 UI 显示数值
    /// </summary>
    /// <param name="card">这张牌本身）</param>
    /// <param name="owner">卡牌效果与施法者有联动时需要</param>
    /// <param name="baseVal">基础值</param>
    /// <param name="finalVal">计算后的最终值</param>
    /// <returns>是否有数值</returns>
    public virtual bool GetDescriptionValue(CardDefinition card, CharacterBase owner, out int baseVal, out int finalVal)
    {
        baseVal = 0;
        finalVal = 0;
        return false;
    }
    
    // CharacterBase owner (谁拿着这张牌？)

}




