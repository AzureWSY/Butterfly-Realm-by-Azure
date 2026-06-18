using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(CircleCollider2D))]
public class BlueVineTrap_Ultimate_MultiCollision : MonoBehaviour
{
    public enum VineType { StaticGrabber, SweepingGrabber, TrackingPiercer, InteractiveGrabber }

    [Header("⚔️ 陷阱形态设置")]
    public VineType currentVineType = VineType.StaticGrabber;

    private enum VineState { Idle, Alert, Attacking, Struggling, Retracting }
    private VineState currentState = VineState.Idle;

    [Header("🔩 共有物理配置")]
    [Tooltip("藤蔓的物理根节点。若漏填，在游戏开始时会自动保底指向当前物体自身。")]
    public Transform vineRoot; // 🔥 修正：恢复为清白、健康的面板公开序列化引脚
    public int segmentCount = 35;
    public float attackSpeed = 18f;
    public int damageAmount = 20;

    [Header("👁️ 【核心】视野与侦测配置")]
    public float aggroRadius = 8f;
    public float detectionAngle = 45f;
    public Vector2 baseDirection = Vector2.down;

    [Header("⏱️ 节奏与预警配置")]
    public float startDelay = 0.5f;
    public bool flickerWarning = true;
    public float manualPhaseOffset = 0f;

    [Header("🪤 形态1&2：常规抛投配置")]
    public float holdTime = 1.5f;
    public Transform specificThrowPoint;
    public float throwMoveSpeed = 25f;
    public float wrapRadius = 0.8f;
    public float swaySpeed = 2.5f;
    public float swayMagnitude = 1.2f;

    [Header("📷 形态2：探照灯扫描专属配置")]
    public float sweepAngle = 45f;
    public float sweepSpeed = 2f;

    [Header("🗡️ 形态3：穿刺专属配置")]
    public float pierceWidthRoot = 0.6f;
    public float pierceWidthTip = 0.1f;
    public float pierceRetractSpeed = 8f;
    private Vector2 lockedTargetPos;

    [Header("🕹️ 形态4：互动操纵型专属配置")]
    public float struggleWindowTime = 3.5f;
    [Tooltip("推荐设为90度，这样按D到底能完全转平指向正右方，方向区别极其明显！")]
    public float maxSteerAngle = 90f;
    public float steerSpeed = 60f;
    public float requiredEscapeEnergy = 100f;
    public float energyPerTap = 12f;
    public float energyDecaySpeed = 15f;
    public float spitForce = 25f;

    [Header("📊 UI与可视化引用")]
    public Slider escapeProgressBar;

    private float currentSteerAngle = 0f;
    private float currentEscapeEnergy = 0f;

    private float originalStartWidth;
    private float originalEndWidth;

    [Header("💡 表现层引用")]
    public Light2D vineLight;
    private Color originalLightColor;

    private LineRenderer line;
    private CircleCollider2D tipCollider;

    private PlayerHub playerHub;
    private Collider2D[] playerColliders;

    private void Start()
    {
        line = GetComponent<LineRenderer>();
        tipCollider = GetComponent<CircleCollider2D>();
        tipCollider.isTrigger = true;
        tipCollider.radius = 0.3f;

        originalStartWidth = line.startWidth;
        originalEndWidth = line.endWidth;

        if (vineLight != null) originalLightColor = vineLight.color;

        // 🔥 核心修正：彻底废除沉重的 transform.Find 字符串低效查找，升级为绝对安全的零分配保底设计
        if (vineRoot == null)
        {
            vineRoot = this.transform;
        }

        playerHub = FindAnyObjectByType<PlayerHub>();
        if (playerHub != null)
        {
            playerColliders = playerHub.GetComponentsInChildren<Collider2D>();
        }

        if (escapeProgressBar != null) escapeProgressBar.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (playerHub == null || playerHub.Collider == null) return;

        switch (currentState)
        {
            case VineState.Idle:
                HandleIdleState();
                break;
            case VineState.Alert:
                if (currentVineType == VineType.InteractiveGrabber)
                    StartCoroutine(InteractiveGrabAttackRoutine());
                else if (currentVineType == VineType.TrackingPiercer)
                    StartCoroutine(PierceAttackRoutine());
                else
                    StartCoroutine(GrabAttackRoutine());
                break;
            case VineState.Attacking:
            case VineState.Struggling:
            case VineState.Retracting:
                break;
        }
    }

