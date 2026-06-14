using System.Collections.Generic;
using UnityEngine;

public class LaserChaseDriver : LaserMovementDriverBase
{
    [Header("🎯 追击基本配置")]
    public float chaseSpeed = 8f;
    [Range(0.05f, 0.5f)]
    public float reactionDelay = 0.2f;

    [Header("🧭 目标偏移量")]
    public Vector2 targetOffset;

    [Header("🎛️ 追踪轴向开关")]
    public bool trackX = true;
    public bool trackY = true;

    [Header("🛡️ 物理与障碍安全层")]
    public LayerMask obstacleLayer;
    public float skinWidth = 0.015f;

    [Header("🤖 导航代理尺寸 (解耦核心)")]
    public Vector2 navigationSize = new Vector2(0.3f, 0.3f);
    public Vector2 navigationOffset = Vector2.zero;

    [Header("🧩 解谜机关稳定层 (Puzzle Constraint Layer)")]
    public float loopCheckRadius = 0.25f;
    public float maxZoneLoopTime = 1.2f;
    public float proactiveSlideBias = 2.5f;

    [Header("🔛 机关联动初始状态")]
    [Tooltip("勾选此项则出生立刻可见并激活追踪。如果不勾选，则默认隐形且静默，等待外部触发器调用 ActivateLaserTracker() 唤醒。")]
    public bool startActive = false;

    private readonly RaycastHit2D[] castResults = new RaycastHit2D[12];

    // 解谜层空间循环状态持有人
    private Vector2 lastZoneCenter;
    private float zoneTimer = 0f;
    private int currentStuckFrames = 0;

    // 出厂初始状态备份保险箱
    private Vector3 startPosition;
    private bool hasInitStartPos = false;
    

    // 运行期核心激活状态闸门
    private bool isTrackingActive = false;

    // =========================================================================
    // 🌟 你的 targetHistory 在这里！高精度双点插值时间轴数据容器
    // =========================================================================
    private struct TargetSnapshot
    {
        public Vector3 position;
        public float time;
    }
    // 改用 List 代替 Queue，彻底解锁 [i] 随机索引读取能力，用零 GC 的开销完成时间轴 Lerp
    private readonly List<TargetSnapshot> targetHistory = new List<TargetSnapshot>();

    protected override void Start()
    {
        base.Start();

        // 🌟 核心升级：根据面板配置初始化激活状态与可见性（完美实现初始隐形）
        isTrackingActive = startActive;
        SetLaserVisibility(startActive);

        if (laserCore != null)
        {
            startPosition = laserCore.transform.position;
            lastZoneCenter = startPosition;
            hasInitStartPos = true;
        }
        initialBaseDelay = delayTimer;
    }

    public override void TickBehavior(float deltaTime, Vector3 targetPosition)
    {
        // 🌟 核心控制闸门：如果当前追踪器未激活，直接熔断返回，不浪费一丁点物理和追踪算力
        if (!isTrackingActive) return;

        if (delayTimer > 0f)
        {
            delayTimer -= deltaTime;
            return;
        }

        if (laserCore == null) return;

        if (!hasInitStartPos)
        {
            startPosition = laserCore.transform.position;
            lastZoneCenter = startPosition;
            hasInitStartPos = true;
        }

        // 🌟 职责1应用：高频打点，将大管家传进来的清白物理中心录入时间轴
        RecordTarget(targetPosition);

        // 🌟 职责2应用：抽丝剥茧，提取出高精度双点线性插值后的平滑历史虚影
        Vector3 delayedTargetCenter = GetInterpolatedDelayedTarget(targetPosition);
        delayedTargetCenter += (Vector3)targetOffset;

        Vector3 currentPos = laserCore.transform.position;

        // 轴向分离
        Vector3 desiredTarget = currentPos;
        if (trackX) desiredTarget.x = delayedTargetCenter.x;
        if (trackY) desiredTarget.y = delayedTargetCenter.y;

        Vector2 totalMovement = (Vector2)Vector3.MoveTowards(currentPos, desiredTarget, chaseSpeed * deltaTime) - (Vector2)currentPos;

        // 区域锁死监控
        if (Vector2.Distance(currentPos, lastZoneCenter) < loopCheckRadius)
        {
            if (totalMovement.magnitude > 0.001f) zoneTimer += deltaTime;
        }
        else
        {
            lastZoneCenter = currentPos;
            zoneTimer = 0f;
        }

        Vector3 finalPos = ExecutePuzzleProjectionMove(currentPos, totalMovement, targetPosition, deltaTime);

        float expectedDist = totalMovement.magnitude;
        float actualDist = Vector2.Distance(currentPos, finalPos);

        if (expectedDist > 0.001f && (actualDist / deltaTime) < 0.05f) currentStuckFrames++;
        else currentStuckFrames = Mathf.Max(0, currentStuckFrames - 1);

        laserCore.transform.position = finalPos;
        laserCore.MarkGeometryDirty();
    }

