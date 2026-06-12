using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(CircleCollider2D))]
public class BlueVineTrap_Ultimate_MultiCollision : MonoBehaviour
{
    public enum VineType { StaticGrabber, SweepingGrabber, TrackingPiercer }

    [Header("⚔️ 陷阱形态设置")]
    public VineType currentVineType = VineType.StaticGrabber;

    private enum VineState { Idle, Alert, Attacking, Retracting }
    private VineState currentState = VineState.Idle;

    [Header("🔩 共有物理配置")]
    public Transform vineRoot;
    public int segmentCount = 35;
    public float attackSpeed = 18f;
    public int damageAmount = 20;

    [Header("👁️ 【核心】视野与侦测配置")]
    [Tooltip("藤蔓的最大感知半径（距离限制）")]
    public float aggroRadius = 8f;
    [Tooltip("光锥检测的范围（半角）。如果Unity里Light外角是90度，这里填45！")]
    public float detectionAngle = 45f;
    [Tooltip("藤蔓默认的朝向。往下垂是(0,-1)")]
    public Vector2 baseDirection = Vector2.down;

    [Header("⏱️ 节奏与预警配置")]
    public float startDelay = 0.5f;
    public bool flickerWarning = true;
    public float manualPhaseOffset = 0f;

    [Header("🪤 形态1&2：抛投专属配置")]
    public float holdTime = 1.5f;
    public Transform specificThrowPoint;
    public float throwMoveSpeed = 25f;
    public float wrapRadius = 0.8f;
    public float swaySpeed = 2.5f;
    public float swayMagnitude = 1.2f;

    [Header("📷 形态2：探照灯扫描专属配置")]
    [Tooltip("探照灯左右摇摆的最大角度幅度")]
    public float sweepAngle = 45f;
    public float sweepSpeed = 2f;

    [Header("🗡️ 形态3：穿刺专属配置")]
    public float pierceWidthRoot = 0.6f;
    public float pierceWidthTip = 0.1f;
    public float pierceRetractSpeed = 8f;
    private Vector2 lockedTargetPos;

    private float originalStartWidth;
    private float originalEndWidth;

    [Header("💡 表现层引用")]
    public Light2D vineLight;
    private Color originalLightColor;

    private LineRenderer line;
    private CircleCollider2D tipCollider;

    // =========================================================================
    // 🌟 终极轻量化：干掉零散的4个老旧引用，一揽子聚合持有中介者，大门一键全开
    // =========================================================================
    private PlayerHub playerHub;
    private Collider2D[] playerColliders; // 留存用于多重叠盒判定

    private void Start()
    {
        line = GetComponent<LineRenderer>();
        tipCollider = GetComponent<CircleCollider2D>();
        tipCollider.isTrigger = true;
        tipCollider.radius = 0.3f;

        originalStartWidth = line.startWidth;
        originalEndWidth = line.endWidth;

        if (vineLight != null)
        {
            originalLightColor = vineLight.color;
        }

        // 🌟 批量动态绑定：Unity 6 现代无序极速查询，开局直接握住完全体枢纽名片
        playerHub = FindAnyObjectByType<PlayerHub>();

        if (playerHub != null)
        {
            // 通过枢纽一键调出多重碰撞组合阵列
            playerColliders = playerHub.GetComponentsInChildren<Collider2D>();
            Debug.Log($"<color=green>【藤蔓陷阱】已成功通过中介者 [PlayerHub] 对齐最高安全物理参考系！</color>");
        }
        else
        {
            Debug.LogWarning($"<color=yellow>【藤蔓陷阱】防呆系统：未在场景中发现玩家 Hub，藤蔓将挂起挂机。</color>");
        }
    }

    private void Update()
    {
        // 如果主角还没诞生，或者切关卡时旧代理被销毁，优雅挂机挂机装死，拒绝报错
        if (playerHub == null || playerHub.Collider == null) return;

        switch (currentState)
        {
            case VineState.Idle:
                HandleIdleState();
                break;
            case VineState.Alert:
                if (currentVineType == VineType.TrackingPiercer)
                    StartCoroutine(PierceAttackRoutine());
                else
                    StartCoroutine(GrabAttackRoutine());
                break;
            case VineState.Attacking:
            case VineState.Retracting:
                break;
        }
    }

    private void AimLightAtTarget(Vector2 targetPos)
    {
        if (vineLight != null)
        {
            Vector2 dir = targetPos - (Vector2)vineRoot.position;
            if (dir.sqrMagnitude > 0.001f)
            {
                float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
                vineLight.transform.rotation = Quaternion.Euler(0, 0, targetAngle);
            }
        }
    }

