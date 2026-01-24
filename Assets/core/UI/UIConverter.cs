using System;

public static class UIConverter
{
    // 将任何枚举值转换为带颜色的中文名
    public static string ToChinese(int value)
    {
        return value switch
        {
            0 => "",
            1 => "<color=#FFCC00>随机单位</color>",
            2 => "<color=#44FF44>随机队友</color>",
            3 => "<color=#FF4444>随机敌人</color>",
            _ => ""
        };
    }
}