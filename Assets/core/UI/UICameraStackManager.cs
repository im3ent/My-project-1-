using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// URP Camera Stack 管理器
/// 负责将 PersistentUI 的 UICamera 添加到每个场景的主相机栈中
/// </summary>
public class UICameraStackManager : MonoBehaviour
{
    [Header("UI 相机引用")]
    [Tooltip("PersistentUI 场景中的 UI Overlay Camera")]
    public Camera uiCamera;

    [Header("调试")]
    public bool showDebugLogs = true;

    private void Start()
    {
        // 首次加载时添加到主相机栈
        AddUICameraToMainStack();
    }

    private void OnEnable()
    {
        // 监听场景加载事件
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        // 取消监听
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// 场景加载回调
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[UICameraStack] Scene loaded: {scene.name}, mode: {mode}");
        
        // 无论是 Single 还是 Additive，只要不是 PersistentUI 场景，都尝试刷新
        if (scene.name != "PersistentUI")
        {
            // 延迟一帧执行，确保新场景的相机已完全初始化
            StartCoroutine(AddUICameraNextFrame());
        }
    }

    /// <summary>
    /// 延迟一帧添加相机（确保主相机已经初始化完成）
    /// </summary>
    private System.Collections.IEnumerator AddUICameraNextFrame()
    {
        yield return null;
        Debug.Log($"[UICameraStack] Attempting to add UICamera (delayed frame)");
        AddUICameraToMainStack();
    }

    /// <summary>
    /// 将 UICamera 添加到主相机的渲染栈
    /// </summary>
    public void AddUICameraToMainStack()
    {
        Debug.Log($"[UICameraStack] AddUICameraToMainStack called");
        
        if (uiCamera == null)
        {
            Debug.LogWarning("[UICameraStack] UICamera 引用为空，请在 Inspector 中指定！");
            return;
        }

        // 查找主相机
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            Debug.LogWarning("[UICameraStack] 场景中没有找到 Main Camera！");
            return;
        }

        Debug.Log($"[UICameraStack] Found Main Camera: {mainCam.name}");

        // 获取主相机的 URP 数据
        var cameraData = mainCam.GetUniversalAdditionalCameraData();
        if (cameraData == null)
        {
            Debug.LogError("[UICameraStack] 主相机没有 UniversalAdditionalCameraData 组件！请确保使用 URP。");
            return;
        }

        Debug.Log($"[UICameraStack] Camera stack count before: {cameraData.cameraStack.Count}");

        // 检查是否已经在栈中
        if (cameraData.cameraStack.Contains(uiCamera))
        {
            if (showDebugLogs)
                Debug.Log($"[UICameraStack] UICamera 已在 {mainCam.name} 的栈中，跳过添加。");
            return;
        }

        // 添加到栈
        cameraData.cameraStack.Add(uiCamera);
        
        Debug.Log($"[UICameraStack] ✓ UICamera 已添加到 {mainCam.name} 的渲染栈！Stack count: {cameraData.cameraStack.Count}");
    }

    /// <summary>
    /// 手动从栈中移除 UICamera（通常不需要调用）
    /// </summary>
    public void RemoveUICameraFromStack()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null || uiCamera == null) return;

        var cameraData = mainCam.GetUniversalAdditionalCameraData();
        if (cameraData != null && cameraData.cameraStack.Contains(uiCamera))
        {
            cameraData.cameraStack.Remove(uiCamera);
            if (showDebugLogs)
                Debug.Log($"[UICameraStack] UICamera 已从 {mainCam.name} 的栈中移除。");
        }
    }
}