    /// <summary>
    /// 👑 🌟 职责3应用：多态重写！当大管家呼叫全局洗牌时，原子级清洗 targetHistory 历史记忆
    /// </summary>
    public override void ResetDriverState()
    {
        if (!hasInitStartPos || laserCore == null) return;

        // 1. 还原基类初始延迟
        delayTimer = initialBaseDelay;

        // 2. 物理位置瞬间归位
        laserCore.transform.position = startPosition;

        // 3. 🔥 核心绝杀：彻底清洗时间轴队列！防止复活瞬间读取到死前的残余历史帧引发大瞬移穿帮
        targetHistory.Clear();

        // 4. 重置解谜层锁死计数
        lastZoneCenter = startPosition;
        zoneTimer = 0f;
        currentStuckFrames = 0;

        // 5. 还原初始激活与隐藏配置
        isTrackingActive = startActive;
        SetLaserVisibility(startActive);

        if (startActive)
        {
            laserCore.MarkGeometryDirty();
        }
    }

    /// <summary>
    /// 📝 往时间轴内无污染追加最新坐标切片
    /// </summary>
    private void RecordTarget(Vector3 targetPos)
    {
        targetHistory.Add(new TargetSnapshot { position = targetPos, time = Time.time });

        // 限制缓冲区上限在 60 帧内（约 1秒的历史跨度），多余的旧数据直接从尾部零开销擦除
        while (targetHistory.Count > 60)
        {
            targetHistory.RemoveAt(0);
        }
    }

    /// <summary>
    /// 📈 基于 List 随机访问特性的双点时间轴线性插值器
    /// </summary>
    private Vector3 GetInterpolatedDelayedTarget(Vector3 fallback)
    {
        float targetTime = Time.time - reactionDelay;
        if (targetHistory.Count == 0) return fallback;
        if (targetHistory[0].time >= targetTime) return targetHistory[0].position;
        if (targetHistory[targetHistory.Count - 1].time <= targetTime) return targetHistory[targetHistory.Count - 1].position;

        // 🌟 正因为升级为了 List，我们才能高效率地利用 i 和 i+1 读取前后紧密相连的两帧！
        for (int i = 0; i < targetHistory.Count - 1; i++)
        {
            TargetSnapshot leftFrame = targetHistory[i];
            TargetSnapshot rightFrame = targetHistory[i + 1];

            if (targetTime >= leftFrame.time && targetTime <= rightFrame.time)
            {
                float timeDelta = rightFrame.time - leftFrame.time;
                float t = timeDelta > 0f ? (targetTime - leftFrame.time) / timeDelta : 0f;

                // 执行高动态插值补间，保证哪怕帧率产生细微晃动，抓出来的路径也是完美连续平滑的
                return Vector3.Lerp(leftFrame.position, rightFrame.position, t);
            }
        }
        return fallback;
    }

