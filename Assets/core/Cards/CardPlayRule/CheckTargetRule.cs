using UnityEngine;

[CreateAssetMenu(menuName = "Rules/Check Target")]
public class CheckTargetRule : PlayRule
{
    public override string Check(RuntimeCard card, CharacterBase target)
    {
        if (card.Data.needsTarget && target == null)
        {
            return "需要选择一个目标！";
        }
        return null;
    }
}