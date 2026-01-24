using UnityEngine;

[CreateAssetMenu(menuName = "Rules/Check Target")]
public class CheckTargetRule : PlayRule
{
    public override string Check(RuntimeItem item, CharacterBase target)
    {
        if (item.data.needsTarget && target == null)
        {
            return "需要选择一个目标！";
        }
        return null;
    }
}