using System.Collections.Generic;

[System.Serializable]
public class StatusInstance : IStatModifier
{
    public StatusEffect Data { get; private set; }         // 引用蓝图
    public CharacterStateManager Owner { get; private set; } // 归属者
    public CharacterBase Caster { get; set; }
              // 当前层数


    public EffectSnapshot snapshot; // 替代 float[] customValues
    
    public StatusInstance(
        StatusEffect data, 
        CharacterStateManager owner,
        CharacterBase caster, EffectSnapshot snapshot)
    {
        Data = data;
        Owner = owner;
        Caster = caster;
        
        // ✨ 优先使用传入的快照，如果没有则克隆 SO 的默认值
        if (snapshot != null)
        {
            this.snapshot = snapshot.Clone();
        }
        else 
        {
            // 兜底：尝试从 Data 获取默认快照
            var def = data.GetInitialSnapshot();
            this.snapshot = def ?? new EffectSnapshot();
        }
    }

    // 快捷方法：减少层数，如果归零则自动请求移除
    public void DecreaseStack(int amount = 1)
    {
        if (snapshot == null)
        {
            Owner.RemoveStatus(this);
            return;
        }

        snapshot.stacks -= amount;
        if (snapshot.stacks <= 0)
        {
            Owner.RemoveStatus(this);
        }
        else
        {
            Owner.NotifyStateChanged(); // 刷新 UI 数字
        }
    }
    public string GetParsedDescription()
    {
        if (string.IsNullOrEmpty(Data.descriptionConfig)) return "";

        // 使用 string.Format 进行格式化替换
        try
        {
            return string.Format(Data.descriptionConfig, Converter(snapshot));
        }
        catch (System.FormatException)
        {
            return Data.descriptionConfig;
        }
    }
    public object[] Converter(EffectSnapshot snap)
    {
        // 简单映射：目前主要支持 {0} 显示层数
        // 如果需要显示 {1}（基础伤害），我们需要约定 Key
        // 这里暂时实现一个兼容逻辑：
        // {0} -> Stacks
        // {1} -> "BaseValue" (如果存在)
        
        var args = new List<object>();
        args.Add(snap.stacks); // {0}
        
        // 尝试获取常用 Key
        float val = snap.GetFloat("BaseValue");
        args.Add(val); // {1}
        
        // 你可以根据扩展:
        // args.Add(snap.GetFloat("Ratio") * 100 + "%"); // {2}
        
        return args.ToArray();
    }

    // ==========================================================
    // ✨ 实现 IStatModifier 接口
    // ==========================================================
    public string SourceName => Data != null ? Data.displayName : "Unknown Buff";

    public float GetStatsFlat(StatsType type) 
        => Data != null ? Data.GetStatsFlat(this, type) : 0;

    public float GetStatsIncreased(StatsType type) 
        => Data != null ? Data.GetStatsIncreased(this, type) : 0;

    public float GetStatsMore(StatsType type) 
        => Data != null ? Data.GetStatsMore(this, type) : 1f;

    public float GetIncomingFlat(CharacterBase attacker) 
        => Data != null ? Data.FlatIncomingDamage(this) : 0; // 注意：旧接口只叫 FlatIncomingDamage

    public float GetIncomingIncreased(CharacterBase attacker) 
        => Data != null ? Data.IncreasedIncomingDamage(this) : 0;

    public float GetIncomingMore(CharacterBase attacker) 
        => Data != null ? Data.MoreIncomingDamage(this) : 1f;
}
