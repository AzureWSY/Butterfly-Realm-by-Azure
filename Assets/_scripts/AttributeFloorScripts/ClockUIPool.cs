using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 🕐 时钟 UI 对象池
/// 运行时从场景中现有的 ClockUI 复制模板，创建可复用的小型池。
/// 644 个独立 Canvas → 池中仅保留 N 个（默认 5），按需借还。
/// 挂在场景任意常驻物体上即可（建议挂在 DynamicFloorManager 同物体）。
/// </summary>
public class ClockUIPool : MonoBehaviour
{
    public static ClockUIPool Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic() => Instance = null;

    [Header("⚙️ 池配置")]
    [Tooltip("预创建的时钟 UI 数量（同一时刻最多显示这么多个冷却时钟）")]
    [SerializeField] private int poolSize = 5;

    [SerializeField] private GameObject ClockUI;

    /// <summary>
    /// 借出的时钟句柄，持有者通过它更新进度和归还
    /// </summary>
    public class ClockHandle
    {
        public GameObject root;
        public Image fillImage;   // "Clock Passed" — 进度填充
        public Image restImage;   // "Clock Rest"   — 底盘背景
        internal bool inUse;

        public void Show(Vector3 worldPos)
        {
            root.transform.position = worldPos;
            root.SetActive(true);
            if (fillImage != null)
            {
                fillImage.gameObject.SetActive(true);
                fillImage.fillAmount = 0f;
            }
            if (restImage != null) restImage.gameObject.SetActive(true);
        }

        public void Hide()
        {
            root.SetActive(false);
        }
    }

    private readonly List<ClockHandle> pool = new List<ClockHandle>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;

        InitializePool();
    }

    private void InitializePool()
    {

        // ② 用模板克隆出 poolSize 个池对象
        for (int i = 0; i < poolSize; i++)
        {
            var clone = Instantiate(ClockUI, transform);
            clone.name = $"PooledClock_{i}";
            clone.SetActive(false);

            var handle = new ClockHandle { root = clone };

            // 按名称匹配子物体（和预制体中的命名一致）
            var restChild = clone.transform.Find("Clock Rest");
            var passedChild = clone.transform.Find("Clock Passed");

            if (restChild != null) handle.restImage = restChild.GetComponent<Image>();
            if (passedChild != null) handle.fillImage = passedChild.GetComponent<Image>();

            pool.Add(handle);
            
        }
        Debug.Log($"抓取到{pool.Count}个");

    }

    /// <summary>
    /// 借出一个时钟 UI，定位到指定世界坐标
    /// </summary>
    public ClockHandle Rent(Vector3 worldPos)
    {
        // 优先复用空闲项
        foreach (var item in pool)
        {
            if (!item.inUse)
            {
                item.inUse = true;
                item.Show(worldPos);
                return item;
            }
        }

        // 池耗尽 → 自动扩容（基于首个池对象克隆）
        if (pool.Count > 0 && pool[0].root != null)
        {
            Debug.LogWarning($"[ClockUIPool] 池已耗尽（{pool.Count}个），自动扩容...");

            var clone = Instantiate(pool[0].root, transform);
            clone.name = $"PooledClock_{pool.Count}";

            var handle = new ClockHandle { root = clone };
            var restChild = clone.transform.Find("Clock Rest");
            var passedChild = clone.transform.Find("Clock Passed");
            if (restChild != null) handle.restImage = restChild.GetComponent<Image>();
            if (passedChild != null) handle.fillImage = passedChild.GetComponent<Image>();

            handle.inUse = true;
            handle.Show(worldPos);
            pool.Add(handle);
            return handle;
        }

        Debug.LogError("[ClockUIPool] 无法创建新的时钟 UI！");
        return null;
    }

    /// <summary>
    /// 归还一个时钟 UI 到池中
    /// </summary>
    public void Return(ClockHandle handle)
    {
        if (handle == null) return;
        handle.inUse = false;
        handle.Hide();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
