using UnityEngine;
using DG.Tweening;

[CreateAssetMenu(fileName = "NewSpawnEffect", menuName = "CardEffects/Spawn Unit")]
public class SpawnUnitEffect : CardEffect {
    
    [Header("召唤配置")]
    public GameObject unitPrefab; // 要召唤的怪物的预制体
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
        var newUnit = newUnitObj.GetComponent<CharacterBase>();
        if (newUnit != null) 
        {
            
            newUnit.Initialize(associatedCardData);
            // 4. 注册到管理器
            GameManager.Instance.RegisterUnit(newUnit, isEnemy);
            
            ctx.createdUnits.Add(newUnit);//给予 拥有连续效果且有相关性的卡牌 关联。如召唤两个1/1，Buff最后召唤的+1+1.
            
            // ... RegisterUnit 之后 ...
            GameManager.Instance.OnBoardChanged(); // 每个人都要重新算一遍
          

        } else {
            Debug.LogError("你召唤的预制体上没有挂 CharacterBase 脚本！");
        }
        return animateDuration = 1f;;
    }
    

}