using UnityEngine;
using System.Reflection;

/// <summary>
/// 🤖 AI 自动玩家控制器 —— 零侵入设计
/// 通过反射注入 Playercontrol 的移动输入和跳跃指令，
/// 用射线检测环境自主决策，完全不修改任何现有脚本。
/// 测试完成后移除此脚本即可恢复原状。
/// </summary>
public class AutoPlayerController : MonoBehaviour
{
    // ================== AI 配置 ==================
    [Header("🤖 AI 行为配置")]
    [Tooltip("悬崖检测射线长度")]
    [SerializeField] private float cliffRayLength = 3f;

    [Tooltip("墙壁检测射线长度")]
    [SerializeField] private float wallRayLength = 0.6f;

    [Tooltip("平台搜索射线长度")]
    [SerializeField] private float platformSearchRange = 6f;

    [Tooltip("判定为卡住的秒数")]
    [SerializeField] private float stuckTimeout = 2.5f;

    [Tooltip("跳跃冷却时间（秒）")]
    [SerializeField] private float jumpCooldownTime = 0.35f;

    // ================== 运行时统计（Inspector 可查看）==================
    [Header("📊 测试统计")]
    public int deathCount = 0;
    public float elapsedTime = 0f;
    public float maxXReached = float.MinValue;
    public float minYReached = float.MaxValue;
    public bool isFinished = false;

    // ================== 内部引用 ==================
    private Playercontrol ctrl;
    private Rigidbody2D rb;
    private BoxCollider2D box;
    private PlayerHub hub;
    private LayerMask groundMask;

    // 反射缓存：Playercontrol 的私有字段
    private FieldInfo fHorizInput;   // private float horizoninput
    private float jumpForce;         // [SerializeField] private float jumpforce

    // AI 运行状态
    private float dir = 1f;              // 移动方向：1=右，-1=左
    private float stuckTimer = 0f;       // 卡住计时器
    private Vector3 lastStuckCheckPos;   // 上次卡住检测时的位置
    private float jumpCD = 0f;           // 跳跃冷却
    private float turnCD = 0f;           // 调头冷却
    private Vector3 lastFramePos;        // 上一帧位置（用于死亡检测）
    private int consecutiveStucks = 0;   // 连续卡住次数
    private float startTime;
    private float reportTimer = 0f;      // 定时汇报计时器

    // =========================================================================
    //  初始化：缓存引用、读取参数
    // =========================================================================
    void Start()
    {
        // 获取同物体上的组件
        ctrl = GetComponent<Playercontrol>();
        rb   = GetComponent<Rigidbody2D>();
        box  = GetComponent<BoxCollider2D>();
        hub  = GetComponent<PlayerHub>();

        if (ctrl == null || rb == null || box == null)
        {
            Debug.LogError("[AutoPlayer] ❌ 缺少必要组件！请确保挂在玩家身上。");
            enabled = false;
            return;
        }

        // 直接读取 Playercontrol 的公开字段 groundLayer
        groundMask = ctrl.groundLayer;

        // 缓存反射引用：私有字段 horizoninput
        fHorizInput = typeof(Playercontrol).GetField(
            "horizoninput",
            BindingFlags.NonPublic | BindingFlags.Instance
        );

        // 读取跳跃力度（序列化私有字段，运行时值=Inspector 值）
        var jf = typeof(Playercontrol).GetField(
            "jumpforce",
            BindingFlags.NonPublic | BindingFlags.Instance
        );
        jumpForce = jf != null ? (float)jf.GetValue(ctrl) : 10f;

        // 初始化状态
        lastStuckCheckPos = transform.position;
        lastFramePos      = transform.position;
        startTime         = Time.time;
        maxXReached       = transform.position.x;
        minYReached       = transform.position.y;

        Debug.Log("<color=cyan>══════════════════════════════════════════</color>");
        Debug.Log("<color=cyan>  [AutoPlayer] 🤖 AI 自动测试已启动</color>");
        Debug.Log($"<color=cyan>  起始位置 : {transform.position}</color>");
        Debug.Log($"<color=cyan>  跳跃力度 : {jumpForce}</color>");
        // moveSpeed 是私有字段，这里不直接访问
        Debug.Log("<color=cyan>══════════════════════════════════════════</color>");
    }

