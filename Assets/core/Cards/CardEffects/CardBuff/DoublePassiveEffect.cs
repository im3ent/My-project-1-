using UnityEngine;

[CreateAssetMenu(fileName = "New DoublePassiveEffect", menuName = "CardEffects/Passive/DoublePassiveEffect")]
public class DoublePassiveEffect : PassiveEffect
{
    // 这个类作为一个标记，InventoryManager 会检测它并对目标应用“双倍倍率”
    // Scope 在基类里定义 (通常设为 Adjacent)

    // ✨ 重写：我不加被动，我只加倍率
    public override void ApplyToInventoryItem(RuntimeItem target, RuntimeItem source)
    {
        target.passiveMultiplier += 1.0f;
    }
}