    private void HandleIdleState()
    {
        Vector2 tipPos;
        Vector2 currentLookDir = baseDirection.normalized;

        if (currentVineType == VineType.SweepingGrabber || currentVineType == VineType.InteractiveGrabber)
        {
            float angleOffset = Mathf.Sin((Time.time + manualPhaseOffset) * sweepSpeed) * sweepAngle;
            currentLookDir = Quaternion.Euler(0, 0, angleOffset) * baseDirection.normalized;

            tipPos = (Vector2)vineRoot.position + currentLookDir * 1.5f;
            DrawSwayingVine(tipPos, currentLookDir);
            AimLightAtTarget(tipPos);
        }
        else if (currentVineType == VineType.TrackingPiercer)
        {
            tipPos = (Vector2)vineRoot.position + currentLookDir * 0.5f;
            DrawPiercingVine(tipPos);
            AimLightAtTarget(tipPos);
        }
        else
        {
            tipPos = (Vector2)vineRoot.position + currentLookDir * 1.5f;
            DrawSwayingVine(tipPos, currentLookDir);
            AimLightAtTarget(tipPos);
        }

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

    // =========================================================================
    // 👑 开放给单例 GameManager 绝对掌控的【最高主权熔断命令接口】
    // =========================================================================
    public void ForceReleaseAndAbort()
    {
        if (currentState == VineState.Attacking || currentState == VineState.Struggling)
        {
            Debug.Log("<color=red>⚡【GameManager 强控熔断】触手控制被中央无条件强行拆除！</color>");

            StopAllCoroutines();

            if (escapeProgressBar != null) escapeProgressBar.gameObject.SetActive(false);

            // 🔥 核心重构：利用老哥你的单例架构进行强类型 $O(1)$ 干净注销，砍掉繁琐的反射
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ClearCurrentGrabber(this);
            }

            if (playerHub != null)
            {
                if (playerHub.Control != null) playerHub.Control.isBeingThrown = false;
                playerHub.Control.SetMovementPermission(true);
                if (playerHub.Rb != null) playerHub.Rb.gravityScale = 3f;
            }

            if (playerColliders != null)
            {
                foreach (Collider2D col in playerColliders) col.enabled = true;
            }

            // 换算尖端绝对坐标，执行触手自我退场回收
            Vector2 abortedTipPos = transform.TransformPoint(tipCollider.offset);
            StartCoroutine(CleanDeathRetractRoutine(abortedTipPos));
        }
    }

    private IEnumerator CleanDeathRetractRoutine(Vector2 abortedStartPos)
    {
        currentState = VineState.Retracting;
        Vector2 curTipPos = abortedStartPos;
        float originalIntensity = vineLight != null ? vineLight.intensity : 1f;

        while (Vector2.Distance(curTipPos, vineRoot.position) > 0.2f)
        {
            curTipPos = Vector2.MoveTowards(curTipPos, vineRoot.position, attackSpeed * 0.8f * Time.deltaTime);
            DrawAttackingVine(curTipPos);
            UpdateColliderPosition(curTipPos);
            AimLightAtTarget(curTipPos);
            yield return null;
        }

        if (vineLight != null) { vineLight.color = originalLightColor; vineLight.intensity = originalIntensity; }
        currentState = VineState.Idle;
    }

    // =========================================================================
    // 🕹️ 形态4：操纵挣脱型核心协程（🔥 已洗净为干净的单例上报）
    // =========================================================================
    private IEnumerator InteractiveGrabAttackRoutine()
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
        bool isGrabbed = false;

        while (Vector2.Distance(tipPos, playerHub.Collider.bounds.center) > 0.5f)
        {
            tipPos = Vector2.MoveTowards(tipPos, playerHub.Collider.bounds.center, attackSpeed * Time.deltaTime);
            DrawAttackingVine(tipPos);
            UpdateColliderPosition(tipPos);
            AimLightAtTarget(tipPos);
            if (tipCollider.OverlapPoint(playerHub.Collider.bounds.center)) { isGrabbed = true; break; }
            yield return null;
        }

        if (isGrabbed && playerHub.Control != null)
        {
            currentState = VineState.Struggling;

            // 🔥 核心重构：咬中玩家时，利用标准的全局单例直接进行强类型极速上报登记
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RegisterCurrentGrabber(this);
            }

            playerHub.Control.SetMovementPermission(false);
            if (playerHub.Rb != null) { playerHub.Rb.linearVelocity = Vector2.zero; playerHub.Rb.gravityScale = 0f; }
            if (playerHub.Health != null) playerHub.Health.TakeDamage(damageAmount);

            currentSteerAngle = 0f;
            currentEscapeEnergy = 0f;
            float windowTimer = 0f;

