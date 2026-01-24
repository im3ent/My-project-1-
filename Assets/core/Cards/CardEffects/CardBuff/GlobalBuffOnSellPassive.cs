using UnityEngine;
// ✨ 需要 System.Collections.Generic 才能遍历 IEnumerable
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New TosserPassive", menuName = "Passives/Global Buff On Sell (Tosser)")]
public class GlobalBuffOnSellPassive : PassiveEffect
{
    [Header("成长配置")]
    public string globalKey = "TosserLevel";
    public int scalingBonus = 1; // 未来的投球手额外获得多少属性

    [Header("售卖效果 (Buff全家)")]
    public StatusEffect buffToApply; // 售卖时给全队贴的 Buff
    public int buffStacks = 1;

    // 1. 自我成长 (未来的投球手一买下来就强)
    public override int GetStatsFlat(CharacterBase owner, StatsType type)
    {
        // 假设只加攻击力，或者你可以把 StatsType 做成配置
        if (type == StatsType.Physical || type == StatsType.Magical) 
        {
            int level = GameManager.Instance.GetGlobalCounter(globalKey);
            return level * scalingBonus;
        }
        return 0;
    }

    // 2. 售卖逻辑
    public override void OnSell(RuntimeItem source)
    {
        // A. 提升全局等级
        GameManager.Instance.ModifyGlobalCounter(globalKey, 1);
        Debug.Log($"[Tosser] 售卖！全局等级提升至 {GameManager.Instance.GetGlobalCounter(globalKey)}");

        // B. 给当前场上/背包里的所有随从贴 Buff
        if (buffToApply == null) return;
        
        // ✨ 修复：ApplyStatus 需要 EffectSnapshot 而不是 int
        var snapshot = buffToApply.GetInitialSnapshot();
        if (snapshot == null) snapshot = new EffectSnapshot(); // 防御性编程
        snapshot.stacks = buffStacks;

        // 这里简单实现：全场友军加 Status
        foreach (var unit in GameManager.Instance.Allies)
        {
            if (unit != null && !unit.isDead)
            {
                unit.stateManager.ApplyStatus(buffToApply, source.owner, snapshot);
            }
        }
    }
}
