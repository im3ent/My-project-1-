using UnityEngine;
using System.Collections.Generic;

// 这是一个纯数据容器
public class EffectContext 
{
    // 1. 基础信息 (进厂时就有的)
    public CharacterBase caster;       // 谁打的牌
    public CharacterBase mainTarget;   // 玩家鼠标选的目标 (如果有)
    public RuntimeCard sourceRuntimeCard;
    
    public CardDefinition SourceCard => sourceRuntimeCard?.Data;
    
    public int repeatCount = 0;
    
    // ✨ 新增：用于传递这一波操作中被消耗的 Buff (给 UI 动画用)
    public List<StatusEffect> consumedBuffs = new List<StatusEffect>();
    
    // 2. 中间产物 (流水线上生产出来的)
    // 用列表是因为可能一次召唤好几个
    public List<CharacterBase> createdUnits = new();
    
    // 辅助属性：方便获取“刚刚生产出来的那个”
   
    public CharacterBase LastCreatedUnit 
    {
        get 
        {
            if (createdUnits is { Count: > 0 })
                return createdUnits[^1];
            return null;
        }
    }
    
    // 构造函数
    public EffectContext(CharacterBase caster, CharacterBase target, RuntimeCard runtimeCard)
    {
        this.caster = caster;
        mainTarget = target;
        this.sourceRuntimeCard = runtimeCard;
    }
}