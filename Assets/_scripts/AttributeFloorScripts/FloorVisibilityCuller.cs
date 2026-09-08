using UnityEngine;

/// <summary>
/// 🔦 地板视觉效果视距剔除器（中央分批扫描）
/// 每帧只扫描一小批 FloorVisualBridge，
/// 超出摄像机距离的自动关灯停粒子，进入范围自动恢复。
/// 挂在场景任意常驻物体上即可（建议挂在 DynamicFloorManager 同物体）。
/// </summary>
public class FloorVisibilityCuller : MonoBehaviour
{
    public static FloorVisibilityCuller Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic() => Instance = null;

    [Header("⚙️ 剔除配置")]
    [Tooltip("超过此距离的地板将关闭灯光和粒子（单位：世界坐标）")]
    [SerializeField] private float cullDistance = 25f;

    [Tooltip("完整扫描一轮的帧数（越大越省 CPU，但响应越慢）")]
    [SerializeField] private int framesPerCycle = 40;

    // 内部缓存
    private FloorVisualBridge[] allBridges;
    private int currentIndex = 0;
    private Transform camTransform;
    private float sqrCullDistance;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    private void Start()
    {
        // 收集场景中所有 FloorVisualBridge
        allBridges = FindObjectsByType<FloorVisualBridge>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        // 缓存主摄像机
        if (Camera.main != null)
            camTransform = Camera.main.transform;

        sqrCullDistance = cullDistance * cullDistance;

        Debug.Log($"<color=#00FFCC>[VisibilityCuller]</color> 已接管 {allBridges.Length} 个地板的视距剔除（距离={cullDistance}，周期={framesPerCycle}帧）");
    }

    private void Update()
    {
        if (allBridges == null || allBridges.Length == 0 || camTransform == null)
            return;

        // 每帧处理的批量大小
        int batchSize = Mathf.CeilToInt((float)allBridges.Length / framesPerCycle);
        Vector3 camPos = camTransform.position;

        for (int i = 0; i < batchSize; i++)
        {
            int idx = (currentIndex + i) % allBridges.Length;
            var bridge = allBridges[idx];
            if (bridge == null) continue;

            // 使用平方距离比较，避免 sqrt 开销
            float sqrDist = (bridge.transform.position - camPos).sqrMagnitude;
            bridge.SetCulled(sqrDist > sqrCullDistance);
        }

        currentIndex = (currentIndex + batchSize) % allBridges.Length;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// 运行时修改剔除距离（供 Inspector 或调试用）
    /// </summary>
    public void SetCullDistance(float newDistance)
    {
        cullDistance = newDistance;
        sqrCullDistance = cullDistance * cullDistance;
    }
}
