using System;

/// <summary>
/// 关卡/节点类型
/// </summary>
public enum NodeType
{
    Battle,     // 普通战斗
    Elite,      // 精英战斗
    Shop,       // 商店
    Event,      // 随机事件 (非战斗)
    Treasure,   // 宝箱房
    Rest,       // 休息点 (回血/升级)
    Boss        // Boss 战
}
