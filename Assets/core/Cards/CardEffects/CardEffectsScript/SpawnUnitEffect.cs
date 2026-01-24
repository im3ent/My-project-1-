using UnityEngine;
using DG.Tweening;

[CreateAssetMenu(fileName = "NewSpawnEffect", menuName = "CardEffects/Spawn Unit")]
public class SpawnUnitEffect : CardEffect {
    
    [Header("召唤配置")]
    public CharacterBase unitPrefab; // 要召唤的怪物的预制体
    public CardDefinition associatedCardData;   //随从的身份证/蓝图
    public bool isEnemy = false;  // 是召唤给敌人(内鬼/小怪)，还是召唤给自己(随从)？
    public Vector3 spawnOffset = new Vector3(2, 0, 0); // 相对生成点的偏移量
    
    public override float Execute(EffectContext ctx) {
        // 1. 决定生成位置
        // 如果是敌人，生成在 EnemySpawnZone；如果是友军，生成在 AllySpawnZone
        var zone = isEnemy ? GameManager.Instance.enemySpawnZone : GameManager.Instance.allySpawnZone;
        
        // 简单的随机偏移，防止完全重叠
        var finalPos = zone.position + new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0);

        // 2. 生成物体
        var newUnitObj = Instantiate(unitPrefab, finalPos, Quaternion.identity);

        // --- ✨ 新增动画代码开始 ✨ ---
        var originalScale = newUnitObj.transform.localScale;
        // A. 先把单位缩小成 0 (看不见)
        newUnitObj.transform.localScale = Vector3.zero;

        // B. 用 0.5秒 的时间变大到 1 (原大小)

        // Ease.OutBack 会让它稍微放大超过 1，然后再缩回 1，产生果冻般的弹性
        newUnitObj.transform.DOScale(originalScale, animateDuration).SetEase(Ease.OutBack);
        // --- ✨ 新增动画代码结束 ✨ ---
        
        // 3. 获取角色组件

        // 3. 获取角色组件 (从生成的实例上获取，而不是改预制体！)
        var newUnitInstance = newUnitObj.GetComponent<CharacterBase>();

        if (newUnitInstance != null) 
        {
            // ✨ 强制绑定：防止 Awake 没执行或者执行顺序问题导致 stateManager 为空
            if (newUnitInstance.stateManager == null)
            {
                newUnitInstance.stateManager = newUnitInstance.GetComponent<CharacterStateManager>();
            }
            
            newUnitInstance.isEnemy = this.isEnemy;
            newUnitInstance.Initialize(new RuntimeItem(associatedCardData,ctx.caster));
            // 4. 注册到管理器 (注册实例，不是预制体)
            GameManager.Instance.RegisterUnit(newUnitInstance);
            
            // ✨ 补充：因为不是通过 PlayCard 打出的，我们需要手动触发那些“本来应该在打出时触发”的 Self Buff
            // 比如 "Battlecry: Gain Taunt" (Self)
            

            ctx.createdUnits.Add(newUnitInstance);//给予 拥有连续效果且有相关性的卡牌 关联。
            
            // ... RegisterUnit 之后 ...
            GameManager.Instance.OnBoardChanged(); // 每个人都要重新算一遍
          

        } else {
            Debug.LogError("你召唤的预制体上没有挂 CharacterBase 脚本！");
        }
        return animateDuration = 1f;;
    }
    

}
// 1. 定义召唤阵营模式
public enum SpawnSide {
    CasterSide,    // 召唤在施法者这一方 (常规随从)
    OpponentSide,  // 召唤在对手那一方 (给对面的负面随从/炸弹)
    ForcePlayer,   // 强制召唤在玩家这一边
    ForceEnemy     // 强制召唤在敌人那一边
}