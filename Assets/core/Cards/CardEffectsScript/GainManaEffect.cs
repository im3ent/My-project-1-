using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName = "GeminiStone/Effects/GainManaEffect")]
public class GainManaEffect : CardEffect
{
    [FormerlySerializedAs("amount")]
    [Header("数值设置")]
    [Tooltip("获得的法力值数量")]
    public int value = 1;

    [Header("模式设置")]
    [Tooltip("是否增加法力上限？(也就是跳费/获得空水晶)")]
    public bool gainMaxMana = false;

    [Tooltip("如果是增加上限，是否同时回满这部分法力？(通常是 True)")]
    public bool fillNewCrystal = true;

    [Tooltip("是否允许当前法力超过上限？(比如激活，10费变12费)")]
    public bool allowOverflow = false;
    public override float Execute(EffectContext ctx)
    {
        // 1. 处理跳费 (增加上限)
        if (gainMaxMana)
        {
            // 获得空法力水晶
            GameManager.Instance.ModifyMaxMana(value);

            if (fillNewCrystal)
            {
                // 获得一颗满的法力水晶
                GameManager.Instance.ModifyMana(value, allowOverflow); 
            }
        }
        // 获得临时法力水晶
        else
        {
            GameManager.Instance.ModifyMana(value, allowOverflow);
        }

        
        return animateDuration;
    }
    
}
