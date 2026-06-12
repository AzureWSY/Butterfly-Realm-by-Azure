using UnityEngine;

public class LaserPingPongDriver : LaserMovementDriverBase
{
    [Header("🔄 往复巡逻专有数值")]
    public float moveDistance = 5f;
    public float moveDuration = 2f;
    public float movePauseTime = 0.5f;

    private Vector3 targetPos;
    private float pingPongTimer = 0f;
    private bool movingToTarget = true;
    private float pauseTimer = 0f;
    
    protected override void Start()
    {
        base.Start();
        // 利用爸爸提供的方向向量，准确推算出编辑期摆放位置对应的终点世界坐标
        targetPos = startPos + GetMovementDirection() * moveDistance;
       
    }

    public override void TickBehavior(float deltaTime, Vector3 targetTransform)
    {
        if (delayTimer > 0f) { delayTimer -= deltaTime; return; }
        if (pauseTimer > 0f) { pauseTimer -= deltaTime; return; }

        pingPongTimer += deltaTime / moveDuration;
        Vector3 origin = movingToTarget ? startPos : targetPos;
        Vector3 destination = movingToTarget ? targetPos : startPos;

        // 纠正之前的变量命名错误，使用严谨的三阶埃尔米特多项式平滑插值
        float easedPercent = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(pingPongTimer));
        transform.position = Vector3.Lerp(origin, destination, easedPercent);

        if (pingPongTimer >= 1f)
        {
            pingPongTimer = 0f;
            movingToTarget = !movingToTarget;
            pauseTimer = movePauseTime;
        }
    }
    /// <summary>
    /// 👑 多态重写：往复巡逻物理导轨与插值归位
    /// </summary>
    public override void ResetDriverState()
    {
        // 1. 基类初始延迟完美复位
        delayTimer = initialBaseDelay;

        // 2. 彻底抹去死前的插值进度与暂停状态，让巡逻方向重新面向正前方
        pingPongTimer = 0f;
        movingToTarget = true;
        pauseTimer = 0f;

        // 3. 刚体肉身瞬间闪现回起点（这里的 startPos 是基类开局第 0 秒录入的绝对安全出厂坐标）
        transform.position = startPos;
    }
    

    private void OnDrawGizmosSelected()
    {
        Vector3 previewStart = Application.isPlaying ? startPos : transform.position;
        Vector3 previewTarget = previewStart + GetMovementDirection() * moveDistance;

        Vector2 boxSize = Vector2.one;
        if (TryGetComponent<BoxCollider2D>(out var col)) boxSize = col.size;

        Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.7f); // 亮黄色导轨中心线
        Gizmos.DrawLine(previewStart, previewTarget);

        Gizmos.color = new Color(0f, 0.7f, 1f, 0.25f); // 起点青色安全框
        Gizmos.DrawWireCube(previewStart + transform.right * (boxSize.x / 2f), new Vector3(boxSize.x, boxSize.y, 0.1f));

        Gizmos.color = new Color(1f, 0f, 0f, 0.35f); // 终点红色死亡框
        Gizmos.DrawWireCube(previewTarget + transform.right * (boxSize.x / 2f), new Vector3(boxSize.x, boxSize.y, 0.1f));
    }
}