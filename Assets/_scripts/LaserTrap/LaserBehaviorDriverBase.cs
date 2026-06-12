using UnityEngine;

[RequireComponent(typeof(LaserTrapCore))]
public abstract class LaserBehaviorDriverBase : MonoBehaviour
{
    [Header("⏳ 基础行为通用配置")]
    public float startDelay = 0f;

    protected Vector3 startPos;
    protected float delayTimer;
    protected LaserTrapCore laserCore;
    protected float initialBaseDelay;

    protected virtual void Start()
    {
        startPos = transform.position;
        delayTimer = startDelay;
        initialBaseDelay = delayTimer;
        laserCore = GetComponent<LaserTrapCore>();

        // 开局向大总管报到
        if (LaserTrapManager.Instance != null)
        {
            LaserTrapManager.Instance.RegisterDriver(this);
        }
    }

    protected virtual void OnDestroy()
    {
        // 优雅注销，防止野指针崩溃
        if (LaserTrapManager.Instance != null)
        {
            LaserTrapManager.Instance.UnregisterDriver(this);
        }
    }

    // 每一帧由总管强行推入的生命周期出口
    public abstract void TickBehavior(float deltaTime, Vector3 targetTransform);
    // 确保在你的基类 LaserBehaviorDriverBase 内部加上这一行：
    public virtual void ResetDriverState()
    {
        // 基类留空，允许不同行为种类的激光各自实现自己的复位逻辑
    }
}