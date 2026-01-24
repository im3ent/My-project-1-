using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 一层地图的完整数据
/// </summary>
[Serializable]
public class MapData
{
    public List<MapNode> nodes = new List<MapNode>();
    public string currentActiveNodeId; // 玩家当前正处于的节点

    public MapNode GetNode(string id) => nodes.FirstOrDefault(n => n.nodeId == id);
    
    /// <summary>
    /// 获取玩家当前可以点击的所有节点
    /// </summary>
    public List<MapNode> GetAvailableNodes()
    {
        return nodes.Where(n => n.isAvailable && !n.isCompleted).ToList();
    }
}
