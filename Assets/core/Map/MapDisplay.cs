using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 负责在 UI 上渲染整个地图
/// </summary>
public class MapDisplay : MonoBehaviour
{
    public GameObject nodePrefab;   // MapNodeUI 预制体
    public Transform nodeContainer; // 节点父物体
    public RectTransform lineContainer; // 连线父物体 (可选)

    private void Start()
    {
        RefreshMap();
    }

    public void RefreshMap()
    {
        // 1. 清理旧 UI
        foreach (Transform child in nodeContainer) Destroy(child.gameObject);
        if (lineContainer != null)
        {
            foreach (Transform child in lineContainer) Destroy(child.gameObject);
        }

        // 2. 获取当前存档
        var run = SaveManager.Instance?.currentRun;
        if (run == null || run.mapData == null)
        {
            Debug.LogWarning("[MapDisplay] No map data found in SaveManager.");
            return;
        }

        // 3. 生成节点
        Dictionary<string, RectTransform> spawnedNodes = new Dictionary<string, RectTransform>();
        
        foreach (var nodeData in run.mapData.nodes)
        {
            var nodeObj = Instantiate(nodePrefab, nodeContainer);
            var nodeUI = nodeObj.GetComponent<MapNodeUI>();
            nodeUI.Setup(nodeData);
            
            spawnedNodes.Add(nodeData.nodeId, nodeObj.GetComponent<RectTransform>());
        }

        // 4. (可选) 绘制连接线
        if (lineContainer != null)
        {
            foreach (var nodeData in run.mapData.nodes)
            {
                foreach (var childId in nodeData.outgoingNodeIds)
                {
                    if (spawnedNodes.ContainsKey(childId))
                    {
                        DrawLine(spawnedNodes[nodeData.nodeId], spawnedNodes[childId]);
                    }
                }
            }
        }
    }

    private void DrawLine(RectTransform start, RectTransform end)
    {
        // 这里可以使用简单的 Image 旋转缩放来模拟连线
        // 或者使用专用的 UI Line Renderer 插件
        GameObject line = new GameObject("Line", typeof(Image));
        line.transform.SetParent(lineContainer, false);
        
        var img = line.GetComponent<Image>();
        img.color = new Color(1,1,1, 0.3f);
        img.raycastTarget = false; // 🛑 重要：防止连线遮挡点击
        
        var rect = img.rectTransform;
        Vector2 dir = (end.anchoredPosition - start.anchoredPosition);
        float distance = dir.magnitude;
        
        rect.sizeDelta = new Vector2(distance, 2f); // 2像素厚
        rect.pivot = new Vector2(0, 0.5f);
        rect.anchoredPosition = start.anchoredPosition;
        
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        rect.rotation = Quaternion.Euler(0, 0, angle);
    }
}