    // =========================================================================
    //  核心 AI 循环 —— 在 LateUpdate 中运行
    //  时序：FixedUpdate → Update（Playercontrol 读 Input）→ LateUpdate（我们覆写）
    //  这样 FixedUpdate 始终使用我们注入的值
    // =========================================================================
    void LateUpdate()
    {
        // 如果玩家被锁住（藤蔓抓取等），暂停 AI
        if (ctrl == null || !ctrl.canMove) return;

        elapsedTime = Time.time - startTime;

        // ---------- 死亡检测 ----------
        // 原理：玩家死亡后会被 Respawn 传送，位置瞬间跳变
        float posDelta = Vector3.Distance(transform.position, lastFramePos);
        if (posDelta > 5f && elapsedTime > 0.5f)
        {
            deathCount++;
            Debug.Log($"<color=red>[AutoPlayer] 💀 死亡复活！第 {deathCount} 次 | 死亡位置: {lastFramePos}</color>");
            // 死亡后重置卡住计数
            consecutiveStucks = 0;
            stuckTimer = 0f;
        }
        lastFramePos = transform.position;

        // ---------- 更新极值 ----------
        if (transform.position.x > maxXReached) maxXReached = transform.position.x;
        if (transform.position.y < minYReached) minYReached = transform.position.y;

        // ---------- 冷却递减 ----------
        jumpCD -= Time.deltaTime;
        turnCD -= Time.deltaTime;

        // =============================================================
        //  第一阶段：射线检测环境
        // =============================================================
        Vector2 center = box.bounds.center;
        Vector2 bottom = new Vector2(center.x, box.bounds.min.y);
        float halfW = box.bounds.extents.x;
        bool onGround = ctrl.isGrounded;

        // ① 前方墙壁检测（3 根射线：上/中/下）
        bool wallAhead = CheckWall(center, dir);

        // ② 前方悬崖检测（脚前方往下射线）
        Vector2 cliffCheckOrigin = bottom + new Vector2(dir * (halfW + 0.3f), 0.1f);
        RaycastHit2D cliffHit = Physics2D.Raycast(cliffCheckOrigin, Vector2.down, cliffRayLength, groundMask);
        bool groundAhead = cliffHit.collider != null;

        // ③ 前方远处地面检测（用于判断跳过去能否落地）
        Vector2 farAheadOrigin = bottom + new Vector2(dir * 3.5f, 0.5f);
        bool groundFarAhead = Physics2D.Raycast(farAheadOrigin, Vector2.down, cliffRayLength * 2.5f, groundMask);

        // ④ 正上方平台检测
        bool platformAbove = Physics2D.Raycast(center, Vector2.up, platformSearchRange, groundMask);

        // ⑤ 斜上前方平台检测
        Vector2 diagUp = new Vector2(dir * 0.7f, 0.7f).normalized;
        bool platformDiagAbove = Physics2D.Raycast(center, diagUp, platformSearchRange, groundMask);

        // ⑥ 下方远处是否有地面（判断是否可以安全下落）
        bool groundFarBelow = Physics2D.Raycast(
            bottom + new Vector2(dir * 1f, 0f),
            Vector2.down, 15f, groundMask
        );

        // ---------- Debug 射线可视化 ----------
        Debug.DrawRay(cliffCheckOrigin, Vector2.down * cliffRayLength,
            groundAhead ? Color.green : Color.red);
        Debug.DrawRay(center, new Vector2(dir, 0) * wallRayLength,
            wallAhead ? Color.red : Color.green);
        Debug.DrawRay(farAheadOrigin, Vector2.down * cliffRayLength * 2.5f,
            groundFarAhead ? Color.blue : Color.yellow);
        Debug.DrawRay(center, Vector2.up * platformSearchRange,
            platformAbove ? Color.magenta : Color.gray);
        Debug.DrawRay(center, diagUp * platformSearchRange,
            platformDiagAbove ? Color.cyan : Color.gray);

        // =============================================================
        //  第二阶段：AI 决策
        // =============================================================
        float inputDir = dir;
        bool shouldJump = false;

        if (onGround)
        {
            if (wallAhead)
            {
                // 🧱 前方有墙 → 跳！
                shouldJump = true;
            }
            else if (!groundAhead)
            {
                // 🕳️ 前方是悬崖
                if (groundFarAhead || platformDiagAbove)
                {
                    // 远处有落点或斜上方有平台 → 起跳飞越
                    shouldJump = true;
                }
                else if (groundFarBelow)
                {
                    // 下方有地面 → 安全下落，继续前进
                    inputDir = dir;
                }
                else
                {
                    // 完全没路 → 调头
                    if (turnCD <= 0f)
                    {
                        dir *= -1f;
                        inputDir = dir;
                        turnCD = 1f;
                        Debug.Log($"<color=yellow>[AutoPlayer] 🔄 悬崖调头！新方向: {(dir > 0 ? "→右" : "←左")}</color>");
                    }
                }
            }
            else if (platformAbove || platformDiagAbove)
            {
                // ⬆️ 上方有平台 → 跳上去探索
                shouldJump = true;
            }
            // 否则：前方有路、没墙 → 正常前进
        }
        else
        {
            // 🌬️ 空中：保持前进方向（空中微操）
            inputDir = dir;
        }

        // =============================================================
        //  第三阶段：卡住检测与反制
        // =============================================================
        stuckTimer += Time.deltaTime;
        if (stuckTimer >= stuckTimeout)
        {
            float movedDist = Vector3.Distance(transform.position, lastStuckCheckPos);
            if (movedDist < 0.5f)
            {
                consecutiveStucks++;
                Debug.Log($"<color=yellow>[AutoPlayer] ⚠️ 卡住！第 {consecutiveStucks} 次 | 位置: {transform.position}</color>");

                if (consecutiveStucks <= 2)
                {
                    // 第 1-2 次：原地跳跃
                    shouldJump = true;
                }
                else
                {
                    // 第 3+ 次：调头 + 跳跃
                    dir *= -1f;
                    inputDir = dir;
                    shouldJump = true;
                    consecutiveStucks = 0;
                    Debug.Log($"<color=yellow>[AutoPlayer] 🔄 卡住调头！新方向: {(dir > 0 ? "→右" : "←左")}</color>");
                }
            }
            else
            {
                // 正常移动中，重置计数
                consecutiveStucks = 0;
            }

            lastStuckCheckPos = transform.position;
            stuckTimer = 0f;
        }

        // =============================================================
        //  第四阶段：执行动作
        // =============================================================

        // ① 通过反射注入移动方向到 Playercontrol.horizoninput
        if (fHorizInput != null)
        {
            fHorizInput.SetValue(ctrl, inputDir);
        }

        // ② 跳跃：直接设置 Rigidbody2D 的垂直速度
        if (shouldJump && jumpCD <= 0f && onGround)
        {
            rb.linearVelocityY = jumpForce;
            // 设置 isSuperJumping = true，
            // 防止 Playercontrol.Update 中的低跳重力惩罚（因为没有按住 Jump 键）
            ctrl.isSuperJumping = true;
            jumpCD = jumpCooldownTime;
        }

        // =============================================================
        //  定时汇报
        // =============================================================
        reportTimer += Time.deltaTime;
        if (reportTimer >= 10f)
        {
            reportTimer = 0f;
            Debug.Log(
                $"<color=cyan>[AutoPlayer] 📊 " +
                $"时间:{elapsedTime:F1}s | " +
                $"死亡:{deathCount} | " +
                $"位置:({transform.position.x:F1},{transform.position.y:F1}) | " +
                $"最远X:{maxXReached:F1}</color>"
            );
        }
    }

