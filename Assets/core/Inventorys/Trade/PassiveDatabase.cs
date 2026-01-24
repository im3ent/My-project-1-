using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor; // 这一行只在编辑器模式下编译
#endif

[CreateAssetMenu(menuName = "Game/Passive Database")]
public class PassiveDatabase : ScriptableObject
{
    [Header("被动池列表")]
    public List<PassiveEffect> allPassives;

    // --- 运行时逻辑 ---
    public PassiveEffect GetRandomPassive()
    {
        //if (allPassives == null || allPassives.Count == 0) return null;
        
        // 这里可以结合权重算法
        return allPassives[Random.Range(0, allPassives.Count)];
    }

    // 运行时缓存 (name -> PassiveEffect)
    private Dictionary<string, PassiveEffect> _cache;

    /// <summary>
    /// 通过名称查找被动
    /// </summary>
    public PassiveEffect GetByName(string passiveName)
    {
        if (string.IsNullOrEmpty(passiveName)) return null;

        // 懒加载缓存
        if (_cache == null)
        {
            _cache = new Dictionary<string, PassiveEffect>();
            foreach (var p in allPassives)
            {
                if (p != null && !string.IsNullOrEmpty(p.name))
                {
                    _cache[p.name] = p;
                }
            }
        }

        return _cache.TryGetValue(passiveName, out var passive) ? passive : null;
    }

    // --- 👑 编辑器自动化工具 (核心部分) ---
#if UNITY_EDITOR
    [ContextMenu("自动扫描并填充所有被动")]
    private void AutoFillPassives()
    {
        // 1. 查找项目中所有类型为 PassiveEffect 的资源 GUID
        string[] guids = AssetDatabase.FindAssets("t:PassiveEffect");
        
        allPassives.Clear(); // 先清空旧的

        foreach (string guid in guids)
        {
            // 2. 将 GUID 转换为实际路径
            string path = AssetDatabase.GUIDToAssetPath(guid);
            
            // 3. 加载资源
            PassiveEffect passive = AssetDatabase.LoadAssetAtPath<PassiveEffect>(path);
            
            // 4. (可选) 过滤逻辑：比如不把名为 "Test_" 开头的测试文件加进来
            if (passive != null && !passive.name.StartsWith("Test_"))
            {
                allPassives.Add(passive);
            }
        }
        
        // 5. 标记已修改，让 Unity 保存
        EditorUtility.SetDirty(this);
        Debug.Log($"成功自动填充了 {allPassives.Count} 个被动效果！");
    }
#endif
}