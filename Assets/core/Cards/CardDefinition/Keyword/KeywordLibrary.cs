using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// 关键词库 - 管理所有关键词并提供解析功能
/// </summary>
public static class KeywordLibrary
{
    // 从 Resources 自动加载的关键词
    private static Dictionary<string, KeywordData> _keywords;
    private static bool _isInitialized = false;
    
    // 解析缓存
    private static readonly Dictionary<string, string> _parseCache = new();
    
    /// <summary>
    /// 初始化：从 Resources/Keywords 文件夹加载所有关键词
    /// </summary>
    public static void Initialize()
    {
        if (_isInitialized) return;
        
        _keywords = new Dictionary<string, KeywordData>();
        
        // 从 Resources/Keywords 加载所有 KeywordData
        var allKeywords = Resources.LoadAll<KeywordData>("Keywords");
        
        foreach (var kw in allKeywords)
        {
            if (kw != null && !string.IsNullOrEmpty(kw.keywordName))
            {
                _keywords[kw.keywordName] = kw;
            }
        }
        
        // 如果没有关键词资产，尝试从 CardDefinition/Keyword 目录加载
        if (_keywords.Count == 0)
        {
            // 手动扫描可能需要 Editor 脚本，这里保留一个回退机制
            Debug.Log("[KeywordLibrary] 未在 Resources/Keywords 找到关键词，请将关键词资产移动到该目录。");
        }
        
        _isInitialized = true;
        Debug.Log($"[KeywordLibrary] 加载了 {_keywords.Count} 个关键词");
    }
    
    /// <summary>
    /// 手动注册关键词（用于不在 Resources 的情况）
    /// </summary>
    public static void RegisterKeyword(KeywordData keyword)
    {
        if (keyword == null || string.IsNullOrEmpty(keyword.keywordName)) return;
        
        if (_keywords == null) _keywords = new Dictionary<string, KeywordData>();
        _keywords[keyword.keywordName] = keyword;
    }
    
    /// <summary>
    /// 获取关键词描述
    /// </summary>
    public static string GetDesc(string key)
    {
        EnsureInitialized();
        
        if (_keywords.TryGetValue(key, out var kw))
        {
            return kw.description;
        }
        return "未知效果";
    }
    
    /// <summary>
    /// 获取关键词数据
    /// </summary>
    public static KeywordData GetKeyword(string key)
    {
        EnsureInitialized();
        
        _keywords.TryGetValue(key, out var kw);
        return kw;
    }
    
    /// <summary>
    /// 获取所有关键词
    /// </summary>
    public static IEnumerable<KeywordData> GetAllKeywords()
    {
        EnsureInitialized();
        return _keywords.Values;
    }
    
    /// <summary>
    /// 解析描述文本，将关键词替换为带颜色的版本
    /// 支持两种格式：
    /// 1. [关键词名] - 方括号包裹的显式引用
    /// 2. 直接匹配关键词名称
    /// </summary>
    public static string Parse(string rawDescription, bool autoDetect = true)
    {
        if (string.IsNullOrEmpty(rawDescription)) return rawDescription;
        
        EnsureInitialized();
        
        // 检查缓存
        string cacheKey = $"{rawDescription}_{autoDetect}";
        if (_parseCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }
        
        string result = rawDescription;
        
        // 1. 处理显式引用 [关键词名]
        result = ProcessExplicitReferences(result);
        
        // 2. 自动检测关键词名称
        if (autoDetect)
        {
            result = ProcessAutoDetection(result);
        }
        
        // 缓存结果
        _parseCache[cacheKey] = result;
        
        return result;
    }
    
    /// <summary>
    /// 处理显式引用 [关键词名]
    /// </summary>
    private static string ProcessExplicitReferences(string text)
    {
        // 匹配 [xxx] 格式
        var regex = new Regex(@"\[([^\]]+)\]");
        
        return regex.Replace(text, match =>
        {
            string keywordName = match.Groups[1].Value;
            var keyword = GetKeyword(keywordName);
            
            if (keyword != null)
            {
                return keyword.GetFormattedText();
            }
            
            // 如果找不到关键词，保留原文
            return match.Value;
        });
    }
    
    /// <summary>
    /// 自动检测文本中的关键词名称并高亮
    /// </summary>
    private static string ProcessAutoDetection(string text)
    {
        if (_keywords == null) return text;
        
        foreach (var kvp in _keywords)
        {
            var keyword = kvp.Value;
            if (keyword == null || string.IsNullOrEmpty(keyword.keywordName)) continue;
            
            // 直接替换完整匹配
            if (text.Contains(keyword.keywordName))
            {
                // 避免重复替换（如果已经有颜色标签）
                if (text.Contains($">{keyword.keywordName}<"))
                {
                    continue;
                }
                
                text = text.Replace(keyword.keywordName, keyword.GetFormattedText());
            }
        }
        
        return text;
    }
    
    /// <summary>
    /// 从描述中提取所有使用的关键词
    /// </summary>
    public static List<KeywordData> ExtractKeywords(string rawDescription)
    {
        var result = new List<KeywordData>();
        if (string.IsNullOrEmpty(rawDescription)) return result;
        
        EnsureInitialized();
        
        // 检查显式引用
        var regex = new Regex(@"\[([^\]]+)\]");
        var matches = regex.Matches(rawDescription);
        
        foreach (Match match in matches)
        {
            string keywordName = match.Groups[1].Value;
            var keyword = GetKeyword(keywordName);
            if (keyword != null && !result.Contains(keyword))
            {
                result.Add(keyword);
            }
        }
        
        // 检查自动检测的关键词
        if (_keywords != null)
        {
            foreach (var kvp in _keywords)
            {
                var keyword = kvp.Value;
                if (keyword == null || result.Contains(keyword)) continue;
                
                if (rawDescription.Contains(keyword.keywordName))
                {
                    result.Add(keyword);
                }
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// 清除缓存
    /// </summary>
    public static void ClearCache()
    {
        _parseCache.Clear();
    }
    
    private static void EnsureInitialized()
    {
        if (!_isInitialized)
        {
            Initialize();
        }
    }
}