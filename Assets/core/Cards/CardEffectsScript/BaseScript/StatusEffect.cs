using UnityEngine;

// 这是所有 Buff 的基类 (ScriptableObject)
// 它只定义“规则”，不存“层数”
public abstract class StatusEffect : ScriptableObject
{
    [Header("UI 显示")]
    public string id;            // 唯一ID，如 "DoubleCast"
    public string displayName;   // 显示名
    public Sprite icon;          // 图标
    
    [Header("行为配置")]
    public bool isStackable = true; // 是否可堆叠 (比如中毒)
    public bool removeAtTurnEnd;    // 是否回合结束自动移除 (比如本回合法强+1)

    // =================================================
    // 核心钩子 (Hooks) - 让子类去重写
    // =================================================

    // 1. 回合开始时 (中毒扣血、自动抽牌)
    public virtual void OnTurnStart(StatusInstance instance) { }

    // 2. 回合结束时 (炸弹倒计时、移除临时Buff)
    public virtual void OnTurnEnd(StatusInstance instance) { }

    // 3. 计算费用时 (减费 Buff) - 返回修正后的费用
    public virtual int ModifyCost(StatusInstance instance, RuntimeCard card, int currentCost) 
    { 
        return currentCost; // 默认不改变
    }

    // 4. 卡牌打出时 (双倍施法、下张必暴)
    // 在这里修改 EffectContext，或者消耗 Buff 层数
    // 返回 true 表示 "我起作用了，并且我想消耗掉我的层数" (可选)
    public virtual void OnPlayCard(StatusInstance instance, EffectContext ctx) { }

    // 5. 造成/受到伤害时 (力量、易伤)
    public virtual int ModifyOutgoingDamage(StatusInstance instance, int damage) => damage;
    public virtual int ModifyIncomingDamage(StatusInstance instance, int damage) => damage;
}