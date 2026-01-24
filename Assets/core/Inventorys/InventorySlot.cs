using UnityEngine;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour
{
    public int slotIndex;
    public bool isVisible = true;
    public bool isLocked = false;

    [Header("UI")]
    public Image slotImage;
    public GameObject lockIcon; // 可选：锁图标

    public void UpdateVisual(Color unlockedColor, Color lockedColor)
    {
        if (slotImage == null) slotImage = GetComponent<Image>();
        
        // 1. 处理显隐 (Fog of War)
        // 使用 CanvasGroup 来隐身但保留布局占位
        var group = GetComponent<CanvasGroup>();
        if (group == null) group = gameObject.AddComponent<CanvasGroup>();

        if (!isVisible)
        {
            group.alpha = 0f;
            group.interactable = false; 
            group.blocksRaycasts = false;
            return;
        }

        // 如果可见，恢复正常
        group.alpha = 1f;
        group.interactable = true;
        group.blocksRaycasts = true;

        // 2. 处理锁定状态
        if (isLocked)
        {
            slotImage.color = lockedColor;
            if (lockIcon != null) lockIcon.SetActive(true);
        }
        else
        {
            slotImage.color = unlockedColor;
            if (lockIcon != null) lockIcon.SetActive(false);
        }
    }
}
