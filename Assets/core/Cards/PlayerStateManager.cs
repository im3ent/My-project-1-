using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PlayerStateManager : MonoBehaviour 
{
    public static PlayerStateManager Instance; // 简单单例

    // 这里存着所有的状态：{ 双倍施法: 2, 减费: 3 ... }
    public List<PlayerModifier> activeModifiers = new();

    // ✨ 定义一个事件：当Buff列表发生任何变化时触发
    public event Action OnStateChanged;
    private void Awake() { Instance = this; }

    // --- 1. 添加状态 ---
    public void AddModifier(ModifierType type, int value, bool consumeOnUse)
    {
        // 简单处理：如果有同类状态，是叠加还是覆盖？这里假设叠加
        var existing = activeModifiers.FirstOrDefault(m => m.type == type);
        if (existing != null)
        {
            existing.value = value; // 这里的逻辑看你需求，可以是 +=
        }
        else
        {
            activeModifiers.Add(new PlayerModifier(type, value, consumeOnUse));
        }
        NotifyStateChanged();
    }

    // --- 2. 查询状态 (给 GM 用的) ---
    public int GetModifierValue(ModifierType type, int defaultValue = 0)
    {
        var mod = activeModifiers.FirstOrDefault(m => m.type == type);
        return mod?.value ?? defaultValue;
    }

    public bool HasConsumableModifier(ModifierType type)
    {
        return activeModifiers.Any(m => m.type == type && m.consumeOnUse == true);
    }
    // --- 3. 消耗状态 ---
    public void ConsumeModifier(ModifierType type)
    {
        var mod = activeModifiers.FirstOrDefault(m => m.type == type);
        if (mod is { consumeOnUse: true })
        {
            activeModifiers.Remove(mod);
        }
        NotifyStateChanged();
    }
    /// <summary>
    /// 智能判断：给定的这张卡，能触发并消耗我身上的哪些 Buff？
    /// </summary>
    public List<ModifierType> GetConsumableModifiersForCard(CardDefinition card)
    {
        var result = new List<ModifierType>();

        // 1. 检查法术伤害 (只有法术能吃)
        if (card.cardType == CardType.Spell)
        {
            if (HasConsumableModifier(ModifierType.SpellDamage))
                result.Add(ModifierType.SpellDamage);
            
            if (HasConsumableModifier(ModifierType.DoubleCast))
                result.Add(ModifierType.DoubleCast);
                
            // 以后加 "法术吸血"、"法术必暴" 只需要改这里
            // GameManager 的代码一行都不用动！
        }

        // 2. 检查物理/攻击 Buff (假设你有攻击牌)
        //if (card.cardType == CardType.Attack)
        {
            //if (HasConsumableModifier(ModifierType.NextAttackCritical))
            //   result.Add(ModifierType.NextAttackCritical);
        }

        // 3. 通用 Buff (比如：下张牌打出时抽一张牌)
        //if (HasConsumableModifier(ModifierType.DrawOnPlay))
        {
            //result.Add(ModifierType.DrawOnPlay);
        }
        NotifyStateChanged();
        return result;
    }
    // --- 4. 回合结束清理 ---
    public void OnTurnEnd()
    {
        activeModifiers.Clear(); // 简单粗暴：回合结束所有Buff清空
    }
    // 封装一个触发方法，方便其他地方（比如回合结束）调用
    public void NotifyStateChanged()
    {
        OnStateChanged?.Invoke();
    }
}