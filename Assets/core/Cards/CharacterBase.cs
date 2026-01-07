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
    public int spellPower; 
    public int currentArmor;
    public int currentAttack;
    
    private bool isDying = false;
    [Header("Data Reference")]
    public CardDefinition cardData;

    [Header("状态列表")]
    // 这里存着角色身上所有的 Buff 和 Debuff
    public List<StatusEffectInstance> currentStatuses = new();
    
    private bool isInitialized = false;
    // --- 核心方法 1：施加状态 ---
    public void ApplyStatus(StatusType type, int duration)
    {
        // 检查是不是已经有这个状态了
        var existingStatus = currentStatuses.FirstOrDefault(s => s.type == type);

        if (existingStatus != null)
        {
            // 如果有了，通常是刷新时间，或者叠加层数
            existingStatus.duration = Mathf.Max(existingStatus.duration, duration);
            Debug.Log($"{characterName} 的 {type} 刷新了，剩余 {existingStatus.duration} 回合");
        }
        else
        {
            // 如果没有，加个新的
            currentStatuses.Add(new StatusEffectInstance(type, duration));
            Debug.Log($"{characterName} 获得了状态: {type}");
        }
    }
    // --- 核心方法 2：检查有没有某个状态 ---
    public bool HasStatus(StatusType type)
    {
        return currentStatuses.Any(s => s.type == type);
    }

    public void ApplyBuff(int atkMod, int healthMod)
    {
        // 第一步：只管改基础数值
        baseAttack += atkMod;
        baseMaxHealth += healthMod;
        
        RefreshStats(); 
    }
    
    [Header("视觉反馈")]
    public GameObject floatingTextPrefab;
    
    private SpriteRenderer spriteRenderer;
    protected virtual void Awake() {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    void Start()
    {
        // ✅ 这里是安全的：如果场景里手动摆了怪，这里会自动初始化
        // 这里的 baseMaxHealth == 0 是为了防止 SpawnUnitEffect 已经初始化过一次了
        if (cardData != null && baseMaxHealth == 0)
        {
            Initialize(cardData);
        }
    }
    public virtual void Initialize(CardDefinition data) 
    {
        if (isInitialized) return; // 如果已经初始化过，就别再动了
        // 1. 存蓝图
        cardData = data; 

        // 2. 设定“地基”（Base）
        baseMaxHealth = data.health;
        baseAttack = data.attack;
        // 3. 设定“初始状态”（Current）
        // 刚出生时，如果没有光环，当前值就应该等于基础值

        currentMaxHealth = baseMaxHealth;
        currentHealth = baseMaxHealth; // 出生满血
        currentAttack = baseAttack;
        
        isInitialized = true;
        RefreshStats();
    }

    // --- 定义动作 ---
    public virtual void TakeDamage(DamageInfo info)
    {
        var finalDamage = info.amount;

        // --- 1. 处理 易伤 (Vulnerable) ---
        // 只有当伤害类型不是“真实伤害” 且 没说要“无视易伤”时，才计算易伤
        if (info.type != DamageType.True && !info.ignoreVulnerable)
        {
            // 假设你有一个 isVulnerable 变量
            if (HasStatus(StatusType.Vulnerable))
            {
                finalDamage = Mathf.RoundToInt(finalDamage * 1.5f);
            }
        }

        // --- 2. 处理 护甲 (Armor) ---
        // 只有当伤害类型是“物理” 且 没说要“无视护甲”时，才计算护甲
        if (info.type == DamageType.Physical && !info.ignoreArmor)
        {
            finalDamage -= currentArmor;
            if (finalDamage < 0) finalDamage = 0; // 护甲不能回血
        }
        // 如果是 DamageType.Magical，也许你会去减魔抗...
        // 如果是 DamageType.True，上面两个 if 都进不去，直接造成原始伤害

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
        // --- ✨ 新增：生成伤害飘字 ✨ ---
        if (floatingTextPrefab != null && finalDamage > 0) // 只有掉血才飘字
        {
            // 1. 生成在头顶稍微高一点的位置
            Vector3 spawnPos = transform.position + Vector3.up * 1.5f; 
            
            // 2. 实例化
            GameObject popup = Instantiate(floatingTextPrefab, spawnPos, Quaternion.identity);
            
            // 3. 初始化数字
            popup.GetComponent<FloatingText>().Setup(finalDamage);
        }
        
        
        if (currentHealth > 0) return;
        currentHealth = 0;
        Die();
    }

    public void Heal(int amount)
    {
        currentHealth += amount;
        if (currentHealth > currentMaxHealth) currentHealth = currentMaxHealth;
    }
    
    // 核心：刷新属性（光环系统的灵魂）
    public void RefreshStats()
    {
        if (isDying) return;
        // 1. 先重置回基础值 (把之前的光环全忘掉)
        var oldMaxHealth = currentMaxHealth;
        currentAttack = baseAttack;
        currentMaxHealth = baseMaxHealth;

        // 2. 问 GameManager：现在谁罩着我？
        var activeAuras = GameManager.Instance.GetActiveAurasFor(this);
        
        // 3. 累加所有光环
        foreach (var aura in activeAuras)
        {
            currentAttack += aura.attackBuff;
            currentMaxHealth += aura.healthBuff;
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
        UpdateUI();
    }

    private void UpdateUI()
    {
        // 这里写你更新攻击力/血量 Text 的代码
        // 比如：healthText.text = currentHealth.ToString();
        // 如果 currentAttack > baseAttack，把字变绿...
    }

    public virtual void DrawCard(int amount) { }
    
    public virtual void DoTurnAction() {
        // 默认什么都不做
    }


    protected virtual void Die()
    {
        if (isDying) return;
        isDying = true; // 上锁

        // 1. 逻辑层先处理 (通知 GM 移除列表，防止后续还能被选中)
        if (this == GameManager.Instance.player) {
            GameManager.Instance.OnPlayerDied();
        } 
        else if (isEnemy) 
            GameManager.Instance.OnEnemyDied(this);

        else
            GameManager.Instance.OnAlliesDie(this);


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
        
        // 重要！我死了，光环消失，所有人重新计算
        GameManager.Instance.OnBoardChanged();
    }
}
