using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
public class CharacterBase : MonoBehaviour
{
    [Header("基础属性 (Base Stats)")]
    public int baseAttack;
    public int baseMaxHealth;

    public bool isEnemy;
    
    public string characterName;
    public int currentMaxHealth;
    public int currentHealth;
    public int baseSpellPower;
    public int spellPower; 
    public int currentArmor;
    public int currentAttack;
    
    [System.NonSerialized] private bool isDying = false;
    [System.NonSerialized] public bool isDead = false;
    [SerializeField]

    [Header("Data Reference")]
    public CardDefinition cardData;
    [System.NonSerialized] public RuntimeItem sourceRuntimeItem;
    public CharacterStateManager  stateManager;
    
    [System.NonSerialized] protected bool isInitialized = false;
    
    
    [Header("视觉反馈")]

    private SpriteRenderer spriteRenderer;
    
    // 1. 状态广播：用于刷新面板 (UI只关心当前值)
    public event Action<StatCTXForUI> OnStatsChanged;
    // 2. 瞬时广播：用于飘字/特效 (UI需要知道具体的变动数值)
    public event Action<int, DamageInfo> OnHealthDelta;
    // 3. 死亡广播
    public event Action<CharacterBase> OnDeath;
    
    protected virtual void Awake() 
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        stateManager = GetComponent<CharacterStateManager>();
    }
    
    /// <summary>
    /// 🎯 虚方法：子类可以重写以自定义初始化行为
    /// </summary>
    protected virtual void Start()
    {
        // ✅ 如果场景里手动摆了怪，这里会自动初始化
        Debug.Log($"[CharacterBase.Start] {gameObject.name}: cardData={(cardData != null ? cardData.name : "NULL")}, sourceRuntimeItem={(sourceRuntimeItem != null ? "EXISTS" : "NULL")}");
        
        if (cardData != null && sourceRuntimeItem == null)
        {
            Debug.Log($"[CharacterBase.Start] {gameObject.name}: Calling Initialize...");
            Initialize(new RuntimeItem(cardData, this));
        }
    }
    // 当物体被激活/加载时
    protected virtual void OnEnable()
    {
        // 这一步非常安全，因为 OnEnable 执行时 Instance 可能已经存在
        // 如果 GM 是懒加载单例，这里也没问题
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterUnit(this);
        }
    }
    protected virtual void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UnregisterUnit(this);
        }
    }
    
    public virtual void Initialize(RuntimeItem sourceItem) 
    {
        Debug.Log($"[CharacterBase.Initialize] {gameObject.name}: isInitialized={isInitialized}");
        if (isInitialized) return; // 如果已经初始化过，就别再动了
        // 1. 存蓝图
        this.sourceRuntimeItem = sourceItem;
        this.cardData = sourceItem.data;

        // 2. 设定“地基”（Base）
        baseMaxHealth = sourceItem.health; // 👈 读实例数据！
        baseAttack = sourceItem.attack;
        // 3. 设定“初始状态”（Current）
        // 刚出生时，如果没有光环，当前值就应该等于基础值
        currentMaxHealth = baseMaxHealth;
        currentHealth = baseMaxHealth; // 出生满血
        currentAttack = baseAttack;
        
        isInitialized = true;
        

        // B. 初始化自我施加的 Buff (Battlecry - Self) -> 撤销！
        // 原因：如果放在这里，正常打出卡牌时会导致双重触发 (Initialize一次，PlayCard一次)
        // 这个逻辑应该由 SpawnUnitEffect 这种绕过 PlayCard 的特例自己去处理。
        
        // ✨ C. 初始化原生被动 (Native Passives)
        if (stateManager != null)
        {
            stateManager.InitializePassives(sourceItem);
        }

        RefreshStats();
    }
    
    // --- 定义动作 ---
    public virtual void TakeDamage(DamageInfo info)
    {
        var finalDamage = Mathf.FloorToInt(info.amount);
        
        
        if (stateManager != null)
        {
            finalDamage = stateManager.GetModifiedIncomingDamage(finalDamage, info.source);
        }


        // --- 2. 处理 护甲 (Armor) ---
        // 只有当伤害类型是“物理” 且 没说要“无视护甲”时，才计算护甲
        if (info.type == StatsType.Physical && !info.ignoreArmor)
        {
            finalDamage -= currentArmor;
            if (finalDamage < 0) finalDamage = 0; // 护甲不能回血
        }
        // 如果是 StatsType.Magical，也许你会去减魔抗...
        // 如果是 StatsType.True，上面两个 if 都进不去，直接造成原始伤害

        // --- ✨ 新增动画代码开始 ✨ ---
        
        // 1. 震动效果：持续0.2秒，强度0.5，震动20次
        // 这会让角色挨打时猛烈晃动一下
        transform.DOShakePosition(0.2f, 0.5f, 20);

        // 2. 变红闪烁：如果角色有图片，就变红
        if (spriteRenderer != null) {
            // 瞬间变红
            spriteRenderer.DOColor(Color.red, 0.1f).OnComplete(() => {
                // 0.1秒后变回白色
                spriteRenderer.DOColor(Color.white, 0.1f);
            });
        }
        // --- ✨ 新增动画代码结束 ✨ ---
        
        // --- 3. 最终扣血 ---
        currentHealth -= finalDamage;
        OnHealthDelta?.Invoke(-finalDamage, info);
        
        // ✨ 全局事件：造成/受到伤害
        GameEvents.TriggerDamageDealt(info.source, this, finalDamage);
        GameEvents.TriggerDamageTaken(this, info.source, finalDamage);
        
        if (currentHealth > 0) return;
        currentHealth = 0;
        Die(info.source);
        
    }

    public void Heal(int amount)
    {
        var oldCurrentHealth = currentHealth;
        currentHealth += amount;
        if (currentHealth > currentMaxHealth) currentHealth = currentMaxHealth;
        
        int actualHeal = currentHealth - oldCurrentHealth;
        if (actualHeal > 0)
        {
            // ✨ 全局事件：治疗
            GameEvents.TriggerHeal(this, actualHeal);
        }
        
        OnStatsChanged?.Invoke(new StatCTXForUI(oldCurrentHealth, currentHealth, currentAttack));
    }
    
    // 核心：刷新属性（光环系统的灵魂）
    public void RefreshStats()
    {
        if (isDying) return;
        // 1. 先重置回基础值 (把之前的光环全忘掉)
        var oldMaxHealth = currentMaxHealth;
        var oldCurrentHealth = currentHealth;
        if (stateManager != null)
        {
            currentAttack = stateManager.GetModifiedStats(baseAttack,StatsType.Physical);
            currentMaxHealth = stateManager.GetModifiedStats(baseMaxHealth , StatsType.Health);
        }
        else
        {
            // 兜底：如果没有管理器，就等于基础值
            currentAttack = baseAttack;
            currentMaxHealth = baseMaxHealth;
        }

        // 4. 处理血量变化的特殊逻辑 (炉石规则)
        // 如果获得了生命值光环，当前血量也增加
        if (currentMaxHealth > oldMaxHealth)
        {
            var diff = currentMaxHealth - oldMaxHealth;
            currentHealth += diff;
        }
        // 如果失去了生命值光环 (比如光环怪死了)，当前血量不能超过新的上限
        else if (currentMaxHealth < oldMaxHealth)
        {
            if (currentHealth > currentMaxHealth)
            {
                currentHealth = currentMaxHealth;
            }
        }
        if (currentHealth <= 0)
        {
            Die();
        }
        // 5. 更新头顶的 UI 数字
        OnStatsChanged?.Invoke(new StatCTXForUI(oldCurrentHealth,currentHealth,currentAttack));
    }
    
    
    public virtual void DoTurnAction() {
        // 默认什么都不做
    }

    protected virtual void Die(CharacterBase killer = null)
    {
        if (isDying) return;
        isDying = true; // 上锁
        
        // ✨ 全局事件：单位死亡
        GameEvents.TriggerUnitDied(this, killer);
        
        // ✨ 通知背包系统：有人于此倒下 (可能是敌人，也可能是友军)
        if (InventoryManager.Instance != null) 
        {
            InventoryManager.Instance.OnUnitKilled(this);
        }

        // ✨ 触发自身的被动 (比如永恒骑士的亡语)
        // InventoryManager 只管背包里的东西，不管场上的怪(Native Passive)
        if (stateManager != null)
        {
            var passives = stateManager.GetActivePassives().ToList();
            Debug.Log($"[CharacterBase] {characterName} Died. Triggering {passives.Count} passives.");
            foreach (var ctx in passives)
            {
                if (ctx.effect != null)
                {
                    Debug.Log($"[CharacterBase] Triggering OnUnitKilled for passive: {ctx.effect.name}");
                    // 参数：owner=自己, source=被动来源, victim=自己
                    ctx.effect.OnUnitKilled(this, ctx.source, this);
                }
            }
        }

        GameManager.Instance.UnregisterUnit(this);



        // 2. 物理层禁用 (防止尸体还能挡住射线，或者被当作目标)
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // 3. 视觉层动画 (DOTween Sequence)
        // 必须先杀掉受击动画 (防止还在震动)
        transform.DOKill(); 

        var deathSeq = DOTween.Sequence();

        if (spriteRenderer != null) {
            // A. 变灰 (失去生机)
            deathSeq.Join(spriteRenderer.DOColor(Color.gray, 0.5f));
            // B. 变透明 (灵魂消散)
            deathSeq.Join(spriteRenderer.DOFade(0, 1f));
        }

        // C. 缩小/下沉 (尸体消失)
        deathSeq.Join(transform.DOScale(0f, 1f).SetEase(Ease.InBack));
        // 或者用下沉：deathSeq.Join(transform.DOMoveY(-1f, 1f).SetRelative(true));

        // 4. 动画播完后，彻底销毁物体
        deathSeq.OnComplete(() => {
            Destroy(gameObject);
        });
        isDead = true;
        // 重要！我死了，光环消失，所有人重新计算
        GameManager.Instance.OnBoardChanged();
        OnDeath?.Invoke(this);
    }
    

}
