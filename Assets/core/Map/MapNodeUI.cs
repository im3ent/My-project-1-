using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 挂在地图节点按钮上的脚本
/// </summary>
public class MapNodeUI : MonoBehaviour
{
    public Button button;
    public Image iconImage;
    public TextMeshProUGUI typeText;
    
    [Header("状态颜色")]
    public Color completedColor = Color.gray;
    public Color availableColor = Color.white;
    public Color lockedColor = new Color(0.3f, 0.3f, 0.3f, 1f);

    private MapNode _data;

    public void Setup(MapNode node)
    {
        _data = node;
        
        // 更新 UI
        if (typeText != null) typeText.text = node.nodeType.ToString();
        
        // 更新位置
        GetComponent<RectTransform>().anchoredPosition = node.position;

        // 更新状态
        RefreshState();

        // 绑定点击
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClick);
    }

    private void RefreshState()
    {
        if (_data.isCompleted)
        {
            iconImage.color = completedColor;
            button.interactable = false;
        }
        else if (_data.isAvailable)
        {
            iconImage.color = availableColor;
            button.interactable = true;
        }
        else
        {
            iconImage.color = lockedColor;
            button.interactable = false;
        }
    }

    private void OnClick()
    {
        if (RunManager.Instance != null)
        {
            RunManager.Instance.EnterNode(_data.nodeId);
        }
    }
}
