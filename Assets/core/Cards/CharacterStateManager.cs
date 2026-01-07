using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 挂在 CharacterBase 同级物体上 (Player 或 Enemy)
public class CharacterStateManager : MonoBehaviour
{
    [Header("归属")]
    public CharacterBase ownerCharacter;

    [Header("当前状态列表")]
    // 所有的 Buff/Debuff 都在这里
    public List<StatusInstance> statusList = new List<StatusInstance>();

    // 事件：UI 监听这个来刷新图标
    public event Action OnStateChanged;

    private void Awake()
    {
        ownerCharacter = GetComponent<CharacterBase>();
    }

    // ========================================================================
    // 1. 增删查改 (CRUD)
    // ========================================================================

    /// <summary>
    /// 施加状态
    /// </summary>
    /// <param name="effectData">状态的配置数据 (SO)</param>
    /// <param name="stacks">层数</param>
    public void ApplyStatus(StatusEffect effectData, int stacks)
    {
        // 1. 检查是否已经有了这个状态
        var existing = statusList.FirstOrDefault(s => s.Data.id == effectData.id);

        if (existing != null)
        {
            // 如果可堆叠，就加层数
            if (effectData.isStackable)
            {
                existing.Stacks += stacks;
            }
            // 如果不可堆叠 (比如 "晕眩")，通常是刷新持续时间，或者保持不变
            // 这里简单处理：不做任何事，或者你可以重置 duration (如果你有 duration 字段)
        }
        else
        {
            // 创建新的运行时实例
            var newInstance = new StatusInstance(effectData, this, stacks);
            statusList.Add(newInstance);
        }

        // 通知 UI 刷新
        NotifyStateChanged();
    }

    /// <summary>
    /// 移除状态
    /// </summary>
    public void RemoveStatus(StatusInstance instance)
    {
        if (statusList.Contains(instance))
        {
            statusList.Remove(instance);
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// 手动触发刷新事件 (给 StatusInstance 内部调用)
    /// </summary>
    public void NotifyStateChanged()
    {
        OnStateChanged?.Invoke();
    }

    // ========================================================================
    // 2. 数值计算接口 (供 GameManager 或 Combat 系统调用)
    // ========================================================================

    /// <summary>
    /// 计算卡牌的最终费用
    /// (遍历所有 Buff，应用减费/加费逻辑)
    /// </summary>
    public int GetCalculatedCost(RuntimeCard card)
    {
        int finalCost = card.Data.manaCost;

        foreach (var status in statusList)
        {
            // 让每个 Buff 有机会修改费用
            // 需要 StatusEffect 定义: virtual int ModifyCost(...)
            finalCost = status.Data.ModifyCost(status, card, finalCost);
        }

        return Mathf.Max(0, finalCost); // 费用不能小于 0
    }

    /// <summary>
    /// ✨ 你要求的：计算最终造成的伤害
    /// (用于处理 "力量"、"虚弱" 等 Buff)
    /// </summary>
    public int GetModifiedOutgoingDamage(int baseDamage)
    {
        int finalDamage = baseDamage;

        foreach (var status in statusList)
        {
            // 让每个 Buff 有机会修改输出伤害
            // 需要 StatusEffect 定义: virtual int ModifyOutgoingDamage(...)
            finalDamage = status.Data.ModifyOutgoingDamage(status, finalDamage);
        }

        return Mathf.Max(0, finalDamage);
    }

    /// <summary>
    /// 计算最终受到的伤害
    /// (用于处理 "易伤"、"格挡" 等 Buff)
    /// </summary>
    public int GetModifiedIncomingDamage(int baseDamage)
    {
        int finalDamage = baseDamage;

        foreach (var status in statusList)
        {
            // 让每个 Buff 有机会修改受到的伤害
            // 需要 StatusEffect 定义: virtual int ModifyIncomingDamage(...)
            finalDamage = status.Data.ModifyIncomingDamage(status, finalDamage);
        }

        return Mathf.Max(0, finalDamage);
    }

    // ========================================================================
    // 3. 流程事件钩子 (Hooks)
    // ========================================================================

    /// <summary>
    /// 回合开始时调用 (由 GameManager 触发)
    /// </summary>
    public void OnTurnStart()
    {
        // 倒序遍历，因为 Buff 可能会在执行过程中移除自己 (比如中毒致死，或者过期)
        for (int i = statusList.Count - 1; i >= 0; i--)
        {
            var instance = statusList[i];
            instance.Data.OnTurnStart(instance);
        }
        
        // 统一刷新一次
        NotifyStateChanged();
    }

    /// <summary>
    /// 回合结束时调用
    /// </summary>
    public void OnTurnEnd()
    {
        for (int i = statusList.Count - 1; i >= 0; i--)
        {
            var instance = statusList[i];

            // 1. 执行 Buff 的回合结束逻辑 (比如炸弹倒计时)
            instance.Data.OnTurnEnd(instance);

            // 2. 处理 "回合结束自动移除" 的临时 Buff (比如 "本回合攻击力+3")
            if (instance.Data.removeAtTurnEnd)
            {
                // 清空层数 -> 触发 RemoveStatus
                instance.DecreaseStack(instance.Stacks);
            }
        }
        NotifyStateChanged();
    }

    /// <summary>
    /// 当打出一张牌时调用
    /// (用于处理 "双倍施法"、"下张牌减费" 等逻辑)
    /// </summary>
    /// <param name="ctx">上下文 (包含了此次施法的信息)</param>
    public void OnPlayCard(EffectContext ctx)
    {
        // 倒序遍历
        for (int i = statusList.Count - 1; i >= 0; i--)
        {
            var instance = statusList[i];
            
            // 将 ctx 传进去，让 Buff 自己决定是否要修改 ctx (比如增加 repeatCount)
            // 以及是否要消耗自己
            instance.Data.OnPlayCard(instance, ctx);
        }
        NotifyStateChanged();
    }
}