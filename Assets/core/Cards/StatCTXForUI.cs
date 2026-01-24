
public struct StatCTXForUI
{

    public int oldHpValue;          // 变化前的值
    public int newHpValue;          // 变化后的值
    public int atkValue;            // 变化的类型

    public StatCTXForUI(int oldHpValue, int newHpValue, int atkValue)
    {
        this.oldHpValue = oldHpValue;
        this.newHpValue = newHpValue;
        this.atkValue = atkValue;
    }

}
