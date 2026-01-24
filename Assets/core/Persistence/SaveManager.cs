using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 存档管理器 (Singleton)
/// 负责 Roguelike Run 的存档和读档
/// </summary>
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [Header("数据库引用 (用于反序列化)")]
    public CardDatabase cardDatabase;
    public PassiveDatabase passiveDatabase;

    [Header("当前 Run 数据")]
    [System.NonSerialized] public RunData currentRun;

    // 存档路径
    private string SaveFilePath => Path.Combine(Application.persistentDataPath, "run_save.json");

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// 场景加载时自动保存 (可选)
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 如果有进行中的 Run，自动保存
        if (currentRun != null && !string.IsNullOrEmpty(currentRun.currentSceneName))
        {
            // 更新当前场景名
            currentRun.currentSceneName = scene.name;
            // 自动保存 (可选，根据需求启用)
            // Save();
        }
    }

    // =============================================
    // 核心 API
    // =============================================

    /// <summary>
    /// 开始新的一局游戏
    /// </summary>
    public void StartNewRun(int startHealth = 30, int startGold = 100)
    {
        currentRun = RunData.CreateNew(startHealth, startGold);
        Debug.Log($"[SaveManager] Started new run with seed: {currentRun.runSeed}");
    }

    /// <summary>
    /// 从当前游戏状态收集数据并保存
    /// </summary>
    public void Save()
    {
        if (currentRun == null)
        {
            Debug.LogWarning("[SaveManager] No active run to save.");
            return;
        }

        // 从游戏中收集最新状态
        currentRun.GatherFromGame();

        // 序列化为 JSON
        string json = JsonUtility.ToJson(currentRun, true);

        // 写入文件
        try
        {
            File.WriteAllText(SaveFilePath, json);
            Debug.Log($"[SaveManager] Saved to: {SaveFilePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Save failed: {e.Message}");
        }
    }

    /// <summary>
    /// 从文件加载存档
    /// </summary>
    public bool Load()
    {
        if (!File.Exists(SaveFilePath))
        {
            Debug.LogWarning("[SaveManager] No save file found.");
            return false;
        }

        try
        {
            string json = File.ReadAllText(SaveFilePath);
            currentRun = JsonUtility.FromJson<RunData>(json);
            Debug.Log($"[SaveManager] Loaded run from: {currentRun.saveTime}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Load failed: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 继续已保存的游戏 (加载并应用到游戏)
    /// </summary>
    public bool ContinueRun()
    {
        if (!Load()) return false;

        // 应用存档数据到游戏
        currentRun.ApplyToGame(cardDatabase, passiveDatabase);
        Debug.Log("[SaveManager] Run continued successfully.");
        return true;
    }

    /// <summary>
    /// 检查是否有可继续的存档
    /// </summary>
    public bool HasSave()
    {
        return File.Exists(SaveFilePath);
    }

    /// <summary>
    /// 删除存档 (游戏结束时调用)
    /// </summary>
    public void DeleteSave()
    {
        if (File.Exists(SaveFilePath))
        {
            File.Delete(SaveFilePath);
            Debug.Log("[SaveManager] Save file deleted.");
        }
        currentRun = null;
    }

    /// <summary>
    /// 手动触发保存 (可绑定到按钮或场景切换)
    /// </summary>
    [ContextMenu("Save Game")]
    public void DebugSave() => Save();

    [ContextMenu("Load Game")]
    public void DebugLoad() => ContinueRun();
}
