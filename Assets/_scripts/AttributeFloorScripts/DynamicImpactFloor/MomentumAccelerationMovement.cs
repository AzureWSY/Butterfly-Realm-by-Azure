using UnityEngine;

/// <summary>
/// 动量匀加速单向推进平台：受击/踩踏后激活，匀加速推进至终点，具备严格的【前向预测截断】杜绝过冲
/// </summary>
public class MomentumAccelerationMovement : DynamicFloorController
{
    [Header("🚀 动力学参数")]
    [Tooltip("加速推进方向（矢量）")]
    [SerializeField] private Vector2 direction = Vector2.right;

    [Tooltip("最大巡航限速 (m/s)")]
    [SerializeField] private float maxSpeed = 12f;

    [Tooltip("匀加速度 (m/s²)")]
    [SerializeField] private float acceleration = 15f;

    [Header("🛑 行程限制")]
    [Tooltip("是否在达到最大行程距离后停下？（勾选则刹停于终点，不勾选则持续飞出）")]
    [SerializeField] private bool stopAtMaxDistance = true;

    [Tooltip("最大推进距离")]
    [SerializeField] private float maxTravelDistance = 20f;

    // 运行期状态
    private bool _isActivated = false;
    private float _currentSpeed = 0f;
    private Vector2 _normalizedDir;

    protected override void Start()
    {
        base.Start();
        _normalizedDir = direction.normalized;
    }

    /// <summary>
    /// 💥 响应外部冲击/踩踏
    /// </summary>
    public override void OnImpact(Vector2 incomingVelocity)
    {
        if (_isActivated) return;
        _isActivated = true;
    }

    protected override void UpdateMovement(float fixedDeltaTime)
    {
        // 1. 未激活时不进行物理位移
        if (!_isActivated) return;

        // 2. 测量当前相对于起点的已行进投影距离
        Vector2 currentOffset = rb.position - originPosition;
        float currentDistance = Vector2.Dot(currentOffset, _normalizedDir);

        // 如果开启了行程限制，且已经在终点（或前方），直接锁定
        if (stopAtMaxDistance && currentDistance >= maxTravelDistance)
        {
            _currentSpeed = 0f;
            return;
        }

        // 3. 速度积分：更新本帧的期望速度 (V = V0 + a * dt)
        _currentSpeed = Mathf.MoveTowards(_currentSpeed, maxSpeed, acceleration * fixedDeltaTime);

        // 4. 计算本物理帧期望位移步长
        float stepDistance = _currentSpeed * fixedDeltaTime;

        // 🌟 5. 前向预测截断 (Predictive Clamp)：提前计算本步会不会冲出终点
        if (stopAtMaxDistance)
        {
            float remainingDistance = maxTravelDistance - currentDistance;

            // 如果本步位移 >= 剩余距离，说明这一帧刚好到达或超出终点
            if (stepDistance >= remainingDistance)
            {
                stepDistance = Mathf.Max(0f, remainingDistance); // 截断到刚好填满剩余距离
                _currentSpeed = 0f;                              // 到站刹停，速度归零
            }
        }

        // 6. 执行精确平移（绝对不会超过 maxTravelDistance）
        Vector2 nextPos = rb.position + _normalizedDir * stepDistance;
        rb.MovePosition(nextPos);
    }

    /// <summary>
    /// 🌟 状态与物理全面重置
    /// </summary>
    public override void ResetToOrigin()
    {
        base.ResetToOrigin();

        _isActivated = false;
        _currentSpeed = 0f;
    }

    private void OnDrawGizmosSelected()
    {
        Vector2 origin = Application.isPlaying ? originPosition : (Vector2)transform.position;
        Vector2 dir = direction.normalized;
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(origin, dir * maxTravelDistance);
        Gizmos.DrawWireSphere(origin + dir * maxTravelDistance, 0.25f);
    }
}