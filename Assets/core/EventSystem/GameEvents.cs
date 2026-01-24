using System;
using UnityEngine;

/// <summary>
/// 全局事件中心 - 集中管理所有游戏事件
/// 使用静态类避免单例管理，任何地方都可以直接订阅/触发
/// </summary>
public static class GameEvents
{
    // ==========================================================
    // 战斗相关事件
    // ==========================================================
    
    /// <summary>
    /// 造成伤害时触发 (伤害来源, 目标, 伤害值)
    /// </summary>
    public static event Action<CharacterBase, CharacterBase, int> OnDamageDealt;
    
    /// <summary>
    /// 受到伤害时触发 (目标, 伤害来源, 伤害值)
    /// </summary>
    public static event Action<CharacterBase, CharacterBase, int> OnDamageTaken;
    
    /// <summary>
    /// 单位死亡时触发 (死亡的单位, 击杀者)
    /// </summary>
    public static event Action<CharacterBase, CharacterBase> OnUnitDied;
    
    /// <summary>
    /// 治疗时触发 (目标, 治疗量)
    /// </summary>
    public static event Action<CharacterBase, int> OnHeal;
    
    /// <summary>
    /// 获得护甲时触发 (目标, 护甲值)
    /// </summary>
    public static event Action<CharacterBase, int> OnArmorGained;
    
    // ==========================================================
    // 卡牌相关事件
    // ==========================================================
    
    /// <summary>
    /// 打出卡牌时触发 (卡牌, 施法者, 目标)
    /// </summary>
    public static event Action<RuntimeItem, CharacterBase, CharacterBase> OnCardPlayed;
    
    /// <summary>
    /// 抽牌时触发 (卡牌, 抽牌者)
    /// </summary>
    public static event Action<RuntimeItem, CharacterBase> OnCardDrawn;
    
    /// <summary>
    /// 弃牌时触发 (卡牌)
    /// </summary>
    public static event Action<RuntimeItem> OnCardDiscarded;
    
    /// <summary>
    /// 消耗牌时触发 (卡牌被永久移除)
    /// </summary>
    public static event Action<RuntimeItem> OnCardExhausted;
    
    // ==========================================================
    // 资源相关事件
    // ==========================================================
    
    /// <summary>
    /// 法力值变化时触发 (当前法力, 最大法力)
    /// </summary>
    public static event Action<int, int> OnManaChanged;
    
    /// <summary>
    /// 卡组变化时触发 (用于卡组 UI 刷新)
    /// </summary>
    public static event Action OnDeckChanged;
    
    /// <summary>
    /// 卡牌被修改时触发 (升级、词缀变化等)
    /// </summary>
    public static event Action<RuntimeItem> OnCardModified;
    
    /// <summary>
    /// 弃牌堆洗回抽牌堆时触发
    /// </summary>
    public static event Action OnDeckReshuffled;
    
    /// <summary>
    /// 手牌变化时触发
    /// </summary>
    public static event Action OnHandChanged;
    
    // ==========================================================
    // 状态相关事件
    // ==========================================================
    
    /// <summary>
    /// Buff 施加时触发 (目标, Buff数据, 层数)
    /// </summary>
    public static event Action<CharacterBase, StatusEffect, int> OnBuffApplied;
    
    /// <summary>
    /// Buff 移除时触发 (目标, Buff数据)
    /// </summary>
    public static event Action<CharacterBase, StatusEffect> OnBuffRemoved;
    
    // ==========================================================
    // 回合相关事件
    // ==========================================================
    
    /// <summary>
    /// 玩家回合开始
    /// </summary>
    public static event Action OnPlayerTurnStart;
    
    /// <summary>
    /// 玩家回合结束
    /// </summary>
    public static event Action OnPlayerTurnEnd;
    
    /// <summary>
    /// 敌人回合开始
    /// </summary>
    public static event Action OnEnemyTurnStart;
    
    /// <summary>
    /// 敌人回合结束
    /// </summary>
    public static event Action OnEnemyTurnEnd;
    
    // ==========================================================
    // 战斗流程事件
    // ==========================================================
    
    /// <summary>
    /// 战斗开始
    /// </summary>
    public static event Action OnBattleStart;
    
    /// <summary>
    /// 战斗胜利
    /// </summary>
    public static event Action OnBattleWon;
    
    /// <summary>
    /// 战斗失败
    /// </summary>
    public static event Action OnBattleLost;
    
    // ==========================================================
    // 单位相关事件
    // ==========================================================
    
