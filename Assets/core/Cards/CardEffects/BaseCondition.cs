using UnityEngine;

public abstract class BaseCondition : ScriptableObject
{
    public abstract bool IsMet(RuntimeItem source);
    
    // 钩子：某些条件需要监听事件来累积进度
    public virtual void OnUnitKilled(CharacterBase owner, RuntimeItem source, CharacterBase victim) { }
    
    // ✨ 钩子：监听打牌事件
    public virtual void OnPlayCard(CharacterBase owner, RuntimeItem source, EffectContext ctx) { }
    
    // 未来可以扩展更多的钩子，比如 OnTurnStart, OnDamageDealt 等
}
