using System.Collections.Generic;
using UnityEngine;

public class LaserTrapManager : MonoBehaviour
{
    public static LaserTrapManager Instance { get; private set; }

    // 1. 行为逻辑层队列
    private readonly List<LaserBehaviorDriverBase> activeBehaviorDrivers = new List<LaserBehaviorDriverBase>();

    // 2. 表现/物理重绘层哈希脏队列
    private readonly HashSet<LaserTrapCore> dirtyGeometryQueue = new HashSet<LaserTrapCore>();

    // 🌟 解耦核心：由外部注入进来的追踪重心目标，管家本身不主动高频向上索取单例
    private bool hasTarget = false;
    private Vector3 cachedPlayerCenter;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void RegisterDriver(LaserBehaviorDriverBase driver) => activeBehaviorDrivers.Add(driver);
    public void UnregisterDriver(LaserBehaviorDriverBase driver) => activeBehaviorDrivers.Remove(driver);
    public void RegisterDirtyCore(LaserTrapCore core) => dirtyGeometryQueue.Add(core);

    /// <summary>
    /// 📥 🌟 依赖注入接口：由全局 GameManager 或者是关卡总控，在每帧或者初始化时主动把目标中心发进来
    /// </summary>
    public void UpdateTrackTarget(Vector3 targetCenter)
    {
        cachedPlayerCenter = targetCenter;
        hasTarget = true;
    }

    /// <summary>
    /// ⚡ 全局复位熔断：由 GameManager 在玩家死后统一呼叫
    /// </summary>
    public void ResetAllDrivers()
    {
        int driverCount = activeBehaviorDrivers.Count;
        for (int i = 0; i < driverCount; i++)
        {
            if (activeBehaviorDrivers[i] != null)
            {
                activeBehaviorDrivers[i].ResetDriverState();
            }
        }
    }

    private void Update()
    {
        // 🌟 纯净化防御：如果外部还没准备好、或者这一帧没有喂目标进来，激光管家直接挂机，绝不报错顶牛
        if (!hasTarget) return;

        // ==========================================
        // 阶段一：多态行为驱动逻辑处理 ( Logic Tick )
        // ==========================================
        int driverCount = activeBehaviorDrivers.Count;
        float deltaTime = Time.deltaTime;

        for (int i = 0; i < driverCount; i++)
        {
            if (activeBehaviorDrivers[i] != null)
            {
                // 直接使用本地缓存好的纯净 Vector3 坐标，全场没有任何 GetComponent 或单例交叉！
                activeBehaviorDrivers[i].TickBehavior(deltaTime, cachedPlayerCenter);
            }
        }

        // ==========================================
        // 阶段二：集中式脏标识批处理
        // ==========================================
        if (dirtyGeometryQueue.Count > 0)
        {
            foreach (var dirtyCore in dirtyGeometryQueue)
            {
                if (dirtyCore != null) dirtyCore.ResolveGeometryUpdate();
            }
            dirtyGeometryQueue.Clear();
        }
    }
}