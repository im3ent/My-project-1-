using UnityEngine;

[CreateAssetMenu(menuName = "Rules/Check Mana")]
public class CheckManaRule : PlayRule
{
    public override string Check(RuntimeCard card, CharacterBase target)
    {

        var finalCost = GameManager.Instance.GetModifiedCost(card);
        return GameManager.Instance.currentMana < finalCost ? "法力值不足！" : null; // 通过
    }
}