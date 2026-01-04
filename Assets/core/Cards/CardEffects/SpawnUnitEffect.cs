using UnityEngine;

[CreateAssetMenu(fileName = "NewSpawnEffect", menuName = "CardEffects/Spawn Unit")]
public class SpawnUnitEffect : CardEffect {
    
    [Header("召唤配置")]
    public GameObject unitPrefab; // 要召唤的怪物的预制体
    public bool isEnemy = false;  // 是召唤给敌人(内鬼/小怪)，还是召唤给自己(随从)？
    public Vector3 spawnOffset = new Vector3(2, 0, 0); // 相对生成点的偏移量

    public override void Execute(CharacterBase user, CharacterBase target) {
        // 1. 决定生成位置
        // 如果是敌人，生成在 EnemySpawnZone；如果是友军，生成在 AllySpawnZone
        var zone = isEnemy ? GameManager.Instance.enemySpawnZone : GameManager.Instance.allySpawnZone;
        
        // 简单的随机偏移，防止完全重叠
        var finalPos = zone.position + new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0);

        // 2. 生成物体
        var newUnitObj = Instantiate(unitPrefab, finalPos, Quaternion.identity);
        
        // 3. 获取角色组件
        var newUnit = newUnitObj.GetComponent<CharacterBase>();
        if (newUnit != null) 
        {
            
            newUnit.Initialize();
            // 4. 注册到管理器
            GameManager.Instance.RegisterUnit(newUnit, isEnemy);
            Debug.Log($"召唤了 {newUnit.name} ({(isEnemy ? "敌方" : "友方")})");
        } else {
            Debug.LogError("你召唤的预制体上没有挂 CharacterBase 脚本！");
        }
    }
}