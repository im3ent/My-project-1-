using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    // 单例模式：让任何地方都能找到 GameManager
    public static GameManager Instance;
    [Header("状态标记")]
    public bool isPlayerTurn = true; // 关键标记：防止玩家在敌人回合乱动
    
    [Header("游戏资源")]
    public int maxMana = 3;       // 回合上限
    public int currentMana;       // 当前剩余
    public TextMeshProUGUI manaText; // 拖入场景里的 UI
    
    [Header("角色引用")]
    public Character player;
    public List<CharacterBase> enemies = new List<CharacterBase>();
    public List<CharacterBase> allies = new List<CharacterBase>();
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
    public IEnumerator StartPlayerTurn() {
        isPlayerTurn = true;
        
        // 1. 回费
        currentMana = maxMana;
        UpdateManaUI();
        
        // 2. 抽牌 (调用我们刚写好的 HandManager)
        HandManager.Instance.DrawCard();

        // 3. UI 提示 (可选)
        Debug.Log("【玩家回合】");
        
        yield return null;
    }
    // --- 阶段 2: 玩家点击结束回合 (绑定给按钮) ---
    public void OnEndTurnButton() {
        if (!isPlayerTurn) return; // 防止狂点

        // 进入敌人回合
        StartCoroutine(EnemyTurn());
    }

    // --- 阶段 3: 敌人回合 ---
    // --- 修改：敌人回合逻辑 ---
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

        yield return new WaitForSeconds(0.5f);
        StartCoroutine(StartPlayerTurn());
    }

    private void UpdateManaUI()
    {
        if (manaText != null) {
            manaText.text = $"Mana: {currentMana}/{maxMana}";
        }
    }

    // --- 核心逻辑：打出一张牌 ---
    public void PlayCard(CardDefinition card, CharacterBase target)
    {
        // 执行所有“打出时”的效果
        // 关键点：这里是一个循环，不管你有 1 个技能还是 10 个技能，都会依次执行
        foreach (var effect in card.onPlayEffects)
        {
            // user 是 player (谁打的牌)
            // target 是选中的目标
            effect.Execute(player, target);
        }
        // --- 2. 区分类型 ---
        if (card.cardType == CardType.Spell) {
            // A. 法术逻辑：
            // 效果已经在上面执行完了，现在只需要播放特效，然后销毁卡牌
            Debug.Log($"释放了法术: {card.cardName}");
            // (手牌的销毁逻辑通常在 CardDragHandler 里 Destroy(gameObject))
        }
        else if (card.cardType == CardType.Minion) {
            // B. 随从逻辑：
            // 战吼已经在上面执行完了，现在需要把随从召唤到场上
            SpawnMinion(card); 
            Debug.Log($"召唤了随从: {card.cardName}");
        }
    }
    // 单独的召唤逻辑
    void SpawnMinion(CardDefinition card) {
        // 实例化随从预制体到友方生成区
        GameObject minionObj = Instantiate(card.minionPrefab, allySpawnZone);
        
        // 初始化随从数据 (攻击力、血量)
        CharacterBase minionScript = minionObj.GetComponent<CharacterBase>();
        // 这里你可能需要给 CharacterBase 加个 Setup(card) 方法来通过数据初始化血量
        
        // 注册进列表
        RegisterUnit(minionScript, false);
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
    
    // --- 新增：注册单位 ---
    public void RegisterUnit(CharacterBase unit, bool isEnemy) {
        if (isEnemy) {
            enemies.Add(unit);
        } else {
            allies.Add(unit);
        }
    }
    public CharacterBase GetRandomEnemy() {
        // 过滤掉死人
        var livingEnemies = new List<CharacterBase>();
        foreach (var e in enemies) {
            if (e.currentHealth > 0) livingEnemies.Add(e);
        }

        return livingEnemies.Count > 0 ? livingEnemies[Random.Range(0, livingEnemies.Count)] : null;
    }
    
    // --- 新增：重启游戏 (绑定给按钮) ---
    public void OnRestartButton() {
        // 重新加载当前场景
        // SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    public void OnPlayerDied() {
        GameOver("YOU DIED");
    }

    public void OnEnemyDied(CharacterBase deadEnemy) {
        // 从列表移除
        if (enemies.Contains(deadEnemy)) {
            enemies.Remove(deadEnemy);
        }

        // 检查是不是赢了 (所有敌人都死光才算赢)
        if (enemies.Count == 0) {
            GameOver("VICTORY!");
        }
    }
    void GameOver(string resultText) {
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