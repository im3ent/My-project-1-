using System.Collections;
using UnityEngine;

/// <summary>
/// 游戏动作基类 - 所有可排队执行的动作都继承自此类
/// 类似 Slay the Spire 的 AbstractGameAction
/// </summary>
public abstract class GameAction
{
    /// <summary>
    /// 动作的动画持续时间
    /// </summary>
    public float Duration { get; protected set; }
    
    /// <summary>
    /// 动作是否已完成
    /// </summary>
    public bool IsComplete { get; protected set; }
    
    /// <summary>
    /// 动作的来源（用于追踪是谁触发的）
    /// </summary>
    public object Source { get; set; }
    
    /// <summary>
    /// 执行动作的核心逻辑
    /// </summary>
    public abstract IEnumerator Execute();
    
    /// <summary>
    /// 标记动作完成
    /// </summary>
    protected void Complete()
    {
        IsComplete = true;
    }
}