    /// <summary>
    /// 单位生成时触发 (生成的单位)
    /// </summary>
    public static event Action<CharacterBase> OnUnitSpawned;
    
    // ==========================================================
    // 触发方法 (Invoke Methods)
    // ==========================================================
    
    public static void TriggerDamageDealt(CharacterBase source, CharacterBase target, int damage)
    {
        OnDamageDealt?.Invoke(source, target, damage);
    }
    
    public static void TriggerDamageTaken(CharacterBase target, CharacterBase source, int damage)
    {
        OnDamageTaken?.Invoke(target, source, damage);
    }
    
    public static void TriggerUnitDied(CharacterBase unit, CharacterBase killer)
    {
        OnUnitDied?.Invoke(unit, killer);
    }
    
    public static void TriggerHeal(CharacterBase target, int amount)
    {
        OnHeal?.Invoke(target, amount);
    }
    
    public static void TriggerArmorGained(CharacterBase target, int amount)
    {
        OnArmorGained?.Invoke(target, amount);
    }
    
    public static void TriggerCardPlayed(RuntimeItem card, CharacterBase caster, CharacterBase target)
    {
        OnCardPlayed?.Invoke(card, caster, target);
    }
    
    public static void TriggerCardDrawn(RuntimeItem card, CharacterBase owner)
    {
        OnCardDrawn?.Invoke(card, owner);
    }
    
    public static void TriggerCardDiscarded(RuntimeItem card)
    {
        OnCardDiscarded?.Invoke(card);
    }
    
    public static void TriggerCardExhausted(RuntimeItem card)
    {
        OnCardExhausted?.Invoke(card);
    }
    
    public static void TriggerManaChanged(int current, int max)
    {
        OnManaChanged?.Invoke(current, max);
    }
    
    public static void TriggerDeckChanged()
    {
        OnDeckChanged?.Invoke();
    }
    
    public static void TriggerCardModified(RuntimeItem card)
    {
        OnCardModified?.Invoke(card);
    }
    
    public static void TriggerDeckReshuffled()
    {
        OnDeckReshuffled?.Invoke();
    }
    
    public static void TriggerHandChanged()
    {
        OnHandChanged?.Invoke();
    }
    
    public static void TriggerBuffApplied(CharacterBase target, StatusEffect buff, int stacks)
    {
        OnBuffApplied?.Invoke(target, buff, stacks);
    }
    
    public static void TriggerBuffRemoved(CharacterBase target, StatusEffect buff)
    {
        OnBuffRemoved?.Invoke(target, buff);
    }
    
    public static void TriggerPlayerTurnStart()
    {
        OnPlayerTurnStart?.Invoke();
    }
    
    public static void TriggerPlayerTurnEnd()
    {
        OnPlayerTurnEnd?.Invoke();
    }
    
    public static void TriggerEnemyTurnStart()
    {
        OnEnemyTurnStart?.Invoke();
    }
    
    public static void TriggerEnemyTurnEnd()
    {
        OnEnemyTurnEnd?.Invoke();
    }
    
    public static void TriggerBattleStart()
    {
        OnBattleStart?.Invoke();
    }
    
    public static void TriggerBattleWon()
    {
        OnBattleWon?.Invoke();
    }
    
    public static void TriggerBattleLost()
    {
        OnBattleLost?.Invoke();
    }
    
    public static void TriggerUnitSpawned(CharacterBase unit)
    {
        OnUnitSpawned?.Invoke(unit);
    }
    
    // ==========================================================
    // 清理方法 (用于场景切换时)
    // ==========================================================
    
    /// <summary>
    /// 清除所有事件订阅（场景切换时调用，防止内存泄漏）
    /// </summary>
    public static void ClearAllListeners()
    {
        OnDamageDealt = null;
        OnDamageTaken = null;
        OnUnitDied = null;
        OnHeal = null;
        OnArmorGained = null;
        OnCardPlayed = null;
        OnCardDrawn = null;
        OnCardDiscarded = null;
        OnCardExhausted = null;
        OnManaChanged = null;
        OnDeckChanged = null;
        OnCardModified = null;
        OnDeckReshuffled = null;
        OnHandChanged = null;
        OnBuffApplied = null;
        OnBuffRemoved = null;
        OnPlayerTurnStart = null;
        OnPlayerTurnEnd = null;
        OnEnemyTurnStart = null;
        OnEnemyTurnEnd = null;
        OnBattleStart = null;
        OnBattleWon = null;
        OnBattleLost = null;
        OnUnitSpawned = null;
    }
}

