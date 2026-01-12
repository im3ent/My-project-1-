using UnityEngine;

public class PassiveContext
{
    public PassiveEffect effect;   // 效果数据 (ScriptableObject)
    public RuntimeItem source;     // 来源卡牌 (实例数据)
    
    public PassiveContext(PassiveEffect e, RuntimeItem s)
    {
        effect = e;
        source = s;
    }
}
