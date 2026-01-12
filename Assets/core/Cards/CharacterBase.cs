using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
public class CharacterBase : MonoBehaviour,IPassiveContainer
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
    
    private bool isDying = false;
    [SerializeField]
    public List<PassiveContext> inventoryPassives = new();
    public List<PassiveEffect> nativePassives = new();
    [Header("Data Reference")]
    public CardDefinition cardData;
    public RuntimeItem sourceRuntimeItem;

    
    private bool isInitialized = false;
    
    
    [Header("视觉反馈")]
    public GameObject floatingTextPrefab;
    
    private SpriteRenderer spriteRenderer;
    protected virtual void Awake() 
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    void Start()
    {
        // ✅ 这里是安全的：如果场景里手动摆了怪，这里会自动初始化
        // 这里的 baseMaxHealth == 0 是为了防止 SpawnUnitEffect 已经初始化过一次了
        if (cardData != null && baseMaxHealth == 0)
        {
            Initialize (new RuntimeItem(cardData, this));
        }
    }
    public virtual void Initialize(RuntimeItem sourceItem) 
    {
        if (isInitialized) return; // 如果已经初始化过，就别再动了
        // 1. 存蓝图
        this.sourceRuntimeItem = sourceItem;
        this.cardData = sourceItem.Data;

        // 2. 设定“地基”（Base）
        baseMaxHealth = sourceItem.health; // 👈 读实例数据！
        baseAttack = sourceItem.attack;
        // 3. 设定“初始状态”（Current）
        // 刚出生时，如果没有光环，当前值就应该等于基础值

        currentMaxHealth = baseMaxHealth;
        currentHealth = baseMaxHealth; // 出生满血
        currentAttack = baseAttack;
        
        // ✨✨✨ 新增：应用自带状态 (Innate Statuses) ✨✨✨
        /*if (cardData.initialStatuses is { Count: > 0 })
        {
            // 必须先获取管理器
            var stateManager = GetComponent<CharacterStateManager>();
            if (stateManager != null)
            {
                foreach (var config in cardData.initialStatuses)
                {
                    if (config.status != null)
                    {
                        // 给刚出生的随从贴上 Buff
                        stateManager.ApplyStatus(config.status, config.stacks);
                    }
                }
            }
        }*/
        
        isInitialized = true;
        RefreshStats();
    }


    // --- 定义动作 ---
    public virtual void TakeDamage(DamageInfo info)
    {
        var finalDamage = info.amount;

        var stateManager = GetComponent<CharacterStateManager>();
        
        if (stateManager != null)
        {
            finalDamage = stateManager.GetModifiedIncomingDamage(finalDamage);
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
        
        var stateManager = GetComponent<CharacterStateManager>();
        if (stateManager != null)
        {
            // ✨✨✨ 核心变化：直接问管家要最终结果 ✨✨✨
            // 管家已经把 基础值 + Buff + 光环 全部算好了
            currentAttack = stateManager.GetCalculatedAttack(baseAttack);
            currentMaxHealth = stateManager.GetCalculatedMaxHealth(baseMaxHealth);
            spellPower = stateManager.GetTotalSpellPower(baseSpellPower);
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
    public void ClearInventoryPassives()
    {
        // 如果你有角色的“天赋树”被动，不要在这里清空它们！
        // 建议把 globalPassives 专门用来存“背包给的被动”
        // 或者把被动分成两个 list：nativePassives (天赋) 和 inventoryPassives (背包)
        
        inventoryPassives.Clear(); 
    }
    // --- 接口实现 ---
    
    public void AddTemporaryPassive(PassiveEffect effect = null, RuntimeItem source = null)
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

    public IEnumerable<PassiveContext> GetActivePassives()
    {
        // 1. 返回天生自带的被动 (包装成 source = null，因为没有来源卡牌)
        foreach (var p in nativePassives)
        {
            yield return new PassiveContext(p, null);
        }

        // 2. 返回从背包实时同步过来的被动
        foreach (var ap in inventoryPassives)
        {
            yield return ap;
        }
    }

}
