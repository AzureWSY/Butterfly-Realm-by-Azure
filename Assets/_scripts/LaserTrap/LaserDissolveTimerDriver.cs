using UnityEngine;

public class LaserDissolveTimerDriver : LaserBehaviorDriverBase
{
    private enum TimerState { On, Off }

    [Header("? 原地溶解闪烁时钟配置")]
    public float timeOn = 3.0f;
    public float timeOff = 2.0f;

    private TimerState currentState = TimerState.On;
    private float stateTimer = 0f;

    protected override void Start()
    {
        base.Start();
        stateTimer = timeOn; // 默认开局处于亮起状态
    }

    public override void TickBehavior(float deltaTime, Vector3 targetTransform)
    {
        // 处理开局初始延迟
        if (delayTimer > 0f)
        {
            delayTimer -= deltaTime;
            if (delayTimer <= 0f && laserCore != null) laserCore.TurnOnLaser();
            return;
        }

        stateTimer -= deltaTime;
        if (stateTimer <= 0f)
        {
            if (currentState == TimerState.On)
            {
                currentState = TimerState.Off;
                stateTimer = timeOff;
                if (laserCore != null) laserCore.TurnOffLaser();
            }
            else
            {
                currentState = TimerState.On;
                stateTimer = timeOn;
                if (laserCore != null) laserCore.TurnOnLaser();
            }
        }
    }
    /// <summary>
    /// 👑 多态重写：定时闪烁时钟重新对齐清洗
    /// </summary>
    public override void ResetDriverState()
    {
        // 1. 基类延迟参数强行拉回复活开局
        delayTimer = initialBaseDelay;

        // 2. 状态机与核心时钟沙漏完美重置
        currentState = TimerState.On;
        stateTimer = timeOn;

        // 3. 防御性视效校准：如果开局有初始波浪延迟，先关闭激光；没延迟则必须立马亮起
        if (laserCore != null)
        {
            if (delayTimer > 0f) laserCore.TurnOffLaser();
            else laserCore.TurnOnLaser();
        }
    }
}