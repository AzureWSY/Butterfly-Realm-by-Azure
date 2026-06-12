using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacterStats", menuName = "ButterflyRealm/Base Stats")]
public class BaseStatsData : ScriptableObject
{
    [Header("配置数据 (硬盘存储)")]
    public string characterName;
    public int maxHealth = 100;

    // 如果是给玩家用，这个变量可以用来存进度；
    // 如果是给小怪用，这个变量只在代码里动态修改，不存硬盘。
    [HideInInspector] public int runtimeCurrentHealth;

    public virtual void Initialize()
    {
        runtimeCurrentHealth = maxHealth;
    }
}