    private Vector3 ExecutePuzzleProjectionMove(Vector3 currentPos, Vector2 initialMovement, Vector3 realPlayerCenter, float deltaTime)
    {
        float currentAngle = laserCore.transform.eulerAngles.z;
        Vector2 workingPos = currentPos;
        Vector2 workingVelocity = initialMovement;

        bool hitObstacleThisPass = false;
        Vector2 cachedHitNormal = Vector2.zero;

        for (int bounce = 0; bounce < 3; bounce++)
        {
            float distance = workingVelocity.magnitude;

            if (distance < 0.0001f)
            {
                if (initialMovement.magnitude > 0.001f && hitObstacleThisPass)
                {
                    bool isHeadOnCollision = Vector2.Dot(initialMovement.normalized, cachedHitNormal) < -0.98f;
                    bool isSpatialLocked = zoneTimer >= maxZoneLoopTime;

                    if (isHeadOnCollision || isSpatialLocked)
                    {
                        Vector2 tangent1 = new Vector2(-cachedHitNormal.y, cachedHitNormal.x);
                        Vector2 tangent2 = -tangent1;

                        Vector2 toRealPlayerCenter = (Vector2)realPlayerCenter - workingPos;
                        Vector2 preferredTangent = (Vector2.Dot(tangent1, toRealPlayerCenter) >= Vector2.Dot(tangent2, toRealPlayerCenter)) ? tangent1 : tangent2;

                        workingVelocity = preferredTangent * proactiveSlideBias * deltaTime;
                        distance = workingVelocity.magnitude;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }

            Vector2 direction = workingVelocity.normalized;
            Vector2 worldCenter = workingPos + (Vector2)laserCore.transform.TransformDirection(navigationOffset);

            ContactFilter2D filter = new ContactFilter2D();
            filter.useLayerMask = true;
            filter.layerMask = obstacleLayer;

            int hitCount = Physics2D.BoxCast(worldCenter, navigationSize, currentAngle, direction, filter, castResults, distance + skinWidth);

            RaycastHit2D closestHit = new RaycastHit2D();
            float minDistance = float.MaxValue;
            bool foundValidObstacle = false;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit2D hit = castResults[i];
                if (hit.collider == null || hit.collider.gameObject == laserCore.gameObject || hit.distance <= 0f)
                    continue;

                if (hit.distance < minDistance)
                {
                    minDistance = hit.distance;
                    closestHit = hit;
                    foundValidObstacle = true;
                }
            }

            if (foundValidObstacle)
            {
                hitObstacleThisPass = true;
                cachedHitNormal = closestHit.normal;

                float safeDist = Mathf.Max(0f, closestHit.distance - skinWidth);
                workingPos += direction * safeDist;

                Vector2 remainingMovement = direction * (distance - safeDist);
                workingVelocity = remainingMovement - Vector2.Dot(remainingMovement, closestHit.normal) * closestHit.normal;

                workingPos += closestHit.normal * 0.0015f;
            }
            else
            {
                workingPos += workingVelocity;
                break;
            }
        }

        return new Vector3(workingPos.x, workingPos.y, currentPos.z);
    }

    // =========================================================================
    // 🔓 外部解谜/机关联动专属核心 API (External Trigger Matrix)
    // =========================================================================

    /// <summary>
    /// 👁️ 内部核心高稳定性显示切件开关
    /// </summary>
    private void SetLaserVisibility(bool visible)
    {
        if (laserCore != null)
        {
            // 直接控制核心物件显隐。隐形时不仅看不到渲染，它的 Collider 也将直接失效，不会对玩家误造成伤害
            laserCore.gameObject.SetActive(visible);
        }
    }

    /// <summary>
    /// 📡 供外部压力板、开关或区域检测 Trigger 调用：一键解除隐形并激活追踪器
    /// </summary>
    public void ActivateLaserTracker()
    {
        if (isTrackingActive) return;

        isTrackingActive = true;

        // 🌟 瞬间解除隐形，显示画面并开启物理碰撞判定
        SetLaserVisibility(true);

        zoneTimer = 0f;
        currentStuckFrames = 0;

        // 🔥 细节设计：被激活的瞬间当场清洗历史队列！
        // 这样可以确保它不会去读取“出生前/隐形时”累积的历史玩家死坐标，防止激活显现瞬间产生穿帮拉伸。
        targetHistory.Clear();

        if (laserCore != null)
        {
            laserCore.MarkGeometryDirty();
        }

        Debug.Log($"<color=#00FFFF>[LaserChaseDriver]</color> 接收到机关通路激活信号！激光已解除隐形，全面启动追击。");
    }

    /// <summary>
    /// 💥 一键彻底物理销毁该激光（绝不伤害父节点！）
    /// </summary>
    public void DestroyLaser()
    {
        Debug.Log($"<color=#FF0055>[LaserChaseDriver]</color> 接收到毁灭断路信号！正在阻断追踪并释放激光核心物体。");

        // 1. 瞬间熔断状态闸门并隐藏
        isTrackingActive = false;
        SetLaserVisibility(false);

        // 2. 👑 绝对安全的原子级销毁：仅干掉激光的核心物体（laserCore），绝不触碰和伤害任何父节点层级！
        if (laserCore != null)
        {
            Destroy(laserCore.gameObject);
        }

        // 3. 随后自毁当前的驱动脚本组件，防止后续代码继续空指针空跑，同时保证挂载的宿主GameObject完好无损
        Destroy(this);
    }
    private void OnDrawGizmosSelected()
    {
        if (laserCore == null) return;

        Vector3 laserPos = laserCore.transform.position;

        Vector2 worldCenter =
            (Vector2)laserPos +
            (Vector2)laserCore.transform.TransformDirection(navigationOffset);

        // 导航连接线
        Gizmos.color = Color.white;
        Gizmos.DrawLine(laserPos, worldCenter);

        // 导航中心点
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(worldCenter, 0.05f);

        // 导航盒
        Gizmos.color =
            zoneTimer >= maxZoneLoopTime
            ? Color.red
            : Color.green;

        Matrix4x4 oldMatrix = Gizmos.matrix;

        Gizmos.matrix = Matrix4x4.TRS(
            worldCenter,
            laserCore.transform.rotation,
            Vector3.one);

        Gizmos.DrawWireCube(
            Vector3.zero,
            new Vector3(
                navigationSize.x,
                navigationSize.y,
                0.05f));

        Gizmos.matrix = oldMatrix;

        // loop检测范围
        Gizmos.color = new Color(1f, 0.8f, 0f, 0.5f);
        Gizmos.DrawWireSphere(lastZoneCenter, loopCheckRadius);
    }
}