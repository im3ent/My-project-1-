using UnityEngine;
using TMPro; // 必须引用 TextMeshPro
using DG.Tweening; // 必须引用 DOTween

public class FloatingText : MonoBehaviour
{
    private TextMeshPro textMesh; // 我们用世界空间的 TMP，不是 UI 里的

    void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
        if (textMesh == null) textMesh = GetComponentInChildren<TextMeshPro>();
    }

    // 初始化方法
    public void Setup(int damageAmount, System.Action onComplete = null)
    {
        textMesh.text = damageAmount.ToString();
        textMesh.color = Color.red; // 初始颜色 (也可以根据伤害类型变色，比如暴击是黄色)
        textMesh.fontSize = 6; // 字体大小，根据你的游戏画面调整

        // --- 核心动画 (Juice!) ---
        
        // 1. 向上飘 (0.8秒内飘 2个单位)
        transform.DOMoveY(2f, 0.8f).SetRelative(true).SetEase(Ease.OutQuad);
        
        // 2. 随机一点左右偏移 (让数字看起来更灵动，不会叠在一起)
        float randomX = Random.Range(-0.5f, 0.5f);
        transform.DOMoveX(randomX, 0.8f).SetRelative(true);

        // 3. 逐渐变透明 (Fade Out)
        textMesh.DOFade(0, 0.8f).SetEase(Ease.InQuad).OnComplete(() => {
            if (onComplete != null)
            {
                onComplete.Invoke();
            }
            else
            {
                Destroy(gameObject); // 动画播完，自杀
            }
        });
        
        // 4. (可选) 刚出来时有个弹跳缩放
        transform.localScale = Vector3.zero;
        transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
    }
}