    private void HandleIdleState()
    {
        Vector2 tipPos;
        Vector2 currentLookDir = baseDirection.normalized;

        if (currentVineType == VineType.TrackingPiercer)
        {
            tipPos = (Vector2)vineRoot.position + currentLookDir * 0.5f;
            DrawPiercingVine(tipPos);
            AimLightAtTarget(tipPos);
        }
        else if (currentVineType == VineType.SweepingGrabber)
        {
            float angleOffset = Mathf.Sin((Time.time + manualPhaseOffset) * sweepSpeed) * sweepAngle;
            currentLookDir = Quaternion.Euler(0, 0, angleOffset) * baseDirection.normalized;

            tipPos = (Vector2)vineRoot.position + currentLookDir * 1.5f;
            DrawSwayingVine(tipPos, currentLookDir);
            AimLightAtTarget(tipPos);
        }
        else // StaticGrabber
        {
            tipPos = (Vector2)vineRoot.position + currentLookDir * 1.5f;
            DrawSwayingVine(tipPos, currentLookDir);
            AimLightAtTarget(tipPos);
        }

        // 🌟 修正：侦测计算全面平替为极其清白、健康的【体积空间几何中心】
        Vector2 playerCenter = playerHub.Collider.bounds.center;

        float distToPlayer = Vector2.Distance(vineRoot.position, playerCenter);
        if (distToPlayer <= aggroRadius)
        {
            Vector2 dirToPlayer = (playerCenter - (Vector2)vineRoot.position).normalized;

            if (Vector2.Angle(currentLookDir, dirToPlayer) <= detectionAngle)
            {
                currentState = VineState.Alert;
            }
        }
    }

    private IEnumerator PierceAttackRoutine()
    {
        currentState = VineState.Attacking;
        float originalIntensity = vineLight != null ? vineLight.intensity : 1f;
        float timer = 0f;

        while (timer < startDelay)
        {
            timer += Time.deltaTime;
            if (vineLight != null)
            {
                vineLight.color = Color.red;
                if (flickerWarning) vineLight.intensity = Mathf.PingPong(Time.time * 20f, 2f) + originalIntensity;
            }

            // 🌟 修正：预警蓄力期间，光锥死死聚焦锁定角色的【体积重心】
            Vector2 currentCenter = playerHub.Collider.bounds.center;
            Vector2 aimDir = (currentCenter - (Vector2)vineRoot.position).normalized;
            AimLightAtTarget((Vector2)vineRoot.position + aimDir * 1.5f);
            yield return null;
        }

        if (vineLight != null) vineLight.intensity = originalIntensity + 1f;

        // 🌟 修正：在突刺出击瞬间，锁定角色当下的绝对物理中心，并向前额外突刺1码防蹭边逃脱
        Vector2 finalCenter = playerHub.Collider.bounds.center;
        Vector2 dir = (finalCenter - (Vector2)vineRoot.position).normalized;
        lockedTargetPos = finalCenter + dir * 1f;

        Vector2 tipPos = vineRoot.position;
        bool hasDamaged = false;

        while (Vector2.Distance(tipPos, lockedTargetPos) > 0.1f)
        {
            tipPos = Vector2.MoveTowards(tipPos, lockedTargetPos, attackSpeed * 2f * Time.deltaTime);
            DrawPiercingVine(tipPos);
            UpdateColliderPosition(tipPos);
            AimLightAtTarget(tipPos);

            // 🌟 修正：刺杀判定基于重心检测
            if (!hasDamaged && tipCollider.OverlapPoint(playerHub.Collider.bounds.center))
            {
                if (playerHub.Health != null) playerHub.Health.TakeDamage(damageAmount);
                hasDamaged = true;
            }
            yield return null;
        }

        yield return new WaitForSeconds(0.2f);

        currentState = VineState.Retracting;
        while (Vector2.Distance(tipPos, vineRoot.position) > 0.1f)
        {
            tipPos = Vector2.MoveTowards(tipPos, vineRoot.position, pierceRetractSpeed * Time.deltaTime);
            DrawPiercingVine(tipPos);
            UpdateColliderPosition(tipPos);
            AimLightAtTarget(tipPos);
            yield return null;
        }

        if (vineLight != null) { vineLight.color = originalLightColor; vineLight.intensity = originalIntensity; }
        currentState = VineState.Idle;
    }

