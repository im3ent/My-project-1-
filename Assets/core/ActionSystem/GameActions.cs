using System.Collections;
using UnityEngine;

/// <summary>
/// 伤害动作 - 对目标造成伤害
/// </summary>
public class DamageAction : GameAction
{
    private readonly CharacterBase _target;
    private readonly DamageInfo _damageInfo;
    
    public DamageAction(CharacterBase target, DamageInfo damageInfo, float duration = 0.2f)
    {
        _target = target;
        _damageInfo = damageInfo;
        Duration = duration;
    }
    
    public override IEnumerator Execute()
    {
        if (_target != null && !_target.isDead)
        {
            _target.TakeDamage(_damageInfo);
        }
        
        if (Duration > 0)
        {
            yield return new WaitForSeconds(Duration);
        }
        
        Complete();
    }
}

/// <summary>
/// 治疗动作 - 治疗目标
/// </summary>
public class HealAction : GameAction
{
    private readonly CharacterBase _target;
    private readonly int _amount;
    
    public HealAction(CharacterBase target, int amount, float duration = 0.2f)
    {
        _target = target;
        _amount = amount;
        Duration = duration;
    }
    
    public override IEnumerator Execute()
    {
        if (_target != null && !_target.isDead)
        {
            _target.Heal(_amount);
        }
        
        if (Duration > 0)
        {
            yield return new WaitForSeconds(Duration);
        }
        
        Complete();
    }
}

/// <summary>
/// 施加状态动作 - 给目标施加 Buff/Debuff
/// </summary>
public class ApplyBuffAction : GameAction
{
    private readonly CharacterBase _target;
    private readonly StatusEffect _buffData;
    private readonly CharacterBase _caster;
    private readonly EffectSnapshot _snapshot;
    
    public ApplyBuffAction(CharacterBase target, StatusEffect buffData, CharacterBase caster, EffectSnapshot snapshot, float duration = 0.1f)
    {
        _target = target;
        _buffData = buffData;
        _caster = caster;
        _snapshot = snapshot;
        Duration = duration;
    }
    
    public override IEnumerator Execute()
    {
        if (_target != null && !_target.isDead && _target.stateManager != null)
        {
            _target.stateManager.ApplyStatus(_buffData, _caster, _snapshot);
        }
        
        if (Duration > 0)
        {
            yield return new WaitForSeconds(Duration);
        }
        
        Complete();
    }
}

/// <summary>
/// 抽牌动作
/// </summary>
public class DrawCardAction : GameAction
{
    private readonly CharacterBase _owner;
    private readonly int _amount;
    
    public DrawCardAction(CharacterBase owner, int amount = 1, float duration = 0.3f)
    {
        _owner = owner;
        _amount = amount;
        Duration = duration;
    }
    
    public override IEnumerator Execute()
    {
        for (int i = 0; i < _amount; i++)
        {
            if (HandManager.Instance != null)
            {
                HandManager.Instance.DrawCard(_owner);
            }
            
            // 每张牌之间稍微等一下
            if (i < _amount - 1)
            {
                yield return new WaitForSeconds(0.1f);
            }
        }
        
        if (Duration > 0)
        {
            yield return new WaitForSeconds(Duration);
        }
        
        Complete();
    }
}

/// <summary>
/// 获得法力动作
/// </summary>
public class GainManaAction : GameAction
{
    private readonly int _amount;
    private readonly bool _gainMax;
    private readonly bool _fillNew;
    private readonly bool _allowOverflow;
    
    public GainManaAction(int amount, bool gainMax = false, bool fillNew = true, bool allowOverflow = false, float duration = 0f)
    {
        _amount = amount;
        _gainMax = gainMax;
        _fillNew = fillNew;
        _allowOverflow = allowOverflow;
        Duration = duration;
    }
    
    public override IEnumerator Execute()
    {
        if (_gainMax)
        {
            GameManager.Instance?.ModifyMaxMana(_amount);
            if (_fillNew)
            {
                GameManager.Instance?.ModifyMana(_amount, _allowOverflow);
            }
        }
        else
        {
            GameManager.Instance?.ModifyMana(_amount, _allowOverflow);
        }
        
        if (Duration > 0)
        {
            yield return new WaitForSeconds(Duration);
        }
        
        Complete();
    }
}

/// <summary>
/// 等待动作 - 纯粹等待一段时间（用于动画同步）
/// </summary>
public class WaitAction : GameAction
{
    public WaitAction(float duration)
    {
        Duration = duration;
    }
    
    public override IEnumerator Execute()
    {
        if (Duration > 0)
        {
            yield return new WaitForSeconds(Duration);
        }
        Complete();
    }
}