            if (escapeProgressBar != null) { escapeProgressBar.gameObject.SetActive(true); escapeProgressBar.value = 0; }
            bool isEscapedSuccessfully = false;

            while (windowTimer < struggleWindowTime)
            {
                windowTimer += Time.deltaTime;

                float steerInput = Input.GetAxisRaw("Horizontal");
                currentSteerAngle += steerInput * steerSpeed * Time.deltaTime;
                currentSteerAngle = Mathf.Clamp(currentSteerAngle, -maxSteerAngle, maxSteerAngle);

                if (Input.GetKeyDown(KeyCode.F)) currentEscapeEnergy += energyPerTap;
                currentEscapeEnergy -= energyDecaySpeed * Time.deltaTime;
                currentEscapeEnergy = Mathf.Clamp(currentEscapeEnergy, 0f, requiredEscapeEnergy);

                if (escapeProgressBar != null) escapeProgressBar.value = currentEscapeEnergy / requiredEscapeEnergy;

                Vector2 steerDirection = Quaternion.Euler(0, 0, currentSteerAngle) * baseDirection.normalized;
                tipPos = (Vector2)vineRoot.position + steerDirection * 3.5f;

                if (playerHub.Rb != null) playerHub.Rb.MovePosition(tipPos);

                DrawSwayingVine(tipPos, steerDirection);
                AimLightAtTarget(tipPos);

                if (currentEscapeEnergy >= requiredEscapeEnergy) { isEscapedSuccessfully = true; break; }
                yield return null;
            }

            if (escapeProgressBar != null) escapeProgressBar.gameObject.SetActive(false);

            if (isEscapedSuccessfully)
            {
                if (vineLight != null) vineLight.color = Color.green;

                // 🔥 核心重构：成功挣脱，注销句柄
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.ClearCurrentGrabber(this);
                }

                Vector2 finalSpitDir = (tipPos - (Vector2)vineRoot.position).normalized;

                if (playerHub.Rb != null)
                {
                    playerHub.Rb.gravityScale = 3f;
                    playerHub.Rb.linearVelocity = Vector2.zero;
                    playerHub.Rb.AddForce(finalSpitDir * spitForce, ForceMode2D.Impulse);
                }

