using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem.Interactions;
using System.Collections;

public class Playercontrol : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float jumpforce = 20f;
    [SerializeField] private float maxfallspeed = 10f;
    [SerializeField] private float rayLength = 0.15f;   // 射线的长度（稍微比脚底长一点点即可）
    [Header("状态锁")]
    [SerializeField] private int movementLocks = 0; // 身上挂了几把锁？

    // 这是一个动态属性，只要锁的数量是 0，它就是 true（能动）
    public bool canMove => movementLocks <= 0;

    public LayerMask groundLayer;     // 依然需要标记地面的 Layer
    private Rigidbody2D rb;
    private float horizoninput;
    private BoxCollider2D boxColl;
    public bool isGrounded;
    [SerializeField] private float fallMultiplier = 4f;   // 下落时的重力倍数（数值越大，掉得越快）
    [SerializeField] private float lowJumpMultiplier = 1.5f;  // 小跳时的重力倍数（松开按键时触发）
    private float defaultGravity = 1f;    // 记录刚体初始的正常重力

    [Header("墙壁检测结果（升级为相对机制）")]
    private bool isWallAhead; // 我正前方有没有墙
    [SerializeField] private float wallRayLength = 0.15f; // 墙壁射线的长度

    [Header("预输入设置")]
    [SerializeField] private float jumpBufferTime = 0.15f; // 记住按键的时间（通常0.1到0.2秒手感最好）
    private float jumpBufferCounter;

    [Header("土狼时间设置")]
    [SerializeField] private float coyoteTime = 0.15f;
    private float coyoteTimeCounter;

    [Header("是否处于被扔状态")]
    public bool isBeingThrown = false;

    [Header("猛冲跳跃")]
    public bool canSuperJump = false;
    public bool isSuperJumping = false;
    public float SuperJumpScale;
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        boxColl = GetComponent<BoxCollider2D>();
        defaultGravity = rb.gravityScale;
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!canMove)
        {
            return;
        }

        checkisonground();
        checkistowall();
        horizoninput = Input.GetAxisRaw("Horizontal");

        // ================== 状态重置 ==================
        // 当猛跳到达最高点或开始下落时，自动解除猛冲状态
        if (isSuperJumping && rb.linearVelocityY <= 0.1f)
        {
            isSuperJumping = false;
        }

        // ================== 计时器更新 ==================
        // 土狼时间更新
        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        // 跳跃缓冲更新 (统一使用 "Jump" 监听)
        if (Input.GetButtonDown("Jump"))
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        // ================== 核心起跳判定 ==================
        // 只要缓冲池里有跳跃指令，我们就判断该执行哪种跳
        if (jumpBufferCounter > 0)
        {
            // 优先级 1：能猛跳就坚决猛跳
            if (canSuperJump)
            {
                isSuperJumping = true;
                rb.linearVelocityY = 0f;
                rb.AddForceY(SuperJumpScale, ForceMode2D.Impulse); // Unity 6 的新 API 用得漂亮！

                // 关键：清空缓冲池和土狼时间，防止本帧或下帧重复触发普通跳跃！
                jumpBufferCounter = 0;
                coyoteTimeCounter = 0;
            }
            // 优先级 2：不能猛跳，且土狼时间允许，才执行普通跳跃
            else if (coyoteTimeCounter > 0)
            {
                jump(); // 执行你原来的普通跳跃
                jumpBufferCounter = 0;
                coyoteTimeCounter = 0;
            }
        }

        // ================== 重力控制 ==================
        if (rb.linearVelocityY < 0f)
        {
            rb.gravityScale = fallMultiplier * defaultGravity;
        }
        // 【修改点】：加上 !isSuperJumping 护身符。
        // 如果正在猛跳中，就算玩家松开了空格，也不要给他施加下落重力惩罚！
        else if (rb.linearVelocityY > 0f && !Input.GetButton("Jump") && !isSuperJumping)
        {
            rb.gravityScale = lowJumpMultiplier * defaultGravity;
        }
        else
        {
            rb.gravityScale = defaultGravity;
        }
    }
    private void FixedUpdate()
    {
        if (!canMove)
        {
            return;
        }
        if (isBeingThrown) return;
        float targetVelocityX;
        targetVelocityX = horizoninput * moveSpeed;
        if (targetVelocityX != 0f && isWallAhead)
        {
            rb.linearVelocityX = 0;
        }
        rb.linearVelocityX = targetVelocityX;
        if (rb.linearVelocityY < -maxfallspeed) rb.linearVelocityY = -maxfallspeed;

    }
    private void checkisonground()
    {
        float offset = 0.1f;

        float bottomY = boxColl.bounds.min.y + offset; // 脚底板所在的 Y 坐标

        Vector2 centerPoint = new Vector2(boxColl.bounds.center.x, bottomY); // 底部中心
        Vector2 leftPoint = new Vector2(boxColl.bounds.min.x, bottomY);      // 底部左下角
        Vector2 rightPoint = new Vector2(boxColl.bounds.max.x, bottomY);     // 底部右下角

        // 第二步：发射三根向下 (Vector2.down) 的射线
        // Physics2D.Raycast 会返回一个 RaycastHit2D 结构体，如果碰到了物体，它就为 true
        RaycastHit2D hitCenter = Physics2D.Raycast(centerPoint, Vector2.down, rayLength + offset, groundLayer);
        RaycastHit2D hitLeft = Physics2D.Raycast(leftPoint, Vector2.down, rayLength + offset, groundLayer);
        RaycastHit2D hitRight = Physics2D.Raycast(rightPoint, Vector2.down, rayLength + offset, groundLayer);

        // 第三步：逻辑或 (||) 运算。只要有一根射线碰到了地面，就当做在地上
        isGrounded = hitCenter || hitLeft || hitRight;

        // ==========================================
        // 开发者福利：在 Scene 窗口中把这三根隐形的射线画出来！
        // ==========================================
        Color rayColor = isGrounded ? Color.green : Color.red; // 碰到地变绿，没碰到变红

        Debug.DrawRay(centerPoint, Vector2.down * rayLength, rayColor);
        Debug.DrawRay(leftPoint, Vector2.down * rayLength, rayColor);
        Debug.DrawRay(rightPoint, Vector2.down * rayLength, rayColor);
    }
    void jump()
    {
        rb.linearVelocityY = jumpforce;
    }
    private void checkistowall()
    {
        float offset = 0.05f;

        // 1. 依然获取世界边界
        float topY = boxColl.bounds.max.y - offset;
        float midY = boxColl.bounds.center.y;
        float bottomY = boxColl.bounds.min.y + offset;

        // 🌟 2. 【核心魔法】：利用 transform.right 自动判定“前方”在世界的左边还是右边！
        // 如果我面朝右，transform.right.x 是正数，那前方就是 max.x（右边缘）
        // 如果我面朝左，transform.right.x 是负数，那前方就是 min.x（左边缘）
        float frontX = (transform.right.x > 0) ? boxColl.bounds.max.x - offset : boxColl.bounds.min.x + offset;

        // 🌟 3. 发射射线：方向直接用 transform.right（我的正前方）
        // 只需要 3 根射线，就能同时搞定左墙和右墙的检测！
        Vector2 frontTop = new Vector2(frontX, topY);
        Vector2 frontMid = new Vector2(frontX, midY);
        Vector2 frontBot = new Vector2(frontX, bottomY);

        RaycastHit2D hitTop = Physics2D.Raycast(frontTop, transform.right, wallRayLength + offset, groundLayer);
        RaycastHit2D hitMid = Physics2D.Raycast(frontMid, transform.right, wallRayLength + offset, groundLayer);
        RaycastHit2D hitBot = Physics2D.Raycast(frontBot, transform.right, wallRayLength + offset, groundLayer);

        // 4. 只要有一根碰到，就是“前方撞墙”
        isWallAhead = hitTop || hitMid || hitBot;

        // 5. 开发者福利（Debug 绘制也直接缩减了一半代码）
        Color debugColor = isWallAhead ? Color.green : Color.red;
        Debug.DrawRay(frontTop, transform.right * wallRayLength, debugColor);
        Debug.DrawRay(frontMid, transform.right * wallRayLength, debugColor);
        Debug.DrawRay(frontBot, transform.right * wallRayLength, debugColor);
    }
    // 提供给“公司/系统”的外部强制接口
    public void SetMovementPermission(bool state)
    {
        if (state == false)
        {
            // 有人传了 false，说明他要加一把锁！
            movementLocks++;
        }
        else
        {
            
            // 有人传了 true，说明他要摘掉一把锁！
            movementLocks--;

            // 安全防线：防止有脚本抽风多传了 true，把锁扣成负数
            if (movementLocks < 0) movementLocks = 0;
        }

    }
    // 【关键修复】：专门给藤蔓抛投用的延迟解锁接口
    public void UnlockMovementWithDelay(float delay)
    {
        StartCoroutine(UnlockRoutineWithJuice(delay));
    }

    private IEnumerator UnlockRoutineWithJuice(float delay)
    {
        // --- 1. 魔法开始：启动直升机模式！ ---
        /*if (rb != null)
        {
            // 2D平台游戏通常会冻结旋转 (freezeRotation = true)，必须先临时解开，否则转不起来！
            rb.freezeRotation = false;

            // 生成一个强烈的随机旋转冲量 (扭矩)
            // Adjust the range for faster/slower spin. Positive is counter-clockwise, Negative is clockwise.
            float spinForce = Random.Range(-15f, 15f);

            // 使用 Impulse 模式施加一次性扭矩，让他飞出去瞬间疯狂旋转
            rb.AddTorque(spinForce, ForceMode2D.Impulse);
        }*/

        // --- 2. 硬直时间：在半空中转一会儿 ---
        yield return new WaitForSeconds(delay);

        // --- 3. 恢复控制前夕：准备落地，优雅重置 ---
        if (rb != null)
        {
            // 关键：必须重新冻结旋转，防止落地后玩家像个陀螺一样滚来滚去
            rb.freezeRotation = true;

            // ??视觉修正：瞬间或者用协程把角度掰正 (0度)。
            // 为了让逻辑干净，这里瞬间掰正。这样canMove恢复时，人是立直的。
            rb.rotation = 0f;

            // 如果想更优雅，可以使用 LeanTween 或 Coroutine 在一段时间内 Lerp 回 0 度。
        }

        // 解锁移动控制锁
        movementLocks--;
        if (movementLocks < 0) movementLocks = 0;

        Debug.Log("抛投硬直结束，玩家恢复直立并恢复控制");
    }
}
   

