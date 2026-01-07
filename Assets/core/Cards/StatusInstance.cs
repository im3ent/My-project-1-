[System.Serializable]
public class StatusInstance
{
    public StatusEffect Data { get; private set; }         // 引用蓝图
    public CharacterStateManager Owner { get; private set; } // 归属者
    public int Stacks { get; set; }                        // 当前层数

    public StatusInstance(StatusEffect data, CharacterStateManager owner, int stacks)
    {
        this.Data = data;
        this.Owner = owner;
        this.Stacks = stacks;
    }

    // 快捷方法：减少层数，如果归零则自动请求移除
    public void DecreaseStack(int amount = 1)
    {
        Stacks -= amount;
        if (Stacks <= 0)
        {
            Owner.RemoveStatus(this);
        }
        else
        {
            Owner.NotifyStateChanged(); // 刷新 UI 数字
        }
    }
}