using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 这是一个“模版”，不能直接用，只能被继承
// 它的作用是规定所有技能必须长什么样
public abstract class CardEffect : ScriptableObject
{
    [Header("描述 (给策划看的)")]
    public string effectName;

    // 真正的游戏里，技能执行需要两个核心信息：
    // 1. user: 谁放的技能？
    // 2. target: 对谁放？(如果不需要目标，这个就是 null)
    public abstract void Execute(CharacterBase user, CharacterBase target);
    
}

// 顺便定义一个 CharacterBase，让后面的代码不报错
// 以后我们的 Player 和 Enemy 都会继承这个类



public class CharacterBase : MonoBehaviour
{
    [Header("基础属性 (Base Stats)")]
    public string characterName;
    public int maxHealth = 30;
    public int currentHealth;
    public int spellPower = 0; 
    public int currentArmor = 0;

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

    // 初始化方法 (子类可以覆写)
    protected virtual void Start()
    {

    }
    
    public virtual void Initialize() {
        if (isInitialized) return; // 如果已经初始化过，就别再动了
        // 这里还可以重置护甲、Buff等状态
        currentHealth = maxHealth;
        isInitialized = true;
        
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

        // --- 3. 最终扣血 ---
        currentHealth -= finalDamage;
        if (currentHealth > 0) return;
        currentHealth = 0;
        Die();
    }

    public void Heal(int amount)
    {
        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;
        Debug.Log($"<color=green>{characterName} 恢复 {amount} 点生命。当前血量: {currentHealth}</color>");
    }
    public virtual void DrawCard(int amount) { }
    public virtual void DoTurnAction() {
        // 默认什么都不做
    }
    protected virtual void Die() {
        // 直接比较引用，比 Tag 更安全
        if (this == GameManager.Instance.player) {
            GameManager.Instance.OnPlayerDied();
        } else  {
            GameManager.Instance.OnEnemyDied(this);
        }

        // 简单的视觉反馈：变灰或者消失
        GetComponent<SpriteRenderer>().color = Color.gray;
        // Destroy(gameObject); // 先别删，不然 GameManager 可能会报空引用
    }
}