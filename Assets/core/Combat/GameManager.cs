using System;
using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour
{
    // 单例模式：让任何地方都能找到 GameManager
    public static GameManager Instance;
    // 定义事件：法力值变动事件
    public event Action OnManaChanged;
    [Header("状态标记")]
    public bool isPlayerTurn = true; // 关键标记：防止玩家在敌人回合乱动
    // 这是一个信号旗
    private bool _playerWantsToEnd = false;
    // 防止玩家在敌人回合或动画播放时乱按按钮
    public bool IsPlayerTurn { get; private set; } = false;
    
    [Header("游戏资源")]
    public int maxMana = 3;       // 回合上限
    public int currentMana;       // 当前剩余
    public int maxManaCap = 10; // 游戏规则允许的最大水晶数 (比如炉石是10)
    public TextMeshProUGUI manaText; // 拖入场景里的 UI
    
    [Header("角色引用")]
    public CharacterBase player;
    public CharacterStateManager playerState;
    
    private readonly List<CharacterBase> enemies = new();
    private readonly List<CharacterBase> allies = new();
    private HashSet<CharacterBase> allUnits = new();
    
    public IEnumerable<CharacterBase> AllUnits =>allUnits;
    public IEnumerable<CharacterBase> Allies => allies;
    public IEnumerable<CharacterBase> Enemies => enemies;
    
    public bool IsBattleActive { get; private set; } = false;
    // 友方召唤物列表 (如果你想召唤帮手)
    [Header("生成点 (位置)")]
    public Transform enemySpawnZone; // 敌人在哪生成？
    public Transform allySpawnZone;  // 友军在哪生成？

    public List<PlayRule> globalPlayRules = new List<PlayRule>();

    // ✨ 全局计数器 (用于永恒骑士、冰雪投球手等机制)
    // Key: "EternalKnightDeaths", "TosserBuffLevel"
    public Dictionary<string, int> globalCounters = new Dictionary<string, int>();

    public int GetGlobalCounter(string key)
    {
        return globalCounters.GetValueOrDefault(key, 0);
    }

    public void ModifyGlobalCounter(string key, int amount)
    {
        if (!globalCounters.TryAdd(key, amount))
            globalCounters[key] += amount;

        Debug.Log($"[Global] {key} changed to {globalCounters[key]}");
        
        // 当全局变量改变时，可能所有东西的面板都要刷新
        OnBoardChanged(); // 稍微有点重，但如果有 UI 显示“本局死亡数”是有必要的
    }
    private void Awake()
    {
        // 🎯 每场景独立的 GameManager（不使用 DontDestroyOnLoad）
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // 💡 如果 RunManager 已创建了 player，使用它
        if (RunManager.Instance != null && RunManager.Instance.GetCurrentPlayer() != null)
        {
            player = RunManager.Instance.GetCurrentPlayer();
        }
    }
    // 2. 监听场景加载事件
    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 这里不需要 yield return null，因为 OnSceneLoaded 触发时，
        // 场景里的 Awake/Start 通常已经跑完了（或者正在跑，时序比较安全）
        
        // 但为了保险，也可以开协程
        StartCoroutine(InitSceneRoutine());
    }
  
    IEnumerator InitSceneRoutine()
    {
        yield return null; // 等一帧，确保新场景里的怪都 Awake 好了

        // A. 从 RunManager 获取 player
        player = RunManager.Instance?.GetCurrentPlayer();
        playerState = player?.GetComponent<CharacterStateManager>();
        
        // B. 找到所有敌人并注册
        var units = FindObjectsByType<CharacterBase>(FindObjectsSortMode.None);
        foreach (var unit in units)
        {
            RegisterUnit(unit);
        }

        StartCoroutine(BattleLoop());// 游戏开始
    }


    // --- 4. 核心战斗循环 (协程流) ---
    private IEnumerator BattleLoop()
    {
        // A. 等待阶段：等一帧，确保所有 CharacterBase 的 OnEnable 都跑完，都注册好了
        yield return null;

        IsBattleActive = true;

        // ✨ 战斗开始设置（只执行一次）
        yield return StartCoroutine(BattleSetup());

        // B. 循环阶段：只要战斗没结束，就一直轮流走
        while (IsBattleActive)
        {
            // ==========================================
            // 🔵 阶段 1: 玩家回合开始 (Start)
            // ==========================================
            IsPlayerTurn = true; // 允许玩家操作卡牌
            _playerWantsToEnd = false; // 重置信号旗

            // 触发 "回合开始" 特效 (抽牌、回费、Buff结算)
            yield return StartCoroutine(StartPlayerTurn()); 

            // ==========================================
            // ⏸️ 阶段 2: 等待玩家操作 (Wait)
            // ==========================================
        
            // ✨✨✨ 核心魔法 ✨✨✨
            // 协程会在这里卡住，直到 _playerWantsToEnd 变成 true
            // 期间玩家可以打牌、攻击，干任何事
            yield return new WaitUntil(() => _playerWantsToEnd);

            // ==========================================
            // 🟠 阶段 3: 玩家回合结束结算 (Ending)
            // ==========================================

            isPlayerTurn = false; // 锁住玩家操作 (这时候不能再打牌了)

            // 执行原本 PlayerTurnEnding 的逻辑
            // 比如：丢弃手牌、触发 "回合结束" 的 Buff (毒、甚至是你的光环减层)
            yield return StartCoroutine(PlayerTurnEnding());

            // ==========================================
            // 🔴 阶段 4: 敌人回合
            // ==========================================
            yield return StartCoroutine(EnemyTurn());
        }
    }
    
    // ✨ 战斗开始设置（只在战斗开始时执行一次）
    private IEnumerator BattleSetup()
    {
        Debug.Log("[GameManager] === Battle Setup ===");
        
        // 1. 初始化牌堆
        if (HandManager.Instance != null)
        {
            HandManager.Instance.InitializeDeck();
            
            // 2. 抽起始手牌（固有牌优先）
            HandManager.Instance.DrawStartingHand(player);
        }
        
        // 3. 触发 "战斗开始" 事件
        GameEvents.TriggerBattleStart();
        
        // 4. 给玩家看一眼起始手牌
        yield return new WaitForSeconds(0.5f);
    }
    
    
    // --- 阶段 1: 玩家回合开始 ---
    public IEnumerator StartPlayerTurn() 
    {
        isPlayerTurn = true;
        // 1. 回费
        ResetMana();
        
        // ✨ 1. 触发玩家身上的 Buff (中毒、自动抽牌)
        
        if (playerState != null) playerState.OnTurnStart();

        // 2. 触发场上【友方】随从的“回合开始”效果 (扳机行为)
        // 使用副本遍历防止在执行效果时列表被修改导致报错
        var currentAllies = new List<CharacterBase>(allies);
        foreach (var ally in currentAllies) {
            if (ally != null && ally.currentHealth > 0) {
                // 假设你在 CharacterBase 或其关联的 CardDefinition 中存了效果
                // 这里我们调用一个统一的触发方法
                
                yield return StartCoroutine(TriggerUnitEffects(ally, true)); 
            }
        }
        OnBoardChanged();
        
        // 3. 触发回合开始事件
        GameEvents.TriggerPlayerTurnStart();
                            
        // 4. 每回合抽 1 张牌
        HandManager.Instance.DrawCard(player);
        
        yield return null;
    }
    
    // --- 阶段 2: 玩家点击结束回合 (绑定给按钮) ---
    public void OnEndTurnButton() {
        // 1. 安全检查：如果不是玩家回合，或者已经点过了，就无视
        if (!IsPlayerTurn || _playerWantsToEnd) return;

        Debug.Log("🖱️ 玩家点击了结束回合");

        // 2. 举起信号旗
        _playerWantsToEnd = true;
    
        // 可选：在这里把按钮变灰 (Interactable = false)，给玩家视觉反馈
    }
    private IEnumerator PlayerTurnEnding()
    {
        // 1. 触发玩家身上的 Buff 回合结束逻辑 (移除临时Buff)
        if (playerState != null) playerState.OnTurnEnd();

        // 2. 触发场上【友方】随从的“回合结束”效果
        var alliesSnapshot = Allies.ToList();
        foreach (var ally in alliesSnapshot) {
            if (ally == null || ally.currentHealth <= 0) continue;
            yield return StartCoroutine(TriggerUnitEffects(ally, false));
            
        }
        
        OnBoardChanged();//回合开始场景：有些特殊的随从效果是“在你的回合开始时，获得攻击力”。
        // 3. 进入敌人回合
        yield return new WaitForSeconds(0.5f);
    }

    // --- 阶段 3: 敌人回合 ---
    private IEnumerator EnemyTurn() 
    {
        // A. 敌人回合开始 (触发各种 Start Buffs)
        var enemySnapshot = enemies.ToList();
        foreach (var enemy in enemySnapshot)
        {
            if (enemy == null || enemy.currentHealth <= 0) continue;
            
            // 1. 触发 Buff (如 Burn DOT)
            if (enemy.stateManager != null) enemy.stateManager.OnTurnStart();
            
            // 2. 触发卡牌定义的 Start 效果
            yield return StartCoroutine(TriggerUnitEffects(enemy, true));
        }
        OnBoardChanged();
        
        yield return new WaitForSeconds(0.5f); // 停顿一下

        // B. 敌人行动阶段
        // 重新获取快照，因为 Start 阶段可能有人挂了
        enemySnapshot = enemies.ToList(); 
        foreach (var enemy in enemySnapshot)
        {
            if (enemy == null || enemy.currentHealth <= 0) continue;
            
            enemy.DoTurnAction();
            // 每一个敌人动完停一下，防止瞬间爆发看不清
            yield return new WaitForSeconds(0.3f); 
        }

        // C. 敌人回合结束 (触发 End Buffs)
        enemySnapshot = enemies.ToList();
        foreach (var enemy in enemySnapshot)
        {
            if (enemy == null || enemy.currentHealth <= 0) continue;

            // 1. 触发 Buff (如移除临时状态)
            if (enemy.stateManager != null) enemy.stateManager.OnTurnEnd();

            // 2. 触发卡牌定义的 End 效果
            yield return StartCoroutine(TriggerUnitEffects(enemy, false));
        }
        OnBoardChanged();

        // --- 回合结束，把控制权还给玩家 ---
        // 注意：这里不需要手动调 StartPlayerTurn，因为 BattleLoop 的 while 循环会自动回到 StartPlayerTurn
        // StartCoroutine(StartPlayerTurn());
    }

    private void ResetMana()
    {
        currentMana = maxMana;
        UpdateManaUI();
        OnManaChanged?.Invoke();
    }
    
    public void ModifyMana(int amount, bool allowOverflow = false)
    {
        currentMana += amount;
        
        // 如果不允许溢出 (常规回蓝)，则不能超过当前的上限
        if (!allowOverflow)
        {
            if (currentMana > maxMana) currentMana = maxMana;
        }
        
        // 永远不能小于 0
        if (currentMana < 0) currentMana = 0;
        
        
        // 3. 广播：钱包变了！
        UpdateManaUI(); // 刷新左下角的法力水晶条
        OnManaChanged?.Invoke(); // 通知手牌里的卡
    }
    
    // --- 2. 修改法力上限 (跳费) ---
    public void ModifyMaxMana(int amount)
    {
        maxMana += amount;

        // 限制不能超过游戏规则的总上限 (比如 10 费)
        if (maxMana > maxManaCap) maxMana = maxManaCap;

        // 广播事件：UI 刷新 (你需要确保 UpdateManaUI 能处理 maxMana 的变化)
        UpdateManaUI();
        OnManaChanged?.Invoke();
    }
    private void UpdateManaUI()
    {
        if (manaText != null) {
            manaText.text = $"Mana: {currentMana}/{maxMana}";
        }
    }
    
    // --- 辅助方法：统一处理随从身上的效果队列 ---
    private IEnumerator TriggerUnitEffects(CharacterBase unit, bool isStartOfTurn)
    {
        var data = unit.cardData; // unit.cardData 就是我们在 Initialize 时塞进去的那张卡
        if (data == null) yield break;

        var effects = isStartOfTurn ? data.onTurnStartEffects : data.onTurnEndEffects;
        var prefix = isStartOfTurn ? "OnTurnStart" : "OnTurnEnd";
        
        var ctx = new EffectContext(unit, null, unit.sourceRuntimeItem);
        
        for (int i = 0; i < effects.Count; i++)
        {
            var effect = effects[i];
            
            // 在效果执行前，让随从抖一下或者发个光
            unit.transform.DOPunchScale(Vector3.one * 1.1f, 0.2f);
            
            // ✨ 设置精确的快照 key
            ctx.snapshotKey = $"{prefix}_{effect.GetType().Name}_{i}";
            
            // 执行效果并等待动画时间
            var waitTime = effect.Execute(ctx); 
            if (waitTime > 0) yield return new WaitForSeconds(waitTime);
        }
        var activePassives = unit.stateManager.GetActivePassives();
        foreach (var context in activePassives) 
        {
            if (isStartOfTurn)
            {
                // 触发回合开始钩子
                context.effect.OnTurnStart(unit, context.source);
            }
            else
            {
                // 触发回合结束钩子
                context.effect.OnTurnOver(unit, context.source);
            }
        }

        
    }

    // --- 核心逻辑：打出一张牌 ---
    private IEnumerator PlayCardRoutine(EffectContext ctx)
    {
        var card = ctx.SourceCard;
        var totalCasts = 1 + ctx.repeatCount;
        
        // 执行所有"打出时"的效果
        // 关键点：这里是一个循环，不管你有 1 个技能还是 10 个技能，都会依次执行
        for (var i = 0; i < totalCasts; i++)
        {
            // ✨ 使用 for 循环以便生成精确的 Key
            for (int j = 0; j < card.onPlayEffects.Count; j++)
            {
                var effect = card.onPlayEffects[j];
                ctx.snapshotKey = $"OnPlay_{effect.GetType().Name}_{j}";

                var waitTime = effect.Execute(ctx);
            
                // 兼容模式：如果效果返回了等待时间（没有使用 ActionManager）
                if (waitTime > 0)
                {
                    yield return new WaitForSeconds(waitTime);
                }
            }
        }
        
        // ✨ 等待 ActionManager 处理完所有排队的动作
        if (ActionManager.Instance != null)
        {
            yield return ActionManager.Instance.WaitForQueueEmpty();
        }
        
        OnBoardChanged();
    }
    
    public bool PlayCard(RuntimeItem runtimeItem, CharacterBase target)
    {
        
        if (runtimeItem == null) return false;
        
        // 先查“国法” (全局规则：有没有费，有没有被沉默)
        foreach (var rule in globalPlayRules)
        {
            var error = rule.Check(runtimeItem, target);
            if (error == null) continue;
            return false;
        }
        
        // 再查“家规” (卡牌自己的特殊要求)
        // 比如斩杀卡配置了一个 TargetMustBeDamagedRule
        if (runtimeItem.data.customRequirements != null)
        {
            foreach (var rule in runtimeItem.data.customRequirements)
            {
                var error = rule.Check(runtimeItem, target);
                if (error != null) {  return false; }
            }
        }

        UseMana(GetModifiedCost(runtimeItem));
        
        HandManager.Instance.OnCardPlayed(runtimeItem);
        
        var caster = runtimeItem.owner;
        // 构建上下文
        var ctx = new EffectContext(caster, target, runtimeItem);
        
        // 【介入阶段】全局光环/被动触发 (Passive Effects)
        foreach (var unit in AllUnits)
        {
            // 过滤死人
            if (unit == null || unit.currentHealth <= 0) continue;

            // 检查该单位是否有被动技能配置
            
            
            if (unit.stateManager == null) continue;
            
            foreach (var passiveContext in unit.stateManager.GetActivePassives())
            {
                if (passiveContext.effect != null && passiveContext.effect.ShouldTrigger(unit, caster))
                {
                    // 触发钩子！
                    // 比如 DoubleBattlecryPassive 会在这里把 ctx.repeatCount += 1
                    passiveContext.effect.OnPlayCard(unit, passiveContext.source, ctx);
                }
            }
            
        }
        
        // 4. ✨ 核心：触发 "OnPlayCard" 钩子
        // 这里会自动处理：双倍施法叠加次数、减费Buff消耗
        var stateManager = caster.stateManager;
        if (stateManager != null)
        {
            stateManager.OnPlayCard(ctx);
        }

        // ✨ 全局事件：卡牌打出
        GameEvents.TriggerCardPlayed(runtimeItem, caster, target);
        
        StartCoroutine(PlayCardRoutine(ctx));
        return true;
    }
    



    // ✨ 核心辅助方法：输入基础伤害，输出最终伤害
    public int GetModifiedDamage(RuntimeItem item, int baseDamage)
    {
        var stateManager = item.owner.stateManager;
    
        var finalDamage = baseDamage;
        // 如果没有状态管理器，直接返回基础值
        if (stateManager == null) return baseDamage;
        

        return stateManager.GetModifiedStats(finalDamage, item.data.type);

    }
    
    // 获取某张卡当前的实际费用
    public int GetModifiedCost(RuntimeItem item)
    {
        var stateManager = item.owner.stateManager;
        var cost = item.manaCost;
        if (stateManager != null)
        {
            cost = stateManager.GetCalculatedCost(item);
        }

        return Mathf.Max(0, cost);
    }
    
     // --- 尝试扣费 ---
     private void UseMana(int amount)
     {
         currentMana -= amount;
         UpdateManaUI();
         OnManaChanged?.Invoke();
         GameEvents.TriggerManaChanged(currentMana, maxMana);
     }
    
    
     public void RegisterUnit(CharacterBase unit)
     {
         if (unit == null || !allUnits.Add(unit)) return;

         if (unit.isEnemy) enemies.Add(unit);
         else allies.Add(unit);
     }

     public void UnregisterUnit(CharacterBase unit)
     {
         if (unit == null || !allUnits.Remove(unit)) return;

         if (unit.isEnemy) enemies.Remove(unit);
         else allies.Remove(unit);

         CheckBattleStatus(); // 判胜负
     }
    public CharacterBase GetRandomEnemy(bool includeDead = false)
    {
        // 1. 过滤符合条件的单位
        var candidates = includeDead ? Enemies : Enemies.Where(u => !u.isDead);
    
        // 2. 转为快照（List）以便快速索引
        var list = candidates.ToList();
    
        return list.Count == 0 ? null : list[Random.Range(0, list.Count)];
    }    
    /// <summary>
    /// 获取场上随机一个单位
    /// </summary>
    /// <param name="includeDead">是否包含已死亡但未销毁的单位</param>
    public CharacterBase GetRandomUnit(bool includeDead = false)
    {
        // 1. 过滤符合条件的单位
        var candidates = includeDead ? AllUnits : AllUnits.Where(u => !u.isDead);
    
        // 2. 转为快照（List）以便快速索引
        var list = candidates.ToList();
    
        return list.Count == 0 ? null : list[Random.Range(0, list.Count)];
    } 
    public CharacterBase GetRandomAllies(bool includeDead = false)
    {
        // 1. 过滤符合条件的单位
        var candidates = includeDead ? Allies : Allies.Where(u => !u.isDead);
    
        // 2. 转为快照（List）以便快速索引
        var list = candidates.ToList();
    
        return list.Count == 0 ? null : list[Random.Range(0, list.Count)];
    }

    // 全局刷新：只要场面变了就调用它
    public void OnBoardChanged()
    {
        // ⚠️ 关键修复：必须遍历副本！
        // 因为 RefreshStats() 可能会导致单位死亡 (Die -> UnregisterUnit)，从而修改 allUnits 集合。
        // 如果直接便利 AllUnits 会报 InvalidOperationException。
        var snapshot = AllUnits.ToList(); 
        
        // 让每个人重新算一遍属性
        foreach (var unit in snapshot)
        {
            // 双重检查：防止在这次循环中前面的操作已经把它销毁了（虽然不太可能，但保险）
            if(unit != null && !unit.isDead) 
                unit.RefreshStats();
        }
        OnManaChanged?.Invoke();
    }
    
    private void CheckBattleStatus()
    {
        if (!IsBattleActive) return;

        // 胜利：所有敌人死亡
        if (enemies.Count == 0) 
        {
            IsBattleActive = false;
            Debug.Log("[GameManager] Victory!");
            
            // 如果是在 Run 模式下，通知 RunManager 完成节点
            if (RunManager.Instance != null)
            {
                RunManager.Instance.CompleteCurrentNode();
            }
        }

        // 失败：玩家/所有盟友死亡
        if (allies.Count == 0) 
        {
            IsBattleActive = false;
            Debug.Log("[GameManager] Defeat!");
            
            // 这里可以跳转到 Game Over 场景或显示结算
            // SceneManager.LoadScene("GameOver");
        }
    }


}