                if (playerHub.Control != null)
                {
                    StartCoroutine(EnableMovementDelay());
                }
            }
            else
            {
                // QTE失败流程
                if (vineLight != null) vineLight.color = Color.red;

                if (specificThrowPoint != null)
                {
                    playerHub.Control.isBeingThrown = true;
                    if (playerColliders != null) foreach (Collider2D col in playerColliders) col.enabled = false;

                    Vector2 currentPos = tipPos;
                    while (Vector2.Distance(currentPos, specificThrowPoint.position) > 0.3f)
                    {
                        currentPos = Vector2.MoveTowards(currentPos, specificThrowPoint.position, throwMoveSpeed * Time.deltaTime);
                        playerHub.Rb.MovePosition(currentPos);
                        DrawAttackingVine(currentPos);
                        AimLightAtTarget(currentPos);
                        yield return null;
                    }

                    if (playerColliders != null) foreach (Collider2D col in playerColliders) col.enabled = true;
                    playerHub.Control.isBeingThrown = false;
                    playerHub.Rb.gravityScale = 1f;

                    // 🔥 核心重构：惩罚运送抵达终点抛出，注销句柄
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.ClearCurrentGrabber(this);
                    }
                    playerHub.Control.SetMovementPermission(true);

                    Vector2 forwardBoost = ((Vector2)specificThrowPoint.position - currentPos).normalized;
                    playerHub.Rb.AddForce(forwardBoost * 8f, ForceMode2D.Impulse);
                }
                else
                {
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.ClearCurrentGrabber(this);
                    }
                    playerHub.Rb.gravityScale = 1f;
                    playerHub.Control.SetMovementPermission(true);
                }
            }
        }

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

    // =========================================================================
    // 🪤 形态1&2：常规老版抛投协程（🔥 同步注入强类型单例上报）
    // =========================================================================
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

        while (Vector2.Distance(tipPos, playerHub.Collider.bounds.center) > 0.5f)
        {
            tipPos = Vector2.MoveTowards(tipPos, playerHub.Collider.bounds.center, attackSpeed * Time.deltaTime);
            DrawAttackingVine(tipPos); UpdateColliderPosition(tipPos); AimLightAtTarget(tipPos);
            if (tipCollider.OverlapPoint(playerHub.Collider.bounds.center)) break;
            yield return null;
        }

        if (playerHub.Control != null && playerHub.Control.canMove)
        {
            // 🔥 核心重构：老版抛投抓到人，单例强类型上报
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RegisterCurrentGrabber(this);
            }

            playerHub.Control.SetMovementPermission(false);
            if (playerHub.Rb != null) { playerHub.Rb.linearVelocity = Vector2.zero; playerHub.Rb.gravityScale = 0f; }
            if (playerHub.Health != null) playerHub.Health.TakeDamage(damageAmount);

            float holdTimer = 0;
            while (holdTimer < holdTime)
            {
                playerHub.transform.position = tipPos;
                DrawWrappingVine(tipPos, holdTimer / holdTime); AimLightAtTarget(tipPos);
                holdTimer += Time.deltaTime;
                yield return null;
            }

            if (playerHub.Rb != null && specificThrowPoint != null)
            {
                playerHub.Control.isBeingThrown = true;
                if (playerColliders != null) foreach (Collider2D col in playerColliders) col.enabled = false;

                Vector2 currentPos = tipPos;
                while (Vector2.Distance(currentPos, specificThrowPoint.position) > 0.3f)
                {
                    currentPos = Vector2.MoveTowards(currentPos, specificThrowPoint.position, throwMoveSpeed * Time.deltaTime);
                    playerHub.Rb.MovePosition(currentPos); DrawAttackingVine(currentPos); AimLightAtTarget(currentPos);
                    yield return null;
                }

                if (playerColliders != null) foreach (Collider2D col in playerColliders) col.enabled = true;
                playerHub.Control.isBeingThrown = false;
                playerHub.Rb.gravityScale = 1f;

                // 🔥 核心重构：抛出，单例强类型注销
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.ClearCurrentGrabber(this);
                }
                playerHub.Control.SetMovementPermission(true);

                Vector2 forwardBoost = ((Vector2)specificThrowPoint.position - currentPos).normalized;
                playerHub.Rb.AddForce(forwardBoost * 8f, ForceMode2D.Impulse);
            }
            else
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.ClearCurrentGrabber(this);
                }
                if (playerHub.Rb != null) playerHub.Rb.gravityScale = 1f;
                playerHub.Control.SetMovementPermission(true);
            }
        }

        currentState = VineState.Retracting;
        while (Vector2.Distance(tipPos, vineRoot.position) > 0.2f)
        {
            tipPos = Vector2.MoveTowards(vineRoot.position, tipPos, attackSpeed * 0.8f * Time.deltaTime);
            DrawAttackingVine(tipPos); UpdateColliderPosition(tipPos); AimLightAtTarget(tipPos);
            yield return null;
        }
        if (vineLight != null) { vineLight.color = originalLightColor; vineLight.intensity = originalIntensity; }
        currentState = VineState.Idle;
    }

    // =========================================================================
    // 🎬 Boss剧情大导演演出、形态3穿刺、线段渲染矩阵（完全保留）
    // =========================================================================
    public IEnumerator ExecuteDirectorGrab(PlayerHub targetHub, float pullTime)
    {
        if (targetHub == null) yield break;
        currentState = VineState.Attacking;
        if (vineLight != null) { vineLight.color = Color.red; vineLight.intensity = 3f; }
        Vector2 tipPos = vineRoot.position;

        while (Vector2.Distance(tipPos, targetHub.Collider.bounds.center) > 0.5f)
        {
            tipPos = Vector2.MoveTowards(tipPos, targetHub.Collider.bounds.center, attackSpeed * 2f * Time.deltaTime);
            DrawAttackingVine(tipPos); AimLightAtTarget(tipPos);
            yield return null;
        }

        float timer = 0f;
        Vector3 playerStartPos = targetHub.transform.position;
        while (timer < pullTime)
        {
            timer += Time.deltaTime;
            float t = timer / pullTime;
            targetHub.transform.position = Vector3.Lerp(playerStartPos, vineRoot.position, t);
            tipPos = targetHub.transform.position;
            DrawWrappingVine(tipPos, t); AimLightAtTarget(pullTime > 0 ? tipPos : (Vector2)targetHub.transform.position);
            yield return null;
        }

        currentState = VineState.Retracting;
        while (Vector2.Distance(tipPos, vineRoot.position) > 0.2f)
        {
            tipPos = Vector2.MoveTowards(tipPos, vineRoot.position, attackSpeed * Time.deltaTime);
            DrawAttackingVine(tipPos); UpdateColliderPosition(tipPos); AimLightAtTarget(tipPos);
            yield return null;
        }
        if (vineLight != null) { vineLight.color = originalLightColor; vineLight.intensity = 1f; }
        line.positionCount = 0; currentState = VineState.Idle;
    }

    private IEnumerator PierceAttackRoutine()
    {
        currentState = VineState.Attacking;
        float originalIntensity = vineLight != null ? vineLight.intensity : 1f;
        float timer = 0f;
        while (timer < startDelay)
        {
            timer += Time.deltaTime;
            if (vineLight != null && flickerWarning) vineLight.intensity = Mathf.PingPong(Time.time * 20f, 2f) + originalIntensity;
            yield return null;
        }
        Vector2 finalCenter = playerHub.Collider.bounds.center;
        Vector2 dir = (finalCenter - (Vector2)vineRoot.position).normalized;
        lockedTargetPos = finalCenter + dir * 1f;
        Vector2 tipPos = vineRoot.position;
        bool hasDamaged = false;
        while (Vector2.Distance(tipPos, lockedTargetPos) > 0.1f)
        {
            tipPos = Vector2.MoveTowards(tipPos, lockedTargetPos, attackSpeed * 2f * Time.deltaTime);
            DrawPiercingVine(tipPos); UpdateColliderPosition(tipPos); AimLightAtTarget(tipPos);
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
            DrawPiercingVine(tipPos); UpdateColliderPosition(tipPos); AimLightAtTarget(tipPos);
            yield return null;
        }
        if (vineLight != null) { vineLight.color = originalLightColor; vineLight.intensity = originalIntensity; }
        currentState = VineState.Idle;
    }

    private void OnDrawGizmosSelected()
    {
        if (vineRoot != null && currentVineType == VineType.InteractiveGrabber)
        {
            Gizmos.color = Color.cyan;
            Vector3 lookDir = baseDirection.normalized;
            Vector3 leftLimit = Quaternion.Euler(0, 0, -maxSteerAngle) * lookDir;
            Vector3 rightLimit = Quaternion.Euler(0, 0, maxSteerAngle) * lookDir;
            Gizmos.DrawRay(vineRoot.position, leftLimit * 3.5f);
            Gizmos.DrawRay(vineRoot.position, rightLimit * 3.5f);
            Gizmos.DrawLine(vineRoot.position + leftLimit * 3.5f, vineRoot.position + rightLimit * 3.5f);
        }
    }

    private void UpdateColliderPosition(Vector2 tipPos) { tipCollider.offset = transform.InverseTransformPoint(tipPos); }
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

    private void DrawSwayingVine(Vector2 tipPos, Vector2 lookDir)
    {
        line.positionCount = segmentCount;
        line.startWidth = originalStartWidth; line.endWidth = originalEndWidth;
        for (int i = 0; i < segmentCount; i++)
        {
            float t = i / (float)(segmentCount - 1);
            Vector2 point = Vector2.Lerp(vineRoot.position, tipPos, t);
            Vector2 normal = new Vector2(-lookDir.y, lookDir.x);
            float sway = Mathf.Sin((Time.time + manualPhaseOffset) * swaySpeed + i * 0.5f) * swayMagnitude * t;
            point += normal * sway; line.SetPosition(i, point);
        }
    }

    private void DrawAttackingVine(Vector2 tipPos)
    {
        line.positionCount = segmentCount;
        line.startWidth = originalStartWidth; line.endWidth = originalEndWidth;
        for (int i = 0; i < segmentCount; i++)
        {
            float t = i / (float)(segmentCount - 1);
            Vector2 point = Vector2.Lerp(vineRoot.position, tipPos, t);
            float sway = Mathf.Sin((Time.time + manualPhaseOffset) * swaySpeed * 3f + i) * swayMagnitude * 0.5f * t;
            Vector2 dir = (tipPos - (Vector2)vineRoot.position).normalized;
            Vector2 normal = new Vector2(-dir.y, dir.x);
            point += normal * sway; line.SetPosition(i, point);
        }
    }

    private void DrawWrappingVine(Vector2 center, float progress)
    {
        line.positionCount = segmentCount;
        line.startWidth = originalStartWidth; line.endWidth = originalEndWidth;
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
        line.startWidth = pierceWidthRoot; line.endWidth = pierceWidthTip;
        line.SetPosition(0, vineRoot.position); line.SetPosition(1, tipPos);
    }

    private IEnumerator EnableMovementDelay()
    {
        yield return new WaitForSeconds(0.3f);
        playerHub.Control.SetMovementPermission(true);
    }
}