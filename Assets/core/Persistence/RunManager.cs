using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 运行管理器 (Singleton)
/// 负责处理关卡流转、节点切换和 Run 生命周期
/// </summary>
public class RunManager : MonoBehaviour
{
    public static RunManager Instance { get; private set; }
    
    // 🎯 Run 开始事件 - Manager 订阅此事件进行初始化
    public static event System.Action OnRunStarted;

    [Header("配置")]
    public string mapSceneName = "MapScene";
    public string defaultBattleScene = "BattleScene";
    public string defaultShopScene = "ShopScene";
    public string defaultEventScene = "EventScene";

    [Header("Player 配置")]
    public CharacterBase playerPrefab; // 玩家预制体
    
    /// <summary>
    /// 当前 Run 的 player（跨场景持久化）
    /// </summary>
    public CharacterBase currentPlayer { get; private set; }

    [Header("地图生成配置")]
    public int mapLayers = 6;
    public float nodeXSpacing = 150f;
    public float nodeYSpacing = 200f;

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

    /// <summary>
    /// 继续进度
    /// </summary>
    public bool ContinueRun()
    {
        if (SaveManager.Instance == null || !SaveManager.Instance.HasSave()) return false;

        // 1. 加载并应用数据
        bool success = SaveManager.Instance.ContinueRun();
        if (!success) return false;

        // 2. 跳转到存档所在的场景 (地图或战斗)
        var run = SaveManager.Instance.currentRun;
        string targetScene = run.currentSceneName;
        if (string.IsNullOrEmpty(targetScene)) targetScene = mapSceneName;

        Debug.Log($"[RunManager] Continuing run... Loading Scene: {targetScene}");
        
        // 🎯 读档: 恢复 player (如果没有的话)
        if (currentPlayer == null)
        {
            CreatePlayer();
        }
        
        // 🎯 从 RunData 恢复 player 状态 (覆盖 Initialize 的默认值)
        if (currentPlayer != null && run != null)
        {
            currentPlayer.baseMaxHealth = run.playerMaxHealth;
            currentPlayer.currentMaxHealth = run.playerMaxHealth;
            currentPlayer.currentHealth = run.playerCurrentHealth;
            Debug.Log($"[RunManager] Player state restored from save: HP={run.playerCurrentHealth}/{run.playerMaxHealth}");
        }
        
        // 📣 通知所有 Manager 进行初始化
        OnRunStarted?.Invoke();
        Debug.Log("[RunManager] OnRunStarted event triggered (Continue)");
        
        LoadGameScene(targetScene); // 保留 PersistentUI
        return true;
    }

    /// <summary>
    /// 跳转到地图场景
    /// </summary>
    public void StartNewRun()
    {
        SaveManager.Instance.StartNewRun();
        
        // 初始化卡组
        if (DeckManager.Instance != null)
        {
            DeckManager.Instance.ResetToStarterDeck();
        }
        
        // 🎯 创建并持久化 player
        CreatePlayer();
        
        // 生成初始地图
        GenerateMap();
        
        // 📣 通知所有 Manager 进行初始化
        OnRunStarted?.Invoke();
        Debug.Log("[RunManager] OnRunStarted event triggered");
        
        // 跳转到地图场景 (保留 PersistentUI)
        LoadGameScene(mapSceneName);
    }

    private void GenerateMap()
    {
        var run = SaveManager.Instance.currentRun;
        if (run == null) return;

        // 这里调用 MapGenerator 来生成数据
        // 使用 Inspector 面板配置的值，让玩家可以调整间距
        run.mapData = MapGenerator.Generate(
            floor: run.currentFloor, 
            layers: mapLayers, 
            xSpacing: nodeXSpacing, 
            ySpacing: nodeYSpacing
        );
        
        Debug.Log($"[RunManager] Generated map with {run.mapData.nodes.Count} nodes. Spacing: {nodeXSpacing}/{nodeYSpacing}");
    }

    /// <summary>
    /// 进入某个节点
    /// </summary>
    public void EnterNode(string nodeId)
    {
        var run = SaveManager.Instance.currentRun;
        var node = run.mapData.GetNode(nodeId);
        
        if (node == null || !node.isAvailable || node.isCompleted) return;

        run.mapData.currentActiveNodeId = nodeId;
        
        // 自动保存状态
        SaveManager.Instance.Save();

        // 决定去哪个场景
        string targetScene = node.overrideSceneName;
        if (string.IsNullOrEmpty(targetScene))
        {
            targetScene = GetDefaultSceneForType(node.nodeType);
        }

        // 🎯 战斗类型节点：激活 player
        if (IsBattleNode(node.nodeType))
        {
            ActivatePlayer();
        }
        else
        {
            DeactivatePlayer();
        }

        Debug.Log($"[RunManager] Entering node {nodeId} ({node.nodeType}) -> Loading Scene: {targetScene}");
        LoadGameScene(targetScene);
    }

    /// <summary>
    /// 判断是否为战斗类型节点
    /// </summary>
    private bool IsBattleNode(NodeType type)
    {
        return type == NodeType.Battle 
            || type == NodeType.Elite 
            || type == NodeType.Boss;
    }

    /// <summary>
    /// 激活 player（进入战斗场景时调用）
    /// </summary>
    public void ActivatePlayer()
    {
        if (currentPlayer != null)
        {
            currentPlayer.gameObject.SetActive(true);
            Debug.Log("[RunManager] Player activated");
        }
    }

    /// <summary>
    /// 停用 player（离开战斗场景时调用）
    /// </summary>
    public void DeactivatePlayer()
    {
        if (currentPlayer != null)
        {
            currentPlayer.gameObject.SetActive(false);
            Debug.Log("[RunManager] Player deactivated");
        }
    }

    /// <summary>
    /// 完成当前节点并返回地图
    /// </summary>
    public void CompleteCurrentNode()
    {
        var run = SaveManager.Instance.currentRun;
        var nodeId = run?.mapData?.currentActiveNodeId;
        
        if (string.IsNullOrEmpty(nodeId)) return;

        var node = run.mapData.GetNode(nodeId);
        if (node != null)
        {
            node.isCompleted = true;
            node.isAvailable = false;

            // 解锁后续节点
            foreach (var childId in node.outgoingNodeIds)
            {
                var child = run.mapData.GetNode(childId);
                if (child != null) child.isAvailable = true;
            }
        }

        run.mapData.currentActiveNodeId = null;
        
        // 保存进度
        SaveManager.Instance.Save();
        
        // 🎯 离开战斗场景，隐藏 player
        DeactivatePlayer();

        // 返回地图 (保留 PersistentUI)
        LoadGameScene(mapSceneName);
    }

    private string GetDefaultSceneForType(NodeType type)
    {
        return type switch
        {
            NodeType.Battle => defaultBattleScene,
            NodeType.Elite => defaultBattleScene, // 也可以有专门的场景
            NodeType.Shop => defaultShopScene,
            NodeType.Event => defaultEventScene,
            NodeType.Boss => defaultBattleScene,
            _ => defaultBattleScene
        };
    }

    /// <summary>
    /// 加载游戏场景（保留 PersistentUI）
    /// 先卸载当前游戏场景，再加载新的游戏场景
    /// </summary>
    private void LoadGameScene(string sceneName)
    {
        // 使用协程异步处理
        StartCoroutine(LoadGameSceneAsync(sceneName));
    }

    private System.Collections.IEnumerator LoadGameSceneAsync(string sceneName)
    {
        Debug.Log($"[RunManager] LoadGameSceneAsync started. Target: {sceneName}");
        
        // 1. 卸载当前游戏场景（除了 PersistentUI 和包含 RunManager 的场景）
        List<Scene> scenesToUnload = new List<Scene>();
        
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            Debug.Log($"[RunManager] Checking scene {i}: {scene.name}, isLoaded: {scene.isLoaded}");
            
            // 跳过 PersistentUI 场景
            if (scene.name == "PersistentUI")
            {
                Debug.Log($"[RunManager] Preserving PersistentUI scene");
                continue;
            }
            
            // 跳过 RunManager 所在的场景（DontDestroyOnLoad 场景）
            if (scene == gameObject.scene)
            {
                Debug.Log($"[RunManager] Preserving RunManager scene: {scene.name}");
                continue;
            }
            
            // 跳过目标场景（如果已经加载）
            if (scene.name == sceneName)
            {
                Debug.Log($"[RunManager] Target scene already loaded: {sceneName}");
                continue;
            }
            
            // 加入卸载列表
            scenesToUnload.Add(scene);
        }
        
        // 卸载收集到的场景
        foreach (var scene in scenesToUnload)
        {
            Debug.Log($"[RunManager] Unloading scene: {scene.name}");
            yield return SceneManager.UnloadSceneAsync(scene);
        }

        // 2. 加载新的游戏场景（叠加模式）
        Debug.Log($"[RunManager] Loading new scene additively: {sceneName}");
        yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

        // 3. 设置新场景为活动场景
        Scene newScene = SceneManager.GetSceneByName(sceneName);
        if (newScene.isLoaded)
        {
            SceneManager.SetActiveScene(newScene);
            Debug.Log($"[RunManager] Set active scene to: {sceneName}");
        }
        else
        {
            Debug.LogError($"[RunManager] Failed to load scene: {sceneName}");
        }
        
        // 4. 最终场景状态日志
        Debug.Log($"[RunManager] Scene loading complete. Active scenes:");
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene s = SceneManager.GetSceneAt(i);
            Debug.Log($"  - {s.name} (Active: {s == SceneManager.GetActiveScene()})");
        }
    }

    // ========================================
    // Player 管理
    // ========================================

    /// <summary>
    /// 创建 player 并标记为 DontDestroyOnLoad
    /// </summary>
    private void CreatePlayer()
    {
        // 如果已经有 player，先销毁旧的
        if (currentPlayer != null)
        {
            Destroy(currentPlayer.gameObject);
            currentPlayer = null;
        }

        // 创建新 player
        if (playerPrefab != null)
        {
            var playerObj = Instantiate(playerPrefab);
            currentPlayer = playerObj.GetComponent<CharacterBase>();
            
            // 🔑 关键：跨场景持久化
            DontDestroyOnLoad(playerObj);
            
            // 🎯 立即初始化 player（不依赖 Start，因为 Start 会跳过）
            if (currentPlayer.cardData != null && currentPlayer.sourceRuntimeItem == null)
            {
                currentPlayer.Initialize(new RuntimeItem(currentPlayer.cardData, currentPlayer));
                Debug.Log("[RunManager] Player initialized");
            }
            
            // 默认隐藏（非战斗场景）
            currentPlayer.gameObject.SetActive(false);
            
            Debug.Log("[RunManager] Player created and marked as DontDestroyOnLoad");
        }
        else
        {
            Debug.LogError("[RunManager] playerPrefab is null! Please assign it in the Inspector.");
        }
    }

    /// <summary>
    /// 获取当前 player（供其他系统调用）
    /// </summary>
    public CharacterBase GetCurrentPlayer()
    {
        return currentPlayer;
    }

    /// <summary>
    /// 清理 player（Run 结束时调用）
    /// </summary>
    public void CleanupPlayer()
    {
        if (currentPlayer != null)
        {
            Destroy(currentPlayer.gameObject);
            currentPlayer = null;
            Debug.Log("[RunManager] Player cleaned up");
        }
    }
}
