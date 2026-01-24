using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 挂在 CharacterBase 同级物体上 (Player 或 Enemy)
public class CharacterStateManager : MonoBehaviour,IPassiveContainer
{
    [Header("归属")]
    public CharacterBase ownerCharacter;

    [Header("当前状态列表")]
    // 所有的 Buff/Debuff 都在这里
    [System.NonSerialized] public List<StatusInstance> statusList = new();
    [System.NonSerialized] public List<PassiveContext> inventoryPassives = new();
    [System.NonSerialized] public List<PassiveEffect> nativePassives = new();
    // 事件：UI 监听这个来刷新图标
    
    [Header("数据提供者引用")]
    public ListProvider statusProvider; // 拖入挂了 ListProvider 的子物体A
    public ListProvider auraProvider;   // 拖入挂了 ListProvider 的子物体B
    
    public event Action OnStateChanged;

    private void Awake() => ownerCharacter = GetComponent<CharacterBase>();

    /// <summary>
    /// 施加状态
    /// </summary>
    /// <param name="effectData">状态的配置数据 (SO)</param>
    /// <param name="caster"></param>
    /// <param name="snapshot">快照数据 (包含层数)</param>
    public void ApplyStatus(StatusEffect effectData, CharacterBase caster, EffectSnapshot snapshot)
    {
        // ✨ 安全检查：防止传入 null 导致后续崩溃
        if (effectData == null)
        {
            return;
        }

        // 1. 检查是否已经有了这个状态
        // Debug.Log($"[CSM] ApplyStatus called for {effectData.name} (ID: {effectData.id}). Snapshot Stacks: {snapshot?.Stacks ?? 0}");
        
        Debug.Log($"ApplyStatus: {effectData.name}, ID: '{effectData.id}', IsStackable: {effectData.isStackable}");
        var existing = statusList.FirstOrDefault(s => s.Data.id == effectData.id);
        if (existing != null) Debug.Log($"Found existing status: {existing.Data.name} with ID '{existing.Data.id}'");
        else Debug.Log("No existing status found with this ID.");

        if (existing != null)
        {
            // 如果可堆叠，就加层数
            if (effectData.isStackable && existing.snapshot != null && snapshot != null)
            {
                existing.snapshot.stacks += snapshot.stacks;
            }
            // ✨ 如果不可堆叠，我们更新快照里的参数 (覆盖旧的)
            // 这样后面触发的强力效果可以覆盖前面的弱效果 (Refreshes/Updates value)
            else if (!effectData.isStackable && snapshot != null)
            {
                // ✨ 修复：必须 Clone，否则会引用到源头 (比如 RuntimeItem 里的快照)
                // 导致修改 Status 层数时意外修改了物品本身的原始数据！
                existing.snapshot = snapshot.Clone(); 
                if (caster != null) existing.Caster = caster; // 更新施法者
            }
            
            NotifyStateChanged();
            RefreshTooltips();
        }
        else
        {
            // 创建新的运行时实例
            var newInstance = new StatusInstance(effectData, this, caster, snapshot);
            statusList.Add(newInstance);
            
            // ✨ 全局事件：Buff 施加
            GameEvents.TriggerBuffApplied(ownerCharacter, effectData, snapshot?.stacks ?? 1);
            
            NotifyStateChanged();
            RefreshTooltips();
        }
    }

