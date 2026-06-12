using UnityEngine;

// 这是一个纯 C# 类，作为所有状态效果（Buff）的父类
public abstract class PlayerBuff
{
    // 标记这个 Buff 是否已经结束，如果为 true，管理器会自动销毁它
    public bool IsFinished { get; protected set; } = false;

    protected GameObject playerObj;

    // 初始化时传入玩家物体
    public virtual void Initialize(GameObject player)
    {
        this.playerObj = player;
    }

    // 1. 当刚获得 Buff（刚踩到地板）时执行
    public abstract void OnApply();

    // 2. 每帧执行（用于倒计时、监听按键等）
    public abstract void OnTick();

    // 3. 当 Buff 结束（时间到、或玩家死亡）时执行，用于清理收尾
    public abstract void OnRemove();
}