    // =========================================================================
    //  墙壁检测：从碰撞体边缘发射 3 根水平射线
    // =========================================================================
    private bool CheckWall(Vector2 center, float direction)
    {
        Vector2 rayDir = new Vector2(direction, 0);

        float topY = box.bounds.max.y - 0.1f;
        float midY = center.y;
        float botY = box.bounds.min.y + 0.1f;
        float startX = direction > 0 ? box.bounds.max.x : box.bounds.min.x;

        bool hitTop = Physics2D.Raycast(new Vector2(startX, topY), rayDir, wallRayLength, groundMask);
        bool hitMid = Physics2D.Raycast(new Vector2(startX, midY), rayDir, wallRayLength, groundMask);
        bool hitBot = Physics2D.Raycast(new Vector2(startX, botY), rayDir, wallRayLength, groundMask);

        // Debug 绘制
        Color c = Color.green;
        if (hitTop || hitMid || hitBot) c = Color.red;
        Debug.DrawRay(new Vector2(startX, topY), rayDir * wallRayLength, c);
        Debug.DrawRay(new Vector2(startX, midY), rayDir * wallRayLength, c);
        Debug.DrawRay(new Vector2(startX, botY), rayDir * wallRayLength, c);

        return hitTop || hitMid || hitBot;
    }

    // =========================================================================
    //  销毁时输出最终测试报告
    // =========================================================================
    void OnDestroy()
    {
        Debug.Log("<color=cyan>══════════════════════════════════════════</color>");
        Debug.Log("<color=cyan>  [AutoPlayer] 🤖 AI 测试最终报告</color>");
        Debug.Log($"<color=cyan>  总耗时   : {elapsedTime:F1}s</color>");
        Debug.Log($"<color=cyan>  死亡次数 : {deathCount}</color>");
        Debug.Log($"<color=cyan>  最远 X   : {maxXReached:F1}</color>");
        Debug.Log($"<color=cyan>  最低 Y   : {minYReached:F1}</color>");
        Debug.Log($"<color=cyan>  是否通关 : {isFinished}</color>");
        Debug.Log("<color=cyan>══════════════════════════════════════════</color>");
    }
}
