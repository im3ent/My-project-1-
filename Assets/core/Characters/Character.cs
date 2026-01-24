
// 继承我们在数据层定义的 CharacterBase
// 这样 DamageEffect 里的 target.TakeDamage() 就会调到这里
public class Character : CharacterBase
{
    /// <summary>
    /// 🎯 重写 Start：Player 由 RunManager 管理，跳过自动初始化
    /// </summary>
    protected override void Start()
    {
        // 🎯 如果是 RunManager 管理的持久化 player，跳过自动初始化
        if (RunManager.Instance != null && RunManager.Instance.GetCurrentPlayer() == this)
        {
            UnityEngine.Debug.Log($"[Character.Start] {gameObject.name}: Skipping auto-init, managed by RunManager");
            return;
        }
        
        // 否则调用基类的正常初始化逻辑
        base.Start();
    }
}