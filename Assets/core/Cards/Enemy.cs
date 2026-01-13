public class Enemy : CharacterBase {
    
    public override void DoTurnAction() {
        // 简单的 AI：直接攻击玩家
        // 这里需要获取 GameManager.Instance.player
        DamageInfo info = new DamageInfo(1, StatsType.Physical, this);
        GameManager.Instance.player.TakeDamage(info);
        
        // 进阶：你可以在这里写 if (health < 5) { Heal(); } else { Attack(); }
    }
}