    private IEnumerator GrabAttackRoutine()
    {
        currentState = VineState.Attacking;
        float originalIntensity = vineLight != null ? vineLight.intensity : 1f;
        float delayTimer = 0f;

        while (delayTimer < startDelay)
        {
            delayTimer += Time.deltaTime;
            if (vineLight != null)
            {
                vineLight.color = Color.red;
                if (flickerWarning) vineLight.intensity = Mathf.PingPong(Time.time * 20f, 2f) + originalIntensity;
            }

            Vector2 currentCenter = playerHub.Collider.bounds.center;
            Vector2 aimDir = (currentCenter - (Vector2)vineRoot.position).normalized;
            AimLightAtTarget((Vector2)vineRoot.position + aimDir * 1.5f);
            yield return null;
        }

        if (vineLight != null) vineLight.intensity = originalIntensity + 1f;

        Vector2 tipPos = vineRoot.position;

        // 捕食冲刺：藤蔓尖端疯狂咬向玩家的物理中心
        while (Vector2.Distance(tipPos, playerHub.Collider.bounds.center) > 0.5f)
        {
            tipPos = Vector2.MoveTowards(tipPos, playerHub.Collider.bounds.center, attackSpeed * Time.deltaTime);
            DrawAttackingVine(tipPos);
            UpdateColliderPosition(tipPos);
            AimLightAtTarget(tipPos);
            if (tipCollider.OverlapPoint(playerHub.Collider.bounds.center)) break;
            yield return null;
        }

        // =========================================================================
        // 🌟 核心修正：捕获动作发生！通过 Hub 一揽子调动子零件资源，执行物理囚禁
        // =========================================================================
        if (playerHub.Control != null && playerHub.Control.canMove)
        {
            // 剥夺克隆体平移按键权限
            playerHub.Control.SetMovementPermission(false);

            // 斩断刚体一切外在动能冲量，重力归零，使其无法挣脱悬空
            if (playerHub.Rb != null)
            {
                playerHub.Rb.linearVelocity = Vector2.zero;
                playerHub.Rb.gravityScale = 0f;
            }

            // 伤害灌入
            if (playerHub.Health != null) playerHub.Health.TakeDamage(damageAmount);

            // ─── 阶段二：缠绕绞杀演出 ───
            float holdTimer = 0;
            while (holdTimer < holdTime)
            {
                // 强制束缚：强行平移主角根节点 Transform 坐标死死钉在藤蔓尖端
                playerHub.transform.position = tipPos;
                DrawWrappingVine(tipPos, holdTimer / holdTime);
                AimLightAtTarget(tipPos);
                holdTimer += Time.deltaTime;
                yield return null;
            }

            // ─── 阶段三：工业级安全抛投（The Safe Throw Out Route） ───
            if (playerHub.Rb != null && specificThrowPoint != null)
            {
                playerHub.Control.isBeingThrown = true;
                playerHub.Rb.freezeRotation = true;
                playerHub.Rb.rotation = 0f;

                // 抛投平移期间强行关闭玩家自身的碰撞，防止在中途卡进厚重墙体瓦片产生物理穿透
                if (playerColliders != null) foreach (Collider2D col in playerColliders) col.enabled = false;

                Vector2 currentPos = tipPos;
                while (Vector2.Distance(currentPos, specificThrowPoint.position) > 0.3f)
                {
                    currentPos = Vector2.MoveTowards(currentPos, specificThrowPoint.position, throwMoveSpeed * Time.deltaTime);
                    playerHub.Rb.MovePosition(currentPos); // 严谨使用 MovePosition 刷入刚体连续运动轨迹
                    DrawAttackingVine(currentPos);
                    AimLightAtTarget(currentPos);
                    yield return null;
                }

                // 投掷落地前夕，安全解封碰撞，还回重力系统
                if (playerColliders != null) foreach (Collider2D col in playerColliders) col.enabled = true;
                playerHub.Control.isBeingThrown = false;
                playerHub.Rb.gravityScale = 1f;

                // 赋予受害玩家一个抛投末端强大的离心力物理冲击（Impulse）
                Vector2 forwardBoost = ((Vector2)specificThrowPoint.position - currentPos).normalized;
                playerHub.Rb.AddForce(forwardBoost * 8f, ForceMode2D.Impulse);
            }
            else
            {
                if (playerHub.Rb != null) playerHub.Rb.gravityScale = 1f;
            }

            // 归还操作大权
            playerHub.Control.SetMovementPermission(true);
        }

        // ─── 阶段四：物理功成身退，残影缩回 ───
        currentState = VineState.Retracting;
        while (Vector2.Distance(tipPos, vineRoot.position) > 0.2f)
        {
            tipPos = Vector2.MoveTowards(tipPos, vineRoot.position, attackSpeed * 0.8f * Time.deltaTime);
            DrawAttackingVine(tipPos);
            UpdateColliderPosition(tipPos);
            AimLightAtTarget(tipPos);
            yield return null;
        }

        if (vineLight != null) { vineLight.color = originalLightColor; vineLight.intensity = originalIntensity; }
        currentState = VineState.Idle;
    }

    private void UpdateColliderPosition(Vector2 tipPos) { tipCollider.offset = transform.InverseTransformPoint(tipPos); }

    private void DrawSwayingVine(Vector2 tipPos, Vector2 lookDir)
    {
        line.positionCount = segmentCount;
        line.startWidth = originalStartWidth;
        line.endWidth = originalEndWidth;

        for (int i = 0; i < segmentCount; i++)
        {
            float t = i / (float)(segmentCount - 1);
            Vector2 point = Vector2.Lerp(vineRoot.position, tipPos, t);

            Vector2 normal = new Vector2(-lookDir.y, lookDir.x);
            float sway = Mathf.Sin((Time.time + manualPhaseOffset) * swaySpeed + i * 0.5f) * swayMagnitude * t;
            point += normal * sway;
            line.SetPosition(i, point);
        }
    }

    private void DrawAttackingVine(Vector2 tipPos)
    {
        line.positionCount = segmentCount;
        line.startWidth = originalStartWidth;
        line.endWidth = originalEndWidth;

        for (int i = 0; i < segmentCount; i++)
        {
            float t = i / (float)(segmentCount - 1);
            Vector2 point = Vector2.Lerp(vineRoot.position, tipPos, t);

            float sway = Mathf.Sin((Time.time + manualPhaseOffset) * swaySpeed * 3f + i) * swayMagnitude * 0.5f * t;
            Vector2 dir = (tipPos - (Vector2)vineRoot.position).normalized;
            Vector2 normal = new Vector2(-dir.y, dir.x);
            point += normal * sway;
            line.SetPosition(i, point);
        }
    }

    private void DrawWrappingVine(Vector2 center, float progress)
    {
        line.positionCount = segmentCount;
        line.startWidth = originalStartWidth;
        line.endWidth = originalEndWidth;
        for (int i = 0; i < segmentCount; i++)
        {
            float t = i / (float)(segmentCount - 1);
            if (t < 0.5f) { line.SetPosition(i, Vector2.Lerp(vineRoot.position, center, t * 2f)); }
            else
            {
                float angle = t * Mathf.PI * 4f;
                float curRad = Mathf.Lerp(wrapRadius, wrapRadius * 0.3f, progress);
                Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * curRad;
                line.SetPosition(i, center + offset);
            }
        }
    }

    private void DrawPiercingVine(Vector2 tipPos)
    {
        line.positionCount = 2;
        line.startWidth = pierceWidthRoot;
        line.endWidth = pierceWidthTip;
        line.SetPosition(0, vineRoot.position);
        line.SetPosition(1, tipPos);
    }

    // =========================================================================
    // 🎬 进阶多态：大导演剧情演出接口（同步洗净偏置，升级为直接接收 PlayerHub 句柄）
    // =========================================================================
    public IEnumerator ExecuteDirectorGrab(PlayerHub targetHub, float pullTime)
    {
        if (targetHub == null) yield break;

        currentState = VineState.Attacking;
        if (vineLight != null) { vineLight.color = Color.red; vineLight.intensity = 3f; }

        Vector2 tipPos = vineRoot.position;

        // 2. 像毒蛇一样射向玩家物理中心
        while (Vector2.Distance(tipPos, targetHub.Collider.bounds.center) > 0.5f)
        {
            tipPos = Vector2.MoveTowards(tipPos, targetHub.Collider.bounds.center, attackSpeed * 2f * Time.deltaTime);
            DrawAttackingVine(tipPos);
            AimLightAtTarget(tipPos);
            yield return null;
        }

        // 3. 缠住玩家，并强行拖回 Boss 面前！
        float timer = 0f;
        Vector3 playerStartPos = targetHub.transform.position; // 记录死前根部起始点
        while (timer < pullTime)
        {
            timer += Time.deltaTime;
            float t = timer / pullTime;

            // 完美的单向支配：强行位移主角大根物体坐标
            targetHub.transform.position = Vector3.Lerp(playerStartPos, vineRoot.position, t);
            tipPos = targetHub.transform.position;

            DrawWrappingVine(tipPos, t);
            AimLightAtTarget(tipPos);
            yield return null;
        }

        // 4. 演出结束，缩回并隐藏
        currentState = VineState.Retracting;
        while (Vector2.Distance(tipPos, vineRoot.position) > 0.2f)
        {
            tipPos = Vector2.MoveTowards(tipPos, vineRoot.position, attackSpeed * Time.deltaTime);
            DrawAttackingVine(tipPos);
            UpdateColliderPosition(tipPos);
            AimLightAtTarget(tipPos);
            yield return null;
        }

        if (vineLight != null) { vineLight.color = originalLightColor; vineLight.intensity = 1f; }
        line.positionCount = 0;
        currentState = VineState.Idle;
    }
}