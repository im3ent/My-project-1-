using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 地图生成器 (静态工具类)
/// 负责生成节点、连接线和布局
/// </summary>
public static class MapGenerator
{
    /// <summary>
    /// 生成一个简单的分层地图
    /// </summary>
    public static MapData Generate(int floor, int layers = 6, float xSpacing = 150f, float ySpacing = 200f)
    {
        MapData data = new MapData();
        List<List<MapNode>> layerNodes = new List<List<MapNode>>();

        // 1. 生成各层节点
        for (int i = 0; i < layers; i++)
        {
            int countInLayer = (i == 0 || i == layers - 1) ? 1 : Random.Range(2, 4);
            var nodes = new List<MapNode>();

            for (int j = 0; j < countInLayer; j++)
            {
                string id = $"F{floor}_L{i}_N{j}";
                NodeType type = DecideType(i, layers);
                
                // 使用传入的间距进行布局
                Vector2 pos = new Vector2(j * xSpacing - (countInLayer - 1) * (xSpacing * 0.5f), i * ySpacing);
                
                var node = new MapNode(id, type, pos);
                
                // 第一层节点初始设为可选
                if (i == 0) node.isAvailable = true;
                
                data.nodes.Add(node);
                nodes.Add(node);
            }
            layerNodes.Add(nodes);
        }

        // 2. 建立连接
        for (int i = 0; i < layers - 1; i++)
        {
            var currentLayer = layerNodes[i];
            var nextLayer = layerNodes[i + 1];

            foreach (var node in currentLayer)
            {
                // 确保每个节点至少连接到下一层的一个节点
                int nextIndex = Random.Range(0, nextLayer.Count);
                Connect(node, nextLayer[nextIndex]);

                // 概率增加额外连接 (分叉)
                if (nextLayer.Count > 1 && Random.value > 0.6f)
                {
                    int otherIndex = (nextIndex + 1) % nextLayer.Count;
                    Connect(node, nextLayer[otherIndex]);
                }
            }

            // 反向检查：确保下一层的每个节点都有入口
            foreach (var nextNode in nextLayer)
            {
                if (nextNode.incomingNodeIds.Count == 0)
                {
                    var prevNode = currentLayer[Random.Range(0, currentLayer.Count)];
                    Connect(prevNode, nextNode);
                }
            }
        }

        return data;
    }

    private static void Connect(MapNode from, MapNode to)
    {
        if (!from.outgoingNodeIds.Contains(to.nodeId)) from.outgoingNodeIds.Add(to.nodeId);
        if (!to.incomingNodeIds.Contains(from.nodeId)) to.incomingNodeIds.Add(from.nodeId);
    }

    private static NodeType DecideType(int layer, int totalLayers)
    {
        if (layer == 0) return NodeType.Battle;
        if (layer == totalLayers - 1) return NodeType.Boss;

        float r = Random.value;
        if (r < 0.6f) return NodeType.Battle;
        if (r < 0.8f) return NodeType.Shop;
        return NodeType.Event;
    }
}
