using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 替代原先的 float[] customValues，用于在 RuntimeItem 和 StatusInstance 之间传递数据。
/// 提供了强类型的 Stacks 字段和灵活的字典存储，避免“魔术数字”索引。
/// </summary>
[System.Serializable]
public class EffectSnapshot
{
    // 核心数据：层数 (绝大多数 Buff 都有)
    public int stacks = 1;

    // 灵活数据：键值对存储 (替代 float[1], float[2]...)
    // Key 建议使用可读的字符串，如 "BaseDamage", "Ratio"
    private Dictionary<string, float> _floatValues = new ();

    // 访问器
    public void SetFloat(string key, float value)
    {
        _floatValues[key] = value;
    }

    public float GetFloat(string key, float defaultValue = 0f)
    {
        return _floatValues.GetValueOrDefault(key, defaultValue);
    }

    // ✨ Int 适配器 (底层依然存 float，方便统一管理)
    public void SetInt(string key, int value)
    {
        SetFloat(key, (float)value);
    }

    public int GetInt(string key, int defaultValue = 0)
    {
        return (int)GetFloat(key, defaultValue);
    }
    
    /// <summary>
    /// 检查字典中是否存在指定的 Key
    /// </summary>
    public bool ContainsKey(string key)
    {
        return _floatValues.ContainsKey(key);
    }

    /// <summary>
    /// 深拷贝一个快照
    /// </summary>
    public EffectSnapshot Clone()
    {
        var clone = new EffectSnapshot
        {
            stacks = this.stacks
        };
        // 复制字典
        foreach (var kvp in this._floatValues)
        {
            clone.SetFloat(kvp.Key, kvp.Value);
        }
        return clone;
    }
}