    /// <summary>
    /// 移除状态
    /// </summary>
    public void RemoveStatus(StatusInstance instance)
    {
        if (statusList.Contains(instance))
        {
            // ✨ 全局事件：Buff 移除
            GameEvents.TriggerBuffRemoved(ownerCharacter, instance.Data);
            
            statusList.Remove(instance);
            NotifyStateChanged();
            RefreshTooltips();
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
    public int GetCalculatedCost(RuntimeItem item)
    {
        int finalCost = item.manaCost;
        // 优化：直接遍历，移除 ToList() (GC Friendly)
        // 假设 ModifyCost 只修改数值不移除 Buff (通常如此)
        for (int i = 0; i < statusList.Count; i++)
        {
            var status = statusList[i];
            
            // ✨ 防御性检查
            if (status.Data == null) continue;

            // 让每个 Buff 有机会修改费用
            finalCost = status.Data.ModifyCost(status, item, finalCost);
        }
        RefreshTooltips();
        return Mathf.Max(0, finalCost); // 费用不能小于 0
    }
    //// 属性计算核心 
    // ========================================================================
    /// <summary>
    /// ✨ 核心公式：计算最终法术/元素伤害
    /// 公式 = (基础 + Flat) * (1 + Increased总和) * (More连乘)
    /// </summary>
    public int GetModifiedStats(float baseDamage , StatsType type)
    {
        if (this == null) return Mathf.FloorToInt(baseDamage);
        
        // 优化：直接在循环中累加，避免创建临时 List<IStatModifier> (GC Friendly)
        float totalFlat = 0;
        var totalIncreased = 0f;
        var totalMore = 1.0f;
        
        // 1. 遍历本地 Buff
        for (int i = 0; i < statusList.Count; i++)
        {
            var status = statusList[i];
            if (status.Data != null) 
            {
                totalFlat += status.GetStatsFlat(type);
                totalIncreased += status.GetStatsIncreased(type);
                totalMore *= status.GetStatsMore(type);
            }
        }

        // 2. 遍历场上所有单位的光环
        if (GameManager.Instance != null)
        {
            foreach (var unit in GameManager.Instance.AllUnits)
            {
                if (unit == null || unit.currentHealth <= 0) continue;
                // GetActivePassives 使用 yield return，开销很小
                foreach (var ctx in unit.stateManager.GetActivePassives())
                {
                    if (ctx.effect.ShouldTrigger(unit, ownerCharacter))
                    {
                        totalFlat += ctx.GetStatsFlat(type);
                        totalIncreased += ctx.GetStatsIncreased(type);
                        totalMore *= ctx.GetStatsMore(type);
                    }
                }
            }
        }
        
        // 3. 执行 RPG 伤害公式
        var finalValue = (baseDamage + totalFlat) * (1.0f + totalIncreased) * totalMore;

        return Mathf.Max(0, Mathf.FloorToInt(finalValue));
    }
    /// <summary>
    /// ✨ 你要求的：计算最终造成的伤害
    /// (用于处理 "力量"、"虚弱" 等 Buff)
    /// </summary>
    public float GetModifiedOutgoingDamage(float baseDamage)
    {
        float finalDamage = baseDamage;

        foreach (var status in statusList)
        {
            // ✨ 防御性检查
            if (status.Data == null)
            {
                continue;
            }

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
    public int GetModifiedIncomingDamage(float baseDamage, CharacterBase attacker = null)
    {
        float totalFlat = 0f;
        float totalIncreased = 0f;
        float totalMore = 1.0f;

        // 1. 遍历 Buff
        for (int i = 0; i < statusList.Count; i++)
        {
            var status = statusList[i];
            if (status.Data != null)
            {
                totalFlat += status.GetIncomingFlat(attacker);
                totalIncreased += status.GetIncomingIncreased(attacker);
                totalMore *= status.GetIncomingMore(attacker);
            }
        }

        // 2. 遍历光环
        foreach (var unit in GameManager.Instance.AllUnits)
        {
            if (unit == null || unit.currentHealth <= 0) continue;
            foreach (var ctx in unit.stateManager.GetActivePassives())
            {
                if (ctx.effect.ShouldTrigger(unit, ownerCharacter))
                {
                    totalFlat += ctx.GetIncomingFlat(attacker);
                    totalIncreased += ctx.GetIncomingIncreased(attacker);
                    totalMore *= ctx.GetIncomingMore(attacker);
                }
            }
        }

        // 计算公式：(基础伤害 + 固定修正) * (1 + 加法百分比总和) * 独立乘法
        var finalDamage = (baseDamage + totalFlat) * (1.0f + totalIncreased) * totalMore;

        // 最后还可以传给旧的接口做特殊处理（比如圣盾直接变0）
        // 注意：这里我们使用倒序遍历或副本以防万一，但修改数值通常不涉及 List 变动
        for (int i = 0; i < statusList.Count; i++)
        {
            var status = statusList[i];
            if (status.Data != null)
            {
                finalDamage = status.Data.ModifyIncomingDamage(status, finalDamage);
            }
        }

        RefreshTooltips(); // 注意：这个调用目前依然很重，稍后优化
        return Mathf.FloorToInt(finalDamage);
    }

    // ========================================================================
    // 3. 流程事件钩子 (Hooks)
    // ========================================================================

    /// <summary>
    /// 回合开始时调用 (由 GameManager 触发)
    /// </summary>
    public void OnTurnStart()
    {
        // 这样无论 Buff 内部怎么删除、怎么触发死亡、怎么修改原始 list，循环都不会崩
        var snapshot = new List<StatusInstance>(statusList);

        foreach (var instance in snapshot)
        {
            // 双重保险：确保原始列表里还有这个 Buff (防止被前面的 Buff 顺手移除了)
            if (!statusList.Contains(instance)) continue;
        
            // 执行逻辑
            if (instance.Data != null)
            {
                instance.Data.OnTurnStart(instance);
            }
        }
    
        // 统一刷新一次 UI，而不是每扣一层就刷一次
        NotifyStateChanged();
        RefreshTooltips();
    }

    /// <summary>
    /// 回合结束时调用
    /// </summary>
    public void OnTurnEnd()
    {
        for (int i = statusList.Count - 1; i >= 0; i--)
        {
            var instance = statusList[i];

            // ✨ 防御性检查
            if (instance.Data == null)
            {
                
                statusList.RemoveAt(i);
                continue;
            }

            // 1. 执行 Buff 的回合结束逻辑 (比如炸弹倒计时)
            instance.Data.OnTurnEnd(instance);

            // 2. 处理 "回合结束自动移除" 的临时 Buff (比如 "本回合攻击力+3")
            // 2. 处理 "回合结束自动移除" 的临时 Buff (比如 "本回合攻击力+3")
            if (instance.Data.removeAtTurnEnd)
            {
                // 清空层数 -> 触发 RemoveStatus
                if (instance.snapshot != null)
                {
                    instance.DecreaseStack(instance.snapshot.stacks);
                }
                else
                {
                    RemoveStatus(instance);
                }
            }
        }
        NotifyStateChanged();
        RefreshTooltips();
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
            
            // ✨ 防御性检查
            if (instance.Data == null)
            {
                Debug.LogWarning($"[CharacterStateManager] Found StatusInstance with null Data on {ownerCharacter?.name ?? "Unknown"}! Removing corrupted instance...");
                statusList.RemoveAt(i);
                continue;
            }
            
            // 将 ctx 传进去，让 Buff 自己决定是否要修改 ctx (比如增加 repeatCount)
            // 以及是否要消耗自己
            instance.Data.OnPlayCard(instance, ctx);
        }

        // ✨ 修复：同时触发被动效果 (Passive) 的 OnPlayCard
        // 记得使用 ToList() 或副本，以防回调修改了集合
        foreach (var ctxP in GetActivePassives().ToList())
        {
            if (ctxP.effect != null)
            {
                // 现在传入 source (RuntimeItem)，满足新签名
                ctxP.effect.OnPlayCard(ownerCharacter, ctxP.source, ctx);
            }
        }

        NotifyStateChanged();
        RefreshTooltips();
    }
    
    
    public void ClearInventoryPassives()
    {
        // 如果你有角色的“天赋树”被动，不要在这里清空它们！
        // 建议把 globalPassives 专门用来存“背包给的被动”
        // 或者把被动分成两个 list：nativePassives (天赋) 和 inventoryPassives (背包)
        
        inventoryPassives.Clear(); 
    }
    // --- 接口实现 ---
    
    public void AddTemporaryPassive(PassiveEffect effect, RuntimeItem source)
    {
        inventoryPassives.Add(new PassiveContext(effect, source));
    }

    public void RemovePassive(PassiveEffect effect)
    {
        // 如果需要移除，通常是根据 Effect 的引用来删
        inventoryPassives.RemoveAll(ap => ap.effect == effect);
    }

    public IEnumerable<PassiveContext> GetSourcePassives()
    {
        return Enumerable.Empty<PassiveContext>();
    }

    /// <summary>
    /// 初始化原生被动 (从 RuntimeItem 读取)
    /// </summary>
    public void InitializePassives(RuntimeItem source)
    {
        nativePassives.Clear();
        if (source == null || source.permanentPassives == null) return;

        foreach (var p in source.permanentPassives)
        {
            if (p != null)
            {
                nativePassives.Add(p);
                // 顺便触发一下 OnApply (如果有的话)
                var ctx = new EffectContext(ownerCharacter, null, source);
                // p.OnApply(ctx); // 如果 Passive 有 OnApply 钩子
            }
        }
    }

    public IEnumerable<PassiveContext> GetActivePassives()
    {
        // ✨ 优化：使用 yield return 避免每次调用都创建 new List (GC Friendly)
        
        // 1. 来自背包的被动 (通过 InventoryManager 注入)
        if (inventoryPassives != null)
        {
            foreach (var p in inventoryPassives)
            {
                yield return p;
            }
        }

        // 2. 原生被动 (比如永恒骑士的亡语是自带的)
        if (nativePassives != null)
        {
            foreach (var p in nativePassives)
            {
                yield return new PassiveContext(p, ownerCharacter.sourceRuntimeItem);
            }
        }
    }
    // 模拟数据更新（比如在初始化或状态改变时调用）
    private bool _isUIStateDirty = false;
    private void LateUpdate()
    {
        if (_isUIStateDirty)
        {
            DoRefreshTooltips();
            _isUIStateDirty = false;
        }
    }

    public void RefreshTooltips() 
    {
        // 标记为脏，下一帧由于 LateUpdate 统一刷新
        _isUIStateDirty = true;
    }

    private void DoRefreshTooltips()
    {
        if (statusProvider != null) statusProvider.currentEntries = GetCurrentStatusData(); 
        if (auraProvider != null) auraProvider.currentEntries = GetCurrentAuraData();
    }

    private List<TooltipAllData> GetCurrentStatusData()
    {
        var subs = new List<TooltipAllData>();
        if (statusList == null) return subs;

        // ✨ 修复：使用 GroupBy 聚合相同的 Buff (防御性 UI 编程)
        // 理论上 ApplyStatus 已经保证了逻辑唯一性，但 UI 再聚合一次也没坏处
        var groups = statusList.GroupBy(s => s.Data);

        foreach (var group in groups)
        {
            var status = group.First(); // 取第一个
            /*if (status == null || status.Data == null) continue;
            
            // 注意：Buff 的层数本身由 Logic 决定，我们这里只显示聚合后的效果
            subs.Add(new TooltipAllData {
                title = $"<color=#{ColorUtility.ToHtmlStringRGB(Color.blueViolet)}>{status.Data.name}</color>",
                content = status.GetParsedDescription()
            });*/
            //修改：不聚合，因为buff本身就是唯一的
             subs.Add(new TooltipAllData {
                 title = $"<color=#{ColorUtility.ToHtmlStringRGB(Color.blueViolet)}>{status.Data.name}</color>",
                 content = status.GetParsedDescription()
             });
        }
        
        return subs;
    }

    private List<TooltipAllData> GetCurrentAuraData()
    {
        var subs = new List<TooltipAllData>();
        var activePassives = GetActivePassives();
        if (activePassives == null) return subs;

        // ✨ 修复：使用 GroupBy 聚合相同的被动
        // 这样如果我有 3 个 "Fire Aura"，UI 只会显示一个 "Fire Aura (x3)"
        var groups = activePassives.GroupBy(ctx => ctx.effect);

        foreach (var group in groups)
        {
            var effect = group.Key;
            if (effect == null) continue;

            int count = group.Count();
            string countSuffix = (count > 1) ? $" x{count}" : "";

            subs.Add(new TooltipAllData {
                title = $"<color=#{ColorUtility.ToHtmlStringRGB(Color.aquamarine)}>{effect.name}{countSuffix}</color>",
                content = effect.description
            });
        }

        return subs;
    }

    
}