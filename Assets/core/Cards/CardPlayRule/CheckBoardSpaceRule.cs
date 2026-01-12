using UnityEngine;

[CreateAssetMenu(menuName = "Rules/Check Board Space")]
public class CheckBoardSpaceRule : PlayRule
{
    public override string Check(RuntimeItem item, CharacterBase target)
    {
        // 只有随从才需要检查这个
        if (item.Data.cardType != CardType.Minion) return null;
        return GameManager.Instance.allies.Count >= 7 ? // 假设上限是7
            "随从位置已满！" : null;
    }
}