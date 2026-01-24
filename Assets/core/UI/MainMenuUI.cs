using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 主菜单 UI 逻辑
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    public Button newGameButton;
    public Button continueButton;

    private void Start()
    {
        // 1. 绑定按钮点击
        if (newGameButton != null)
            newGameButton.onClick.AddListener(OnNewGameClicked);

        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinueClicked);

        // 2. 检查是否有存档，如果没有，禁用“继续”按钮
        RefreshContinueButton();
    }

    private void RefreshContinueButton()
    {
        if (continueButton != null)
        {
            // 如果没有存档，按钮置灰
            bool hasSave = (SaveManager.Instance != null && SaveManager.Instance.HasSave());
            continueButton.interactable = hasSave;
        }
    }

    private void OnNewGameClicked()
    {
        if (RunManager.Instance != null)
        {
            RunManager.Instance.StartNewRun();
        }
    }

    private void OnContinueClicked()
    {
        if (RunManager.Instance != null)
        {
            RunManager.Instance.ContinueRun();
        }
    }
}
