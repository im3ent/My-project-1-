using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 地图上的一个节点数据
/// </summary>
[Serializable]
public class MapNode
{
    public string nodeId;      // 唯一ID
    public NodeType nodeType;  // 节点类型
    public Vector2 position;   // 在UI上的位置

    // 状态
    public bool isCompleted;   // 是否已通关
    public bool isAvailable;   // 当前是否可选 (已解锁)

    // 连接
    public List<string> incomingNodeIds = new List<string>(); // 父节点
    public List<string> outgoingNodeIds = new List<string>(); // 子节点

    // 特定关卡数据 (可选)
    public string overrideSceneName; // 如果这个关卡有特殊的场景
    public string enemyGroupId;      // 对应的敌人组 ID
    
    public MapNode(string id, NodeType type, Vector2 pos)
    {
        nodeId = id;
        nodeType = type;
        position = pos;
        isCompleted = false;
        isAvailable = false;
    }
}
