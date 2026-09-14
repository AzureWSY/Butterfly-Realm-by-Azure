using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

/// <summary>
/// 样条曲线机械滑轨平台：基于 Unity Spline + Rigidbody2D 物理运动学巡航
/// </summary>
public class SplineRailMovement : DynamicFloorController
{
    [Header("🛤️ 样条轨道引用")]
    [Tooltip("场景中绑定的 SplineContainer 组件（可在同一物体或父/独立轨道物体上）")]
    [SerializeField] private SplineContainer splineContainer;

    [Header("⚙️ 机械运动参数")]
    [Tooltip("沿轨道行进的恒定线速度 (m/s)")]
    [SerializeField] private float speed = 3.5f;

    [Tooltip("到达轨道端点时的停顿等待时间（秒）")]
    [SerializeField] private float endWaitDuration = 0.5f;

    [Tooltip("运动模式：PingPong 往复折返，Loop 闭环循环")]
    [SerializeField] private SplineMode mode = SplineMode.PingPong;

    [Tooltip("是否让平台朝向沿着轨道切线自动旋转？")]
    [SerializeField] private bool alignRotationWithTrack = false;

    [Tooltip("调整启动方式")]
    [SerializeField] private ActiveMode activeMode = ActiveMode.Auto;

    public enum SplineMode { PingPong, Loop, Once }
    private enum ActiveMode { Auto , Trigger }

    // 运行期状态
    private float _currentDistance = 0f;
    private float _totalLength = 0f;
    private int _direction = 1; // 1 为正向，-1 为反向
    private float _waitTimer = 0f;
    private bool isActive = false; 

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();

        if (splineContainer != null && splineContainer.Spline != null)
        {
            _totalLength = splineContainer.Spline.GetLength();
            // 开局强制对齐到轨道起点
            SyncToSplineDistance(0f);
        }

        if(activeMode == ActiveMode.Auto)
        {
            isActive = true;
        }

    }

    public override void OnImpact(Vector2 incomingVelocity)
    {
        if (!isActive && activeMode ==  ActiveMode.Trigger) isActive = true;
    }

    protected override void UpdateMovement(float fixedDeltaTime)
    {
        if (splineContainer == null || splineContainer.Spline == null || _totalLength <= 0.001f) return;

        if (!isActive) return;

        // 端点停顿计时
        if (_waitTimer > 0f)
        {
            _waitTimer -= fixedDeltaTime;
            return;
        }

        // 依据恒定线速度推进弧长位移量 (恒速巡航，消除参数化 t 的非等速变形)
        float step = _direction * speed * fixedDeltaTime;
        _currentDistance += step;

        // 边界触达判定
        if (_currentDistance >= _totalLength)
        {
            HandleBoundaryReached(_totalLength, 1);
        }
        else if (_currentDistance <= 0f)
        {
            HandleBoundaryReached(0f, -1);
        }

        // 采样并同步物理位置
        SyncToSplineDistance(_currentDistance);
    }

    private void HandleBoundaryReached(float boundaryDist, int hitSign)
    {
        _currentDistance = boundaryDist;
        _waitTimer = endWaitDuration;

        switch (mode)
        {
            case SplineMode.PingPong:
                _direction = -hitSign;
                break;
            case SplineMode.Loop:
                _currentDistance = (hitSign > 0) ? 0f : _totalLength;
                break;
            case SplineMode.Once:
                _direction = 0;
                break;
        }
    }

    /// <summary>
    /// 依据弧长进行精确物理投影位移
    /// </summary>
    private void SyncToSplineDistance(float distance)
    {
        // 归一化进度 t ∈ [0, 1]
        float t = Mathf.Clamp01(distance / _totalLength);

        // 1. 采样局部坐标并转换至世界物理空间
        float3 localPos = splineContainer.Spline.EvaluatePosition(t);
        Vector2 worldPos = splineContainer.transform.TransformPoint(localPos);
        rb.MovePosition(worldPos);

        // 2. 切线旋转对齐（可选）
        if (alignRotationWithTrack)
        {
            float3 tangent = splineContainer.Spline.EvaluateTangent(t);
            Vector3 worldTangent = splineContainer.transform.TransformDirection(tangent);
            float angle = Mathf.Atan2(worldTangent.y, worldTangent.x) * Mathf.Rad2Deg;
            rb.MoveRotation(angle);
        }
    }

    /// <summary>
    /// 🌟 完美配合 DynamicFloorManager 的统一状态重置
    /// </summary>
    public override void ResetToOrigin()
    {
        base.ResetToOrigin();

        _currentDistance = 0f;
        _direction = 1;
        _waitTimer = 0f;
        isActive = false;

        if (splineContainer != null && splineContainer.Spline != null)
        {
            SyncToSplineDistance(0f);
        }
    }
}