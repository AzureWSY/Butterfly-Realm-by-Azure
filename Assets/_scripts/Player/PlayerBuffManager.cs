using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(PlayerHealth))]
public class PlayerBuffManager : MonoBehaviour
{
    // 用一个列表把玩家身上的所有状态存起来
    private List<PlayerBuff> activeBuffs = new List<PlayerBuff>();
    private PlayerHealth healthSystem;

    private void Awake()
    {
        healthSystem = GetComponent<PlayerHealth>();
    }

    private void Start()
    {
        // 🌟 订阅死亡事件：玩家一死，清空所有状态！
        if (healthSystem != null)
            healthSystem.OnPlayerDeath += ClearAllBuffs;
    }

    private void OnDestroy()
    {
        if (healthSystem != null)
            healthSystem.OnPlayerDeath -= ClearAllBuffs;
    }

    private void Update()
    {
        // 倒序遍历列表，执行每一个 Buff 的逻辑。如果结束了，就拔除它
        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            activeBuffs[i].OnTick(); // 运行 Buff 的专属代码（比如倒计时）

            if (activeBuffs[i].IsFinished)
            {
                activeBuffs[i].OnRemove(); // 执行收尾工作（比如恢复摄像机）
                activeBuffs.RemoveAt(i);   // 从列表中剔除
            }
        }
    }

    // 暴露给外部（SO 图纸）的添加 Buff 方法
    public void AddBuff(PlayerBuff newBuff)
    {
        newBuff.Initialize(this.gameObject);
        newBuff.OnApply();
        activeBuffs.Add(newBuff);
    }

    // 死亡重置核心：清理状态 + 刷新地板
    public void ClearAllBuffs()
    {
        foreach (var buff in activeBuffs)
        {
            buff.OnRemove(); // 强制让所有 Buff 还原状态
        }
        activeBuffs.Clear();

        // 🌟 找到场景里所有被踩过的地板，强制充能
        AttributeFloor[] allFloors = Object.FindObjectsByType<AttributeFloor>(FindObjectsSortMode.None);
        foreach (var floor in allFloors)
        {
            floor.ForceReset();
        }
        Debug.Log("[系统]: 玩家死亡，已清空所有状态并重置所有属性地板！");
    }
}