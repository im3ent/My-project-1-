
// 继承我们在数据层定义的 CharacterBase
// 这样 DamageEffect 里的 target.TakeDamage() 就会调到这里
public class Character : CharacterBase
{
// 重写抽牌逻辑
    public override void DrawCard(int amount)
    {
        
        for (var i = 0; i < amount; i++)
        {
            HandManager.Instance.DrawCard(this);
        }
    }
}