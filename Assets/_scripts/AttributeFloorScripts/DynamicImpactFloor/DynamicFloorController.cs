using UnityEngine;

/// <summary>
/// 动态属性地板位移基类：统一接管 Rigidbody2D 物理运动学循环
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public abstract class DynamicFloorController : MonoBehaviour,IImpactSignalRecevier
{
    protected Rigidbody2D rb;
    protected AttributeFloor floor;

    // 物体的初始坐标（世界坐标系基准点）
    protected Vector2 originPosition;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        floor = GetComponentInChildren<AttributeFloor>();

        // 平台跳跃移动平台的工业级刚体标准配置
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    protected virtual void Start()
    {
        originPosition = rb.position;
    }
    protected virtual void OnEnable()
    {
        TryRegisterToManager(); 
    }

    private void TryRegisterToManager()
    {
        if (DynamicFloorManager.Instance != null)
        {
            DynamicFloorManager.Instance.RegisterFloor(this);
        }
    }

    protected virtual void OnDisable()
    {
        // 🌟 注册表机制：物体失活/销毁时安全注销，防止内存泄漏
        if (DynamicFloorManager.Instance != null)
        {
            DynamicFloorManager.Instance.UnregisterFloor(this);
        }
    }

    private void FixedUpdate()
    {
        // 统一在物理固定帧驱动位移，彻底杜绝穿模与顿挫
        UpdateMovement(Time.fixedDeltaTime);
    }

    /// <summary>
    /// 子类具体实现的物理位移算法
    /// </summary>
    /// <param name="fixedDeltaTime">物理帧步长</param>
    protected abstract void UpdateMovement(float fixedDeltaTime);

    public virtual void OnImpact(Vector2 incomingVelocity)
    {

    }

    /// <summary>
    /// 重置地板到出生点（可供 Boss 战或房间重置调用）
    /// </summary>
    public virtual void ResetToOrigin()
    {
        rb.position = originPosition;
    }
}