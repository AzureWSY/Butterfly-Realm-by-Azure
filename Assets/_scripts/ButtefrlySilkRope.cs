using System;
using System.Collections.Generic;
using UnityEngine;


[ExecuteAlways]
[RequireComponent(typeof(LineRenderer))]
public class ButterflySilkRope : MonoBehaviour, ICullable
{

    /// <summary>
    /// 连续平铺的纯值类型质点结构体（提升 CPU Cache 命中率并实现零 GC）
    /// </summary>
    [Serializable]
    public struct VerletNode
    {
        public Vector2 current;
        public Vector2 previous;

        public VerletNode(Vector2 initialPosition)
        {
            current = initialPosition;
            previous = initialPosition;
        }
    }

    [Header("📌 锚点设置")]
    [SerializeField] private Transform startAnchor;
    [SerializeField] private Transform endAnchor;

    [Header("🧵 蝶丝物理属性")]
    [Range(3, 60)]
    [SerializeField] private int segmentCount = 25;
    [Range(1, 30)]
    [SerializeField] private int constraintIterations = 15;
    [SerializeField] private Vector2 gravity = new Vector2(0f, -2.5f);
    [Range(0.85f, 0.999f)]
    [SerializeField] private float damping = 0.97f; // 动量衰减
    [SerializeField] private float restLengthMultiplier = 1.0f; // 绳体松弛度

    [Header("🍃 微风扰动模拟")]
    [SerializeField] private float windStrength = 0.8f;
    [SerializeField] private float windFrequency = 2.0f;

    [Header("✨ 定向粒子区域 (OBB)")]
    [SerializeField] private ParticleSystem silkParticleSystem;
    [Tooltip("X: 沿绳轴向两端余量, Y: 垂直绳体法向厚度余量")]
    [SerializeField] private Vector2 particlePadding = new Vector2(0.35f, 0.2f);



    // 🌟 优化4：使用单一连续平铺的值类型数组，榨干 CPU L1/L2 缓存行
    private VerletNode[] nodes;
    private Vector3[] renderBuffer;
    private LineRenderer lineRenderer;
    private float targetSegmentLength;

    // 🌟 优化1：记录历史时间步，用于动态帧率变步长动量校准
    private float prevDeltaTime = 0.02f;

    // 🌟 性能核心：视距剔除状态标记（控制是否执行昂贵的 Verlet 物理迭代）
    private bool isCulled = false;

    // 预分配定长 5 个采样特征点（起点、终点、中点、1/4点、3/4点），彻底解决超长绳索单点盲区，且零 GC
    private readonly Vector3[] cullPoints = new Vector3[5];

    /// <summary>
    /// 实现通用 ICullable 接口：多点采样覆盖整条超长绳索。
    /// 无论玩家靠近哪个锚点、或绳索横贯屏幕，只要任意一段进入视野+过渡区即刻唤醒！
    /// </summary>
    public Vector3[] CullCheckPoints
    {
        get
        {
            Vector3 start = startAnchor != null ? startAnchor.position : transform.position;
            Vector3 end = endAnchor != null ? endAnchor.position : transform.position;

            cullPoints[0] = start;
            cullPoints[1] = end;
            cullPoints[2] = (start + end) * 0.5f;
            cullPoints[3] = (start * 0.75f) + (end * 0.25f);
            cullPoints[4] = (start * 0.25f) + (end * 0.75f);

            return cullPoints;
        }
    }

    // 供外部逻辑或编辑器快捷读取的只读点集映射
    public int PointCount => nodes != null ? nodes.Length : 0;
    public Vector2 GetPoint(int index) => nodes[index].current;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.useWorldSpace = true;
    }

    private void OnEnable()
    {
        // 自动向全局通用剔除器注册
        FloorVisibilityCuller.Register(this);
    }

    private void OnDisable()
    {
        // 失活时自动注销
        FloorVisibilityCuller.Unregister(this);
    }

    /// <summary>
    /// 实现通用 ICullable 接口响应：由视锥剔除管理器驱动
    /// </summary>
    public void OnCullingStateChanged(bool isVisible)
    {
        bool shouldCull = !isVisible;
        if (isCulled == shouldCull) return;
        isCulled = shouldCull;

        // 视野外直接关闭 LineRenderer 渲染与粒子发射
        if (lineRenderer != null)
        {
            lineRenderer.enabled = isVisible;
        }

        if (silkParticleSystem != null)
        {
            var emission = silkParticleSystem.emission;
            emission.enabled = isVisible;
        }

        // 重新进入视野时，校准历史时间步长，防止 Verlet 物理瞬间产生突变拉扯
        if (isVisible)
        {
            prevDeltaTime = 0.02f;
        }
    }

    private void Start()
    {
        InitializeRopeNodes();
    }

    private void OnValidate()
    {
        if (segmentCount < 3) segmentCount = 3;
        InitializeRopeNodes();
    }

    private void Update()
    {
        // 🌟 性能终极拦截：处于视野外时，彻底跳过所有物理模拟、约束求解与顶点网格重绘！
        if (Application.isPlaying && isCulled) return;

        if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
        if (startAnchor == null || endAnchor == null) return;

        // 节点数组失效或长度变动时自愈重建
        if (nodes == null || nodes.Length != segmentCount)
        {
            InitializeRopeNodes();
        }

        // 🌟 优化2：利用宏定义安全隔离 Editor 专用时钟，打包 Standalone 时绝不暴露 UnityEditor 程序集
        float dt;
#if UNITY_EDITOR
        dt = Application.isPlaying ? Time.deltaTime : 0.02f;
#else
        dt = Time.deltaTime;
#endif

        if (dt <= 0f) return;

        // 1. 变时间步长校准的 Verlet 积分
        SimulateVerletWithTimeCorrection(dt);

        // 2. 几何距离约束迭代
        SolveConstraints();

        // 3. 提交 LineRenderer 渲染
        RenderSilk();

        // 4. 定向包围盒（OBB）拟合粒子区域
        SyncOrientedParticleBounds();

        // 更新历史时间步
        prevDeltaTime = dt;
    }

    /// <summary>
    /// 初始化连续内存平铺的质点数组
    /// </summary>
    private void InitializeRopeNodes()
    {
        if (startAnchor == null || endAnchor == null) return;

        nodes = new VerletNode[segmentCount];
        renderBuffer = new Vector3[segmentCount];

        Vector2 startPos = startAnchor.position;
        Vector2 endPos = endAnchor.position;
        float totalDist = Vector2.Distance(startPos, endPos);
        targetSegmentLength = (totalDist / (segmentCount - 1)) * restLengthMultiplier;

        for (int i = 0; i < segmentCount; i++)
        {
            float t = (float)i / (segmentCount - 1);
            Vector2 pos = Vector2.Lerp(startPos, endPos, t);
            nodes[i] = new VerletNode(pos);
        }
    }

    /// <summary>
    /// 变时间步长校正的 Verlet 积分与空间连续风场
    /// </summary>
    private void SimulateVerletWithTimeCorrection(float dt)
    {
        Vector2 startPos = startAnchor.position;
        Vector2 endPos = endAnchor.position;

        // 🌟 优化2：编辑器与运行时统一时钟隔离
#if UNITY_EDITOR
        float time = Application.isPlaying ? Time.time : (float)UnityEditor.EditorApplication.timeSinceStartup;
#else
        float time = Time.time;
#endif

        // 🌟 优化1：动态帧率变动下的动量补偿系数 (dt / prevDeltaTime)
        float timeStepRatio = prevDeltaTime > 1e-5f ? (dt / prevDeltaTime) : 1.0f;
        float effectiveDamping = damping * timeStepRatio;

        for (int i = 0; i < segmentCount; i++)
        {
            if (i == 0 || i == segmentCount - 1)
            {
                Vector2 anchorPos = (i == 0) ? startPos : endPos;
                nodes[i].current = anchorPos;
                nodes[i].previous = anchorPos;
                continue;
            }

            Vector2 current = nodes[i].current;
            Vector2 previous = nodes[i].previous;

            // 经过时间缩放补偿后的动量惯性
            Vector2 velocity = (current - previous) * effectiveDamping;

            // 双向 Perlin 空间风扰
            float noiseX = Mathf.PerlinNoise(time * windFrequency, i * 0.15f) - 0.5f;
            float noiseY = Mathf.PerlinNoise(i * 0.15f, time * windFrequency) - 0.5f;
            Vector2 windAccel = new Vector2(noiseX, noiseY) * (windStrength * 10f);

            Vector2 totalAccel = gravity + windAccel;

            // x(t + dt) = x(t) + v * (dt / dt_prev) + a * dt^2
            Vector2 next = current + velocity + totalAccel * (dt * dt);

            nodes[i].previous = current;
            nodes[i].current = next;
        }
    }

    /// <summary>
    /// 距离约束松弛（Gauss-Seidel 迭代）
    /// </summary>
    private void SolveConstraints()
    {
        Vector2 startPos = startAnchor.position;
        Vector2 endPos = endAnchor.position;

        float currentDist = Vector2.Distance(startPos, endPos);
        targetSegmentLength = (currentDist / (segmentCount - 1)) * restLengthMultiplier;

        for (int iter = 0; iter < constraintIterations; iter++)
        {
            nodes[0].current = startPos;
            nodes[segmentCount - 1].current = endPos;

            for (int i = 0; i < segmentCount - 1; i++)
            {
                Vector2 delta = nodes[i + 1].current - nodes[i].current;
                float dist = delta.magnitude;
                if (dist < 1e-6f) continue;

                float error = (dist - targetSegmentLength) / dist;
                Vector2 correction = delta * (0.5f * error);

                if (i == 0)
                {
                    nodes[i + 1].current -= correction * 2f;
                }
                else if (i + 1 == segmentCount - 1)
                {
                    nodes[i].current += correction * 2f;
                }
                else
                {
                    nodes[i].current += correction;
                    nodes[i + 1].current -= correction;
                }
            }
        }
    }

    /// <summary>
    /// 提交顶点到 LineRenderer 渲染管线
    /// </summary>
    private void RenderSilk()
    {
        if (lineRenderer.positionCount != segmentCount)
        {
            lineRenderer.positionCount = segmentCount;
        }

        float z = transform.position.z;
        for (int i = 0; i < segmentCount; i++)
        {
            renderBuffer[i].x = nodes[i].current.x;
            renderBuffer[i].y = nodes[i].current.y;
            renderBuffer[i].z = z;
        }

        lineRenderer.SetPositions(renderBuffer);
    }

    /// <summary>
    /// 🌟 优化3：定向包围盒（OBB）投影拟合算法，彻底消灭斜向 45° 粒子盒面积膨胀
    /// </summary>
    private void SyncOrientedParticleBounds()
    {
        if (silkParticleSystem == null || nodes == null || nodes.Length == 0) return;

        Vector2 startPos = startAnchor.position;
        Vector2 endPos = endAnchor.position;
        Vector2 baseline = endPos - startPos;
        float baseDistance = baseline.magnitude;

        // 两锚点重合时的退化防御
        if (baseDistance < 1e-4f) return;

        // 1. 构建以锚点连线为基准的局部正交基底 (u: 沿线切向, v: 垂直法向)
        Vector2 uAxis = baseline / baseDistance;
        Vector2 vAxis = new Vector2(-uAxis.y, uAxis.x); // 正交法线

        // 2. 将所有质点坐标投影到局部正交基上，求解极值
        float minU = float.MaxValue, maxU = float.MinValue;
        float minV = float.MaxValue, maxV = float.MinValue;

        for (int i = 0; i < nodes.Length; i++)
        {
            Vector2 offset = nodes[i].current - startPos;
            float u = Vector2.Dot(offset, uAxis);
            float v = Vector2.Dot(offset, vAxis);

            if (u < minU) minU = u;
            if (u > maxU) maxU = u;
            if (v < minV) minV = v;
            if (v > maxV) maxV = v;
        }

        // 3. 计算局部空间下的包围盒尺寸与中心
        float lengthU = (maxU - minU) + (particlePadding.x * 2f);
        float thicknessV = (maxV - minV) + (particlePadding.y * 2f);

        lengthU = Mathf.Max(lengthU, 0.2f);
        thicknessV = Mathf.Max(thicknessV, 0.1f);

        float midU = (minU + maxU) * 0.5f;
        float midV = (minV + maxV) * 0.5f;

        // 4. 将中心坐标逆变换回世界坐标系
        Vector2 worldCenter = startPos + (uAxis * midU) + (vAxis * midV);
        float angleZ = Mathf.Atan2(uAxis.y, uAxis.x) * Mathf.Rad2Deg;

        // 5. 将定向包围盒矩阵应用至粒子系统
        Transform psTrans = silkParticleSystem.transform;
        psTrans.position = new Vector3(worldCenter.x, worldCenter.y, transform.position.z);
        psTrans.rotation = Quaternion.Euler(0f, 0f, angleZ);

        var shape = silkParticleSystem.shape;
        if (shape.enabled)
        {
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(lengthU, thicknessV, 0.2f);
        }
    }
}