using System;
using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour
{
    // 单例模式：让任何地方都能找到 GameManager
    public static GameManager Instance;
    // 定义事件：法力值变动事件
    public event Action OnManaChanged;
    [Header("状态标记")]
    public bool isPlayerTurn = true; // 关键标记：防止玩家在敌人回合乱动
    
    [Header("游戏资源")]
    public int maxMana = 3;       // 回合上限
    public int currentMana;       // 当前剩余
    public int maxManaCap = 10; // 游戏规则允许的最大水晶数 (比如炉石是10)
    public TextMeshProUGUI manaText; // 拖入场景里的 UI
    
    [Header("角色引用")]
    public CharacterBase player;
    public List<CharacterBase> enemies = new();
    public List<CharacterBase> allies = new();
    public List<CharacterBase> allUnits = new();
    // 友方召唤物列表 (如果你想召唤帮手)
    [Header("生成点 (位置)")]
    public Transform enemySpawnZone; // 敌人在哪生成？
    public Transform allySpawnZone;  // 友军在哪生成？
    private void Awake()
    {
        Instance = this;
    }

    private void Start() {
        // 游戏开始，直接进入玩家回合
        StartCoroutine(StartPlayerTurn());
    }

    // --- 阶段 1: 玩家回合开始 ---
    private IEnumerator StartPlayerTurn() 
    {
        isPlayerTurn = true;
        // 1. 回费
        ResetMana();
        
        // ✨ 1. 触发玩家身上的 Buff (中毒、自动抽牌)
        var playerState = player.GetComponent<CharacterStateManager>();
        if (playerState != null) playerState.OnTurnStart();
        //??
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
        OnBoardChanged();//回合开始场景：有些特殊的随从效果是“在你的回合开始时，获得攻击力”。
                            
        // 3. 系统强制抽牌 (抽牌通常在扳机之后)
        HandManager.Instance.DrawCard(player);
        
        yield return null;
    }
    
    // --- 阶段 2: 玩家点击结束回合 (绑定给按钮) ---
    public void OnEndTurnButton() {
        if (!isPlayerTurn) return; // 防止狂点

        // 进入敌人回合
        StartCoroutine(PlayerTurnEnding());
    }
    private IEnumerator PlayerTurnEnding()
    {
        // 1. 触发玩家身上的 Buff 回合结束逻辑 (移除临时Buff)
        var playerState = player.GetComponent<CharacterStateManager>();
        if (playerState != null) playerState.OnTurnEnd();
        //??
        // 2. 触发场上【友方】随从的“回合结束”效果
        var currentAllies = new List<CharacterBase>(allies);
        foreach (var ally in currentAllies) {
            if (ally != null && ally.currentHealth > 0) {
                yield return StartCoroutine(TriggerUnitEffects(ally, false));
            }
        }
        
        OnBoardChanged();//回合开始场景：有些特殊的随从效果是“在你的回合开始时，获得攻击力”。
        // 3. 进入敌人回合
        StartCoroutine(EnemyTurn());
    }

    // --- 阶段 3: 敌人回合 ---
    private IEnumerator EnemyTurn() {
        isPlayerTurn = false;
        // 循环让每一个活着的敌人行动
        // 为了防止列表在遍历时被修改（比如自爆怪），最好用副本或倒序
        foreach (var currentEnemy in enemies)
        {
            if (currentEnemy.currentHealth > 0) {
                // 模拟思考
                yield return new WaitForSeconds(1.0f);

                // 让这个敌人执行它的行动
                // (建议把攻击逻辑写在 Enemy.cs 里，这里只调用)
                currentEnemy.DoTurnAction(); 
            }
        }
        //OnBoardChanged();//回合开始场景：有些特殊的随从效果是“在你的回合开始时，获得攻击力”。
        yield return new WaitForSeconds(0.5f);
        StartCoroutine(StartPlayerTurn());
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
        var ctx = new EffectContext(unit, null, unit.sourceRuntimeCard);
        foreach (var effect in effects) 
        {
            // 在效果执行前，让随从抖一下或者发个光
            unit.transform.DOPunchScale(Vector3.one * 1.1f, 0.2f);
            // 执行效果并等待动画时间
            var waitTime = effect.Execute(ctx); 
            if (waitTime > 0) yield return new WaitForSeconds(waitTime);
        }
    }

    // --- 核心逻辑：打出一张牌 ---
    private IEnumerator PlayCardRoutine(EffectContext ctx)
    {
        var card = ctx.SourceCard;
        int totalCasts = 1 + ctx.repeatCount;
        // 执行所有“打出时”的效果
        // 关键点：这里是一个循环，不管你有 1 个技能还是 10 个技能，都会依次执行
        for (var i = 0; i < totalCasts; i++)
        {
            foreach (var effect in card.onPlayEffects)

            {
                var waitTime = effect.Execute(ctx);
            
                if (waitTime > 0)
                {
                    yield return new WaitForSeconds(waitTime);
                }
            }
        }
        // --- 2. 区分类型 ---
        if (card.cardType == CardType.Spell) {
            // A. 法术逻辑：
            // 效果已经在上面执行完了，现在只需要播放特效，然后销毁卡牌
            // (手牌的销毁逻辑通常在 CardDragHandler 里 Destroy(gameObject))
        }
        else if (card.cardType == CardType.Minion) {
            // B. 随从逻辑：
            // 战吼已经在上面执行完了，现在需要把随从召唤到场上
            //SpawnMinion(card); 
        }
        

        OnBoardChanged();
    }
    
    public void PlayCard(RuntimeCard runtimeCard, CharacterBase target)
    {
        var caster = runtimeCard.Owner;
        var cardData = runtimeCard.Data;

        var stateManager = caster.GetComponent<CharacterStateManager>();
        
        int finalCost = runtimeCard.Data.manaCost;
        if (stateManager != null) 
        {
            finalCost = stateManager.GetCalculatedCost(runtimeCard);
        }

        // 扣费逻辑...
        if (!TryUseMana(finalCost)) return; // 没钱直接退

        // 3. 构建上下文
        var ctx = new EffectContext(caster, target, runtimeCard);
    
        // 4. ✨ 核心：触发 "OnPlayCard" 钩子
        // 这里会自动处理：双倍施法叠加次数、减费Buff消耗
        if (stateManager != null)
        {
            stateManager.OnPlayCard(ctx);
        }

        
        StartCoroutine(PlayCardRoutine(ctx));
    }
    



    // ✨ 核心辅助方法：输入基础伤害，输出最终伤害
    public int GetModifiedDamage(RuntimeCard card, int baseDamage)
    {
        // 如果还没重构 CharacterStateManager，这里可能还是 GetComponent
        var stateManager = card.Owner.GetComponent<CharacterStateManager>();
        int finalDamage = baseDamage;

        // A. 先算人物属性加成 (力量/法强)
        if (stateManager != null)
        {
            finalDamage = stateManager.GetModifiedOutgoingDamage(finalDamage);
        
            // 如果法强是独立逻辑，也可以在这里单独加
            // if (card.Data.cardType == CardType.Spell) finalDamage += stateManager.GetSpellPower();
        }

        // B. 再算卡牌自身的动态加成 (这才是 RuntimeCard 的精髓！)
        // 假设 RuntimeCard 里有个 damageModifier 字段 (比如被附魔了)
        // finalDamage += card.damageModifier; 

        return finalDamage;
    }
    
    // 获取某张卡当前的实际费用
    public int GetModifiedCost(RuntimeCard card)
    {
        var stateManager = card.Owner.GetComponent<CharacterStateManager>();
        int cost = card.manaCost;
        if (stateManager != null)
        {
            cost = stateManager.GetCalculatedCost(card);
        }

        return Mathf.Max(0, cost);
    }
    
     // --- 尝试扣费 ---
    public bool TryUseMana(int cost) {
        if (currentMana >= cost) {
            currentMana -= cost;
            UpdateManaUI();
            return true;
        } else {
            Debug.LogWarning("法力值不足！需要: " + cost + ", 当前: " + currentMana);
            return false;
        }
    }
    

   

    
    // --- 注册单位 ---
    public void RegisterUnit(CharacterBase unit, bool isEnemy) {
        if (isEnemy) {
            enemies.Add(unit);
        } else {
            allies.Add(unit);
        }
        allUnits.Add(unit);
    }
    
    public CharacterBase GetRandomEnemy() {
        // 过滤掉死人
        var livingEnemies = new List<CharacterBase>();
        foreach (var e in enemies) {
            if (e.currentHealth > 0) livingEnemies.Add(e);
        }

        return livingEnemies.Count > 0 ? livingEnemies[Random.Range(0, livingEnemies.Count)] : null;
    }
    
    // --- 光环系统核心方法 ---

    // 1. 全局刷新：只要场面变了就调用它
    public void OnBoardChanged()
    {
        
        // 让每个人重新算一遍属性
        foreach (var unit in allUnits)
        {
            if(unit != null) unit.RefreshStats();
        }
    }

    // 2. 查询接口：CharacterBase 用它来问“我有啥光环？”
    public List<AuraEffect> GetActiveAurasFor(CharacterBase target)
    {
        var result = new List<AuraEffect>();
        // 遍历全场所有活人
        
        foreach (var provider in allUnits)
        {
            if (provider == null) continue;

            // 如果这个提供者被沉默了，就跳过 (未来扩展)
            // if (provider.isSilenced) continue;

            // 检查提供者身上带不带光环定义
            if (provider.cardData != null && provider.cardData.auraEffects != null)
            {
                foreach (var aura in provider.cardData.auraEffects)
                {
                    // 检查这光环能不能套在 target 身上
                    if (aura.IsApplicable(provider, target))
                    {
                        result.Add(aura);
                    }
                }
            }
        }
        return result;
    }
    
    
    // --- 新增：重启游戏 (绑定给按钮) ---
    public void OnRestartButton() {
        // 重新加载当前场景
        // SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    public void OnPlayerDied() {
        GameOver("YOU DIED");
    }

    public void OnAlliesDie(CharacterBase ally)
    {
        if (!allies.Contains(ally)) return;
        allies.Remove(ally);
        allUnits.Remove(ally);
    }
    public void OnEnemyDied(CharacterBase deadEnemy) {
        // 从列表移除
        if (enemies.Contains(deadEnemy)) {
            enemies.Remove(deadEnemy);
            allUnits.Remove(deadEnemy);
        }

        // 检查是不是赢了 (所有敌人都死光才算赢)
        if (enemies.Count == 0) {
            GameOver("VICTORY!");
        }
    }
    

    private void GameOver(string resultText) {
        // 1. 停止所有协程 (停止敌人思考、回合流转)
        StopAllCoroutines();
        
        // 2. 显示面板
        // if (gameOverPanel != null) {
        //gameOverPanel.SetActive(true);
        // if (gameOverText != null) gameOverText.text = resultText;
        // }

        // 3. 锁定状态 (防止还能拖牌)
        isPlayerTurn = false;
    }


}