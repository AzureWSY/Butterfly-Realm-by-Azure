using System.Collections.Generic;
using UnityEngine;

public class PlayerPickupController : MonoBehaviour
{
    [Header("🚀 搜刮与持物配置")]
    [SerializeField] private float scanRadius = 4.0f;       // 圆形扫描半径
    [SerializeField] private LayerMask itemLayer;           // 可拾取物图层
    [SerializeField] private Transform auraHoldPoint;       // 光环挂载锚点

    private PickupHalo _halo;
    private bool _isDetectionModeActive = false;            // O键检测模式状态
    private IPickableThrowable _heldObject = null;          // 当前持有的唯一物体

    private readonly List<IPickableThrowable> _highlightedCache = new List<IPickableThrowable>();

    // 🌟 Unity 6 顶级性能核心：零堆分配常驻缓冲区
    private readonly Collider2D[] _radarBuffer = new Collider2D[16];

    // 🌟 Unity 6 标配：物理安检过滤器（在Awake初始化，杜绝每帧new产生GC垃圾）
    private ContactFilter2D _contactFilter;

    private void Awake()
    {
        _halo = GetComponentInChildren<PickupHalo>();

        // 🌟 完美对齐 Unity 6 规范：初始化常驻物理图层过滤器
        _contactFilter = new ContactFilter2D();
        _contactFilter.SetLayerMask(itemLayer);
        _contactFilter.useLayerMask = true;

        if (_halo == null)
        {
            Debug.LogError($"【PlayerPickupController】致命错误：未在子层级找到挂载 PickupHalo 的物件！");
        }
    }

    private void Update()
    {
        // 1. 独立按键：O键开关检测模式
        if (Input.GetKeyDown(KeyCode.O))
        {
            _isDetectionModeActive = !_isDetectionModeActive;
            if (!_isDetectionModeActive) ClearAllHighlights();

            if (_halo != null) _halo.FadeHalo(_isDetectionModeActive);
        }

        // 2. 检测模式开启时的独立流转
        if (_isDetectionModeActive)
        {
            PerformRadarScanAndHighlight();
            HandleMouseClickPickup();
        }

        // 3. 独立按键：右键往鼠标方向抛掷
        if (_heldObject != null && Input.GetMouseButtonDown(1))
        {
            ThrowObject();
        }
    }

    private void PerformRadarScanAndHighlight()
    {
        ClearAllHighlights();

        // 🌟 终极修复：使用 Unity 6 标准方法名重载，剥离 NonAlloc 尾缀！
        // 直接向底层注入 过滤器(_contactFilter) 与 缓冲区(_radarBuffer)，依然保持纯代码零分配高吞吐运行！
        int hitCount = Physics2D.OverlapCircle(
            transform.position,
            scanRadius,
            _contactFilter,
            _radarBuffer
        );

        for (int i = 0; i < hitCount; i++)
        {
            if (_radarBuffer[i].TryGetComponent<IPickableThrowable>(out var item))
            {
                if (!item.IsPickedUp)
                {
                    item.ToggleHighlight(true); // 亮起白色高亮
                    _highlightedCache.Add(item);
                }
            }
        }
    }

    private void HandleMouseClickPickup()
    {
        if (Input.GetMouseButtonDown(0)) // 鼠标左键点击点选
        {
            Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mouseWorldPos, Vector2.zero, 0f, itemLayer);

            if (hit.collider != null && hit.collider.TryGetComponent<IPickableThrowable>(out var item))
            {
                float sqrDist = ((Vector2)transform.position - (Vector2)hit.collider.transform.position).sqrMagnitude;

                // 限制红线：必须在半径内、且手里是空的，同时该物体未被别人捡走
                if (sqrDist <= scanRadius * scanRadius && _heldObject == null && !item.IsPickedUp)
                {
                    _heldObject = item;
                    item.OnPickedUp(auraHoldPoint); // 收入光环

                    _isDetectionModeActive = false; // 成功拾取，自动关闭检测模式
                    ClearAllHighlights();

                    if (_halo != null) _halo.FadeHalo(false);
                }
            }
        }
    }

    private void ThrowObject()
    {
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 throwDir = ((Vector2)(mouseWorldPos - auraHoldPoint.position)).normalized;

        var targetThrow = _heldObject;
        _heldObject = null; // 手里腾空
        targetThrow.OnThrown(throwDir * 14f); // 给予冲量抛出
    }

    private void ClearAllHighlights()
    {
        foreach (var item in _highlightedCache)
        {
            if (item != null) item.ToggleHighlight(false);
        }
        _highlightedCache.Clear();
    }

    private void OnDrawGizmosSelected()
    {
        if (_isDetectionModeActive)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(transform.position, scanRadius);
        }
    }
}