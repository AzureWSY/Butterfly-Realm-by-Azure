using UnityEngine;

/// <summary>
/// 蝶翼曲线缓动平台：基于 Lerp + AnimationCurve 控制的高自由度升降/浮动
/// </summary>
public class ButterflyLiftMovement : DynamicFloorController
{
    [Header("📐 位移区间配置")]
    [Tooltip("相对于出生点的目标偏移位移")]
    [SerializeField] private Vector2 targetOffset = new Vector2(0f, 3.5f);

    [Header("开始延迟")]
    [Range(0,10)]
    [SerializeField] private float StartDelay = 0.5f;

    [Header("⏱️ 周期与曲线控制")]
    [Tooltip("单程运动所需时间（秒）")]
    [SerializeField] private float duration = 2.0f;

    [Tooltip("位移插值曲线（0~1）：可自由调配加速、减速、过冲反弹等手感")]
    [SerializeField] private AnimationCurve motionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("是否往复循环（Ping-Pong）")]
    [SerializeField] private bool pingPong = true;

    private float _timer;
    private bool _movingToTarget = true;
    private bool _finishedMovement = false;

    protected override void UpdateMovement(float fixedDeltaTime)
    {
        if (duration <= 0f || _finishedMovement ) return;

        _timer += fixedDeltaTime;

        if (_timer < StartDelay) return;
  
        float normalizedTime = Mathf.Clamp01((_timer - StartDelay) / duration);

        // 评估自定义曲线权重
        float curveWeight = motionCurve.Evaluate(normalizedTime);

        Vector2 startPoint = _movingToTarget ? originPosition : originPosition + targetOffset;
        Vector2 endPoint = _movingToTarget ? originPosition + targetOffset : originPosition;

        // 使用物理 MovePosition 平滑过渡
        Vector2 nextPosition = Vector2.Lerp(startPoint, endPoint, curveWeight);
        rb.MovePosition(nextPosition);

        // 周期结束判定
        if (_timer - StartDelay >= duration)
        {
            _timer = StartDelay;
            if (pingPong)
            {
                _movingToTarget = !_movingToTarget;
            }
            else
            {
                _finishedMovement = true;
            }
        }
    }

    /// <summary>
    /// 🌟 完善后的状态重置：坐标归位 + 曲线时间归零 + 重置为初始前进方向
    /// </summary>
    public override void ResetToOrigin()
    {
        base.ResetToOrigin();

        _timer = 0f;
        _movingToTarget = true;
        _finishedMovement = false;
    }

    private void OnDrawGizmosSelected()
    {
        Vector2 origin = Application.isPlaying ? originPosition : (Vector2)transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(origin, origin + targetOffset);
        Gizmos.DrawWireSphere(origin + targetOffset, 0.2f);
    }
}