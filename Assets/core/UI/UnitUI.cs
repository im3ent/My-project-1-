using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class UnitUI : MonoBehaviour
{
    [Header("绑定目标")]
    private CharacterBase character;

    [Header("UI 组件")]
    public Slider hpSlider;
    public TextMeshProUGUI hpText;       // 显示 80/100

    public GameObject floatingTextPrefab;
    
    [Header("Buff 栏配置")]
    public Transform buffContainer;      // Grid Layout Group 的父物体
    public GameObject buffIconPrefab;    // 刚刚写的 StatusIcon Prefab

    private void Awake()
    {
        character = GetComponent<CharacterBase>();
    }

    private void Start()
    {
        if (character != null) Init(character);
    }

    // 初始化：订阅所有事件
    public void Init(CharacterBase unit)
    {
        character = unit;

        // 1. 订阅血量/护盾变化
        character.OnStatsChanged += UpdateStatsUI;
        character.OnHealthDelta += ShowDamagePopup;

        // 2. 订阅 Buff 变化
        // 注意：要通过 unit 找到它的 StateManager
        if (unit.stateManager != null)
        {
            //unit.stateManager.OnStatusChanged += UpdateBuffList;
        }

        // 3. 立即刷新一次，保证初始状态正确
        //UpdateStatsUI(unit);
        //UpdateBuffList();
    }

    private void OnDestroy()
    {
        // 🛑 必须取消订阅，防止报错
        if (character == null) return;
        character.OnStatsChanged -= UpdateStatsUI;
        character.OnHealthDelta -= ShowDamagePopup;
        if (character.stateManager != null)
        { 
            //character.stateManager.OnStatusChanged -= UpdateBuffList;
        }
    }

    // --- 逻辑 A: 刷新血条和护盾 ---
    void UpdateStatsUI(StatCTXForUI ctx)
    {
        // 血条滑块
        if (hpSlider != null)
            hpSlider.value = (float)ctx.newHpValue / ctx.oldHpValue;

        // 血量文字
        if (hpText != null)
            hpText.text = $"{ctx.newHpValue}/{ctx.oldHpValue}";
        
    }

    // --- 逻辑 B: 刷新 Buff 列表 ---
    void UpdateBuffList()
    {
        // 简单暴力法：先删光，再重建
        // (对于几百个单位的 RTS 游戏要用对象池，但对于卡牌游戏，这样写没问题)
        foreach (Transform child in buffContainer)
        {
            Destroy(child.gameObject);
        }

        if (character.stateManager == null) return;

        // 遍历所有当前 Buff
        foreach (var status in character.stateManager.statusList)
        {
            // 实例化图标
            GameObject iconObj = Instantiate(buffIconPrefab, buffContainer);
            //StatusIcon iconScript = iconObj.GetComponent<StatusIcon>();
            
            // 设置数据
            //iconScript.Setup(status);
        }
    }

    // --- 逻辑 C: 飘字 ---
    
    // 简单的对象池
    private Queue<GameObject> _floatingTextPool = new Queue<GameObject>();

    private void ReturnToPool(GameObject obj)
    {
        obj.SetActive(false);
        _floatingTextPool.Enqueue(obj);
    }

    void ShowDamagePopup(int delta, DamageInfo damageInfo)
    {
        // --- ✨ 新增：生成伤害飘字 ✨ ---
        if (floatingTextPrefab != null && delta < 0) // 只有掉血才飘字
        {
            // 1. 生成在头顶稍微高一点的位置
            var spawnPos = transform.position + Vector3.up * 1.5f; 
            
            GameObject popup;
            if (_floatingTextPool.Count > 0)
            {
                popup = _floatingTextPool.Dequeue();
                popup.transform.SetPositionAndRotation(spawnPos, Quaternion.identity);
                popup.SetActive(true);
            }
            else
            {
                // 2. 实例化
                popup = Instantiate(floatingTextPrefab, spawnPos, Quaternion.identity);
            }
            
            // 3. 初始化数字 (传入回收回调)
            popup.GetComponent<FloatingText>().Setup(delta, () => ReturnToPool(popup));
        }

    }
}