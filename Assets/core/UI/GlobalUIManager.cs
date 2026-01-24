using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// 全局 UI 管理器 (Singleton)
/// 提供对持久化 UI 面板的统一访问
/// </summary>
public class GlobalUIManager : MonoBehaviour
{
    public static GlobalUIManager Instance { get; private set; }

    [Header("面板引用 (在 PersistentUI 场景中配置)")]
    public GameObject inventoryPanel;
    public GameObject deckPanel;
    public GameObject pauseMenuPanel;

    [Header("快捷键配置 (使用新 Input System)")]
    public Key inventoryKey = Key.I;
    public Key deckKey = Key.D;
    public Key pauseKey = Key.Escape;
    
    [Header("场景限制")]
    [Tooltip("在这些场景中禁用快捷键（如主菜单）")]
    public string[] disabledScenes = new string[] { "MainMenu", "Main Menu" };

    private void Awake()
    {
        // 单例模式 (但不使用 DontDestroyOnLoad，因为它在叠加场景中)
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 初始状态：全部隐藏
        CloseAllPanels();
        
        // 初始化缓存状态
        _cachedIsInGameScene = CheckIfInGameScene(SceneManager.GetActiveScene().name);
    }

    private void Update()
    {
        // ✨ 只有在游戏场景中才启用快捷键
        if (!IsInGameScene()) return;
        
        // 快捷键监听 (新 Input System)
        if (Keyboard.current != null)
        {
            if (Keyboard.current[inventoryKey].wasPressedThisFrame)
            {
                Debug.Log($"[GlobalUIManager] Inventory key pressed! Panel reference: {(inventoryPanel != null ? "Valid" : "NULL")}");
                ToggleInventory();
            }
            if (Keyboard.current[deckKey].wasPressedThisFrame)
            {
                Debug.Log($"[GlobalUIManager] Deck key pressed!");
                ToggleDeck();
            }

            if (Keyboard.current[pauseKey].wasPressedThisFrame)
            {
                Debug.Log($"[GlobalUIManager] Pause key pressed!");
                TogglePauseMenu();
            }
        }
    }
    
    // 缓存是否在游戏场景的状态
    private bool _cachedIsInGameScene = false;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _cachedIsInGameScene = CheckIfInGameScene(scene.name);
    }

    /// <summary>
    /// 检查当前是否在游戏场景中（非主菜单）
    /// </summary>
    private bool IsInGameScene()
    {
        return _cachedIsInGameScene;
    }

    private bool CheckIfInGameScene(string sceneName)
    {
        // 检查是否在禁用列表中
        foreach (var disabledScene in disabledScenes)
        {
            if (sceneName.Equals(disabledScene, System.StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }
        
        return true;
    }

    // =============================================
    // 公共 API
    // =============================================

    public void ToggleInventory()
    {
        if (inventoryPanel != null)
        {
            bool newState = !inventoryPanel.activeSelf;
            inventoryPanel.SetActive(newState);
            Debug.Log($"[GlobalUIManager] Inventory toggled to: {(newState ? "OPEN" : "CLOSED")}");
        }
        else
        {
            Debug.LogError("[GlobalUIManager] inventoryPanel is NULL! Please assign it in the Inspector.");
        }
    }

    public void ToggleDeck()
    {
        if (deckPanel != null)
            deckPanel.SetActive(!deckPanel.activeSelf);
    }



    public void TogglePauseMenu()
    {
        if (pauseMenuPanel != null)
        {
            bool willOpen = !pauseMenuPanel.activeSelf;
            pauseMenuPanel.SetActive(willOpen);
            
            // 可选：暂停游戏时冻结时间
            Time.timeScale = willOpen ? 0f : 1f;
        }
    }

    public void CloseAllPanels()
    {
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
        if (deckPanel != null) deckPanel.SetActive(false);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        
        Time.timeScale = 1f;
    }

    public bool IsAnyPanelOpen()
    {
        return (inventoryPanel != null && inventoryPanel.activeSelf)
            || (deckPanel != null && deckPanel.activeSelf)
            || (pauseMenuPanel != null && pauseMenuPanel.activeSelf);
    }
}
