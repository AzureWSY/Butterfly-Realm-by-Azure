using Unity.Cinemachine;
using UnityEngine;

// 这个类是抽象的，作为所有地板行为的基类
public abstract class FloorEffectSO : ScriptableObject
{
    [Header("基础配置")]
    public string effectName;
    [Header("是否会消耗")]
    public bool isOneTimeUse = false; // 是否是消耗品（如加血地板）
    [Header("恢复设置")]
    public bool canRecover = false;
    public float recoverTime = 3f;

    // 👑 核心扩展：这张图纸产生的效果，是否能被玩家的无敌状态免疫？
    // 默认所有的功能地板（回血、充能、弹簧）都是 false（不被无敌挡住）
    public virtual bool CanBeImmunedByInvincibility => false;

    // 🌟 核心魔法：抽象方法
    // 把玩家交互脚本传进来，让这个资产自己决定怎么折磨/奖励玩家
    public abstract void Execute(PlayerFloorInteraction player, AttributeFloor floor);
    public virtual void ExecuteExit(PlayerFloorInteraction player, AttributeFloor floor)
    {

    }
}