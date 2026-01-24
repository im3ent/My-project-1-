using System.Collections.Generic;
using UnityEngine;

[System.Serializable]

public class RuntimeItem :IPassiveContainer
{
    // 身份证 (永远指向原始数据，用于查图片、查描述、查原始费用)
    public CardDefinition data;
    // 归属者 (谁拿着这张牌？) - 不可序列化
    [System.NonSerialized] public CharacterBase owner;

    // 这些是从 Data 里抄过来的，但允许在战斗中随意修改
    public int manaCost;  // 当前费用
    public int attack;    // 当前攻击
    public int health;    // 当前血量

    public  RuntimeItem(CardDefinition data, CharacterBase owner)
    {
        this.data = data;
        this.owner = owner;
        manaCost = data.manaCost;
        attack = data.attack;
        health = data.health;

        // ✨ 自动初始化数值快照
        InitializeCustomValues();
        
        // ✨ 核心修正：将卡牌定义的被动复制到运行时列表
        // 这样统一管理，方便后续做"沉默"（移除被动）等操作
        if (this.data.passives != null)
        {
            permanentPassives.AddRange(this.data.passives);
        }
    }

                                                  
    // 4. 数据初始化钩子
    // ✨ 核心重构：支持存储多个 Buff 的快照
    // Key: Buff 的 ID (例如 "Burn", "Poison")
    public Dictionary<string, EffectSnapshot> initialSnapshots = new();
    
    // 旧的单个 Snapshot 字段废弃，提供一个向后兼容的属性 (返回第一个)
    public EffectSnapshot Snapshot 
    {
        get 
        {
            if (initialSnapshots.Count > 0)
            {
                foreach (var kvp in initialSnapshots) return kvp.Value;
            }
            return null;
        }
    }

    // ✨ 核心方法：获取或创建特定 Key 的快照 (用于存储条件解锁的计数器)
    public EffectSnapshot GetOrCreateSnapshot(string key)
    {
        if (!initialSnapshots.ContainsKey(key))
        {
            initialSnapshots[key] = new EffectSnapshot(); 
        }
        return initialSnapshots[key];
    }

    private void InitializeCustomValues()
    {
        initialSnapshots.Clear();
        
        // ✨ 辅助方法：扫描效果列表并生成快照
        void ScanEffectList(System.Collections.Generic.List<CardEffect> effects, string listPrefix)
        {
            if (effects == null) return;
            
            for (int i = 0; i < effects.Count; i++)
            {
                var effect = effects[i];
                if (effect == null) continue;
                
                // ✨ 新 Key 规则：列表名_类型名_索引，确保全局唯一
                string key = $"{listPrefix}_{effect.GetType().Name}_{i}";
                
                var snap = effect.GetInitialSnapshot(this);
                if (snap != null)
                {
                    initialSnapshots[key] = snap;
                    
                    // 特殊处理：ApplyBuffEffect 额外用 buffData.id 做别名 (保持兼容)
                    if (effect is ApplyBuffEffect applyBuff && 
                        applyBuff.buffData != null && 
                        !string.IsNullOrEmpty(applyBuff.buffData.id))
                    {
                        initialSnapshots[applyBuff.buffData.id] = snap;
                    }
                }
            }
        }
        
        // 1. 扫描 OnPlayEffects (主动卡)
        ScanEffectList(data.onPlayEffects, "OnPlay");
        
        // 2. 扫描 OnTurnStartEffects
        ScanEffectList(data.onTurnStartEffects, "OnTurnStart");
        
        // 3. 扫描 OnTurnEndEffects
        ScanEffectList(data.onTurnEndEffects, "OnTurnEnd");
        
        // 4. 扫描 Passives (光环卡/装备卡)
        if (data.passives != null)
        {
            for (int i = 0; i < data.passives.Count; i++)
            {
                var passive = data.passives[i];
                if (passive == null) continue;
                
                string key = $"Passive_{passive.GetType().Name}_{i}";
                
                var snap = passive.GetInitialSnapshot();
                if (snap != null)
                {
                    initialSnapshots[key] = snap;
                    
                    // 特殊处理：ItemGrantGuardPassive 额外用 guardBuff.id 做别名
                    if (passive is ItemGrantGuardPassive grantGuard && 
                        grantGuard.guardBuff != null &&
                        !string.IsNullOrEmpty(grantGuard.guardBuff.id))
                    {
                        initialSnapshots[grantGuard.guardBuff.id] = snap;
                    }
                }
            }
        }
    }

    // 甚至可以有：是否保密、是否被冻结...
    public bool isPassiveActive; 
    public float passiveMultiplier = 1f;

    public int level = 0;
    public void Upgrade() => level++;
    
    
    public List<PassiveEffect> permanentPassives = new ();  //如果这里要 Ctx，改 AddPermanentPassive
    // 动态列表 (专门存光环、附魔给的临时被动)
    private List<PassiveEffect> temporaryPassives = new();
    public void AddTemporaryPassive(PassiveEffect effect, RuntimeItem source)
    {
        // 允许叠加 (比如两个磨刀石都在旁边，就加两次)
        temporaryPassives.Add(effect);
    }
    public void AddPermanentPassive(PassiveEffect effect)
    {
        permanentPassives.Add(effect);
    }

    public void RemovePassive(PassiveEffect effect)
    {
        if (temporaryPassives.Contains(effect))
        {
            temporaryPassives.Remove(effect);
        }
    }
    
    public void ClearTemporaryPassives()
    {
        temporaryPassives.Clear();
        isPassiveActive = false; 
        passiveMultiplier = 1.0f;
    }
    // ✨ 合并逻辑：统一从 permanentPassives 读取
    public IEnumerable<PassiveContext> GetSourcePassives()
    {
        // 返回永久被动 (包含天生自带 + 后天获得)
        foreach (var p in permanentPassives)
        {
            yield return new PassiveContext(p, this);
        }
        
        // 3. (可选) 临时获得的被动能不能再次传导？
        
        // 3. (可选) 临时获得的被动能不能再次传导？
        // 比如 A 给 B 加了光环，B 因此又能给 C 加光环？
        // 如果允许"传导"，这里也要 yield return temporaryPassives
        // 如果不允许，就到此为止
    }
    public IEnumerable<PassiveContext> GetActivePassives()
    {  
        // 第一部分：所有源被动（天生+永久）
        foreach (var sourcePassive in GetSourcePassives())
        {
            yield return sourcePassive;
        }

        // 第二部分：别人给我的临时光环
        foreach (var tempPassive in temporaryPassives)
        {
            yield return new PassiveContext(tempPassive, this);
        }
    }
}


public interface IPassiveContainer
{
    // 动态添加一个被动（比如光环给的，或者药水给的）
    void AddTemporaryPassive(PassiveEffect effect, RuntimeItem source);

    // 移除一个被动
    void RemovePassive(PassiveEffect effect);

    // ✨ 核心：获取当前所有生效的被动
    // 使用 IEnumerable 是为了能用 foreach 遍历，同时保护内部 List 不被直接修改
    IEnumerable<PassiveContext> GetSourcePassives();
    IEnumerable<PassiveContext> GetActivePassives();
}