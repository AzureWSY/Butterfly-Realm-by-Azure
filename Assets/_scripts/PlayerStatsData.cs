using UnityEngine;

// 右键菜单创建入口
[CreateAssetMenu(fileName = "NewPlayerStats", menuName = "ButterflyRealm/Player Stats Data")]
public class PlayerStatsData : BaseStatsData
{
    [Header("玩家专属配置")]
    public float invincibilityDuration = 1f;

    [Header("跨场景持久化数据")]
    public int currentHealth; // 玩家的当前血量存在这里，保证切场景不丢失

    // 重写初始化方法：只有在“新开存档”或“完全复活”时，才调用这个方法把血回满
    public override void Initialize()
    {
        base.Initialize(); // 可选的基类调用
        currentHealth = maxHealth;
    }
}