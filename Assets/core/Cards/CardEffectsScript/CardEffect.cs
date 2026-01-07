
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
    /// 获取用于 UI 描述的数值
    /// </summary>
    /// <param name="card">运行时的卡牌实例 (包含 Data, Owner 和 动态数值)</param>
    /// <param name="baseVal">基础值 (配表填的)</param>
    /// <param name="finalVal">计算后的最终值 (加了法强/Buff的)</param>
    /// <returns>如果有数值返回 true，否则 false</returns>
    public virtual bool GetDescriptionValue(RuntimeCard card, out int baseVal, out int finalVal)
    {
        baseVal = 0;
        finalVal = 0;
        return false;
    }

}




