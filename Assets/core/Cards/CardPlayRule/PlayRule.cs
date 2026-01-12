using UnityEngine;

public abstract class PlayRule : ScriptableObject
{
    // 返回 null 或 空字符串 代表通过
    // 返回 具体文字 代表失败原因
    public abstract string Check(RuntimeItem item, CharacterBase target);
}