using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 🌟 通用屏幕视锥与过渡区剔除管理器（Universal Viewport Culler）
/// 动态获取主摄像机的正交视野（Orthographic Bounds），并在外围附加“安全过渡区（Padding Zone）”。
/// 自动对视野外的地砖视觉、Verlet物理绳索等昂贵组件执行批量休眠，进入视野秒级唤醒，
/// 从根本上杜绝 CPU 物理空转与 GPU 2D 光照 / 网格过载。
/// </summary>
public class FloorVisibilityCuller : MonoBehaviour
{
    public static FloorVisibilityCuller Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic() => Instance = null;

    [Header("📐 屏幕视野过渡区配置")]
    [Tooltip("屏幕左右两侧外延的水平缓冲过渡区距离（单位：世界坐标）")]
    [SerializeField] private float paddingX = 5.0f;

    [Tooltip("屏幕上下两侧外延的垂直缓冲过渡区距离（单位：世界坐标）")]
    [SerializeField] private float paddingY = 4.0f;

    [Tooltip("完整扫描一轮的目标帧数（越小响应越快，建议 3~6 帧平摊 CPU 开销）")]
    [Range(1, 20)]
    [SerializeField] private int framesPerCycle = 4;

    // 内部注册表（统一管理所有实现 ICullable 的对象）
    private readonly List<ICullable> targets = new List<ICullable>();
    private int currentIndex = 0;
    private Camera cachedCamera;
    private Transform camTransform;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    private void Start()
    {
        EnsureCameraBinding();
        SweepAndRegisterSceneTargets();
        Debug.Log("$<color=#00FFCC>[UniversalCuller]</color> 通用剔除管线已启动，已纳管 {targets.Count} 个场景目标（缓冲区: X±{paddingX}, Y±{paddingY}，周期={framesPerCycle}帧）");
    }

    public static void Register(ICullable target)
    {
        if (target == null) return;
        if (Instance != null) Instance.RegisterTarget(target);
    }

    public static void Unregister(ICullable target)
    {
        if (target == null) return;
        if (Instance != null) Instance.UnregisterTarget(target);
    }

    public void RegisterTarget(ICullable target)
    {
        if (target != null && !targets.Contains(target))
        {
            targets.Add(target);
        }
    }

    public void UnregisterTarget(ICullable target)
    {
        if (target != null)
        {
            targets.Remove(target);
        }
    }

    /// <summary>
    /// 开局主动扫描场景中已有的地砖和绳索
    /// </summary>
    private void SweepAndRegisterSceneTargets()
    {
        var bridges = FindObjectsByType<FloorVisualBridge>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < bridges.Length; i++)
        {
            RegisterTarget(bridges[i]);
        }

        var ropes = FindObjectsByType<ButterflySilkRope>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < ropes.Length; i++)
        {
            RegisterTarget(ropes[i]);
        }
    }

    private void EnsureCameraBinding()
    {
        if (cachedCamera == null)
        {
            cachedCamera = Camera.main;
            if (cachedCamera != null)
                camTransform = cachedCamera.transform;
        }
    }

    private void Update()
    {
        int targetCount = targets.Count;
        if (targetCount == 0) return;

        EnsureCameraBinding();
        if (cachedCamera == null || camTransform == null) return;

        // 1. 动态计算摄像机正交视野 AABB 边界（自适应任何屏幕宽高比与视野缩放 Buff）
        Vector3 camPos = camTransform.position;
        float halfH = cachedCamera.orthographicSize + paddingY;
        float halfW = (cachedCamera.orthographicSize * cachedCamera.aspect) + paddingX;

        float minX = camPos.x - halfW;
        float maxX = camPos.x + halfW;
        float minY = camPos.y - halfH;
        float maxY = camPos.y + halfH;

        // 2. 分帧批量调度（每帧平摊一部分，维持 0 耗时波动）
        int batchSize = Mathf.CeilToInt((float)targetCount / framesPerCycle);

        for (int i = 0; i < batchSize; i++)
        {
            int idx = (currentIndex + i) % targetCount;
            var target = targets[idx];

            if (target == null || target.Equals(null))
                continue;

            Vector3[] points = target.CullCheckPoints;
            if (points == null || points.Length == 0)
                continue;

            // 🌟 多特征点快速遍历判定：只要任意一个采样点（如近端锚点、远端锚点或中点）落在屏幕+过渡区内，立即判定为可见！
            bool isInsideViewport = false;
            int pCount = points.Length;
            for (int p = 0; p < pCount; p++)
            {
                Vector3 pt = points[p];
                if (pt.x >= minX && pt.x <= maxX && pt.y >= minY && pt.y <= maxY)
                {
                    isInsideViewport = true;
                    break;
                }
            }

            target.OnCullingStateChanged(isInsideViewport);
        }

        currentIndex = (currentIndex + batchSize) % targetCount;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        targets.Clear();
    }

    private void OnDrawGizmosSelected()
    {
        Camera cam = cachedCamera != null ? cachedCamera : Camera.main;
        if (cam == null) return;

        Vector3 pos = cam.transform.position;
        float h = (cam.orthographicSize + paddingY) * 2f;
        float w = ((cam.orthographicSize * cam.aspect) + paddingX) * 2f;

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(new Vector3(pos.x, pos.y, 0f), new Vector3(w, h, 0f));
    }
}
