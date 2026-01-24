using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 持久化 UI 加载器
/// 负责在游戏启动时加载叠加的 UI 场景
/// </summary>
public class PersistentUILoader : MonoBehaviour
{
    [Header("配置")]
    [Tooltip("持久化 UI 场景的名称 (必须添加到 Build Settings)")]
    public string uiSceneName = "PersistentUI";
    
    [Tooltip("是否在 Start 时自动加载")]
    public bool autoLoadOnStart = true;

    private static bool isUILoaded = false;

    private void Start()
    {
        if (autoLoadOnStart)
        {
            LoadPersistentUI();
        }
    }

    /// <summary>
    /// 加载持久化 UI 场景 (叠加模式)
    /// </summary>
    public void LoadPersistentUI()
    {
        // 防止重复加载
        if (isUILoaded) return;
        
        // 检查场景是否已经加载
        Scene uiScene = SceneManager.GetSceneByName(uiSceneName);
        if (uiScene.isLoaded)
        {
            isUILoaded = true;
            return;
        }

        // 异步叠加加载
        SceneManager.LoadSceneAsync(uiSceneName, LoadSceneMode.Additive).completed += _ =>
        {
            isUILoaded = true;
            Debug.Log($"[PersistentUILoader] UI Scene '{uiSceneName}' loaded.");
        };
    }

    /// <summary>
    /// 卸载持久化 UI 场景 (通常在返回主菜单时调用)
    /// </summary>
    public void UnloadPersistentUI()
    {
        if (!isUILoaded) return;

        Scene uiScene = SceneManager.GetSceneByName(uiSceneName);
        if (uiScene.isLoaded)
        {
            SceneManager.UnloadSceneAsync(uiSceneName);
            isUILoaded = false;
            Debug.Log($"[PersistentUILoader] UI Scene '{uiSceneName}' unloaded.");
        }
    }
}
