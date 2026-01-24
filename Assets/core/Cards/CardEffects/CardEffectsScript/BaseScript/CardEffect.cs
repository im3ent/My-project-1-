
using UnityEngine;

// 这是一个"模版"，不能直接用，只能被继承
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
    /// 获取用于 UI 描述的快照（可能经过修改）
    /// 子类可以重写此方法，对快照中的值进行加成计算后返回
    /// </summary>
    /// <param name="item">运行时卡牌实例</param>
    /// <param name="snapshot">该效果对应的原始快照</param>
    /// <returns>可能被修改过的快照（用于 Converter 格式化）</returns>
    public virtual EffectSnapshot GetDescriptionSnapshot(RuntimeItem item, EffectSnapshot snapshot)
    {
        // 默认实现：直接返回原始快照，不做任何修改
        return snapshot ?? new EffectSnapshot();
    }
    
    /// <summary>
    /// 获取该效果在卡牌初始化时的快照数据
    /// 子类可以重写此方法，提供自己需要存储的数值
    /// </summary>
    /// <param name="item">运行时卡牌实例</param>
    /// <returns>快照数据，返回 null 表示该效果不需要快照</returns>
    public virtual EffectSnapshot GetInitialSnapshot(RuntimeItem item)
    {
        return null;
    }
}
