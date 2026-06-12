using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
[ExecuteAlways]
public class LaserTrap : MonoBehaviour
{
    public enum LaserBehaviorType { Static, DissolveTimer, Moving }

    [Header("行为模式设定")]
    public LaserBehaviorType behaviorType = LaserBehaviorType.Static;
    public float startDelay = 0f;

    [Header("激光基础设置")]
    public int damageAmount = 999;
    public float transitionDuration = 0.8f;

    [Header("溶解模式设置 (DissolveTimer)")]
    public float timeOn = 3.0f;
    public float timeOff = 2.0f;

    [Header("移动模式设置 (Moving)")]
    public float moveDistance = 5f;
    public float moveDuration = 2f;
    public float movePauseTime = 0.5f;

    private SpriteRenderer laserRenderer;
    private BoxCollider2D laserCollider;
    private Material uniqueMaterial;

    private static readonly int LengthID = Shader.PropertyToID("_LaserLength");
    private static readonly int DissolveID = Shader.PropertyToID("_DissolveAmount");

    private bool isShuttingDown = false;
    private Coroutine behaviorCoroutine;
    private float lastSizeX = -1f;

    private Vector3 startPos;
    private Vector3 targetPos;

    [Header("事件配置 (Event-Driven)")]
    public UnityEvent<GameObject> OnPlayerHitLaser;

    [Header("传送与惩罚 (针对底部防坠落激光)")]
    public bool teleportOnHit = false;
    [Tooltip("扣血后将玩家传送到这个位置 (可以拖入祭坛位置)")]
    public Transform teleportTarget;
    private void Awake()
    {
        laserRenderer = GetComponent<SpriteRenderer>();
        laserCollider = GetComponent<BoxCollider2D>();

        if (Application.isPlaying) uniqueMaterial = laserRenderer.material;
        else uniqueMaterial = laserRenderer.sharedMaterial;
    }

    private void Start()
    {
        laserRenderer.enabled = false;
        laserRenderer.enabled = true;

        if (Application.isPlaying)
        {
            switch (behaviorType)
            {
                case LaserBehaviorType.DissolveTimer:
                    behaviorCoroutine = StartCoroutine(TimerLoopRoutine());
                    break;
                case LaserBehaviorType.Moving:
                    startPos = transform.position;
                    targetPos = startPos + transform.right * moveDistance;
                    behaviorCoroutine = StartCoroutine(MoveLoopRoutine());
                    break;
                case LaserBehaviorType.Static:
                default: break;
            }
        }
    }

    private void Update()
    {
        // 🌟 核心修复 1：在 [ExecuteAlways] 下，Unity 编译重载会让 private 变量变回 null！
        // 我们必须在 Update 开头做保底自动获取（懒加载），彻底消灭编辑器的“间歇性失忆”
        if (laserRenderer == null) laserRenderer = GetComponent<SpriteRenderer>();
        if (laserCollider == null) laserCollider = GetComponent<BoxCollider2D>();
        if (uniqueMaterial == null && laserRenderer != null)
        {
            if (Application.isPlaying) uniqueMaterial = laserRenderer.material;
            else uniqueMaterial = laserRenderer.sharedMaterial;
        }

        // 绝对安全锁：如果连组件都拿不到，直接返回，不往下走
        if (laserRenderer == null) return;

        // ==============================================================
        // 🎨 渲染层：处理 Shader 激光拉伸与溶解（独立运行）
        // ==============================================================
        if (uniqueMaterial != null)
        {
            if (laserRenderer.size.x != lastSizeX)
            {
                lastSizeX = laserRenderer.size.x;
                uniqueMaterial.SetFloat(LengthID, laserRenderer.size.x);
                uniqueMaterial.SetFloat(DissolveID, isShuttingDown ? 1.1f : 0f);
            }
        }

        // ==============================================================
        // 🛡️ 物理层：处理碰撞框缩放（独立运行，不再受 uniqueMaterial 牵连！）
        // ==============================================================
        if (laserCollider != null)
        {
            // 🌟 核心修复 2：先算好我们“期望”的理想碰撞框尺寸
            Vector2 targetColliderSize = new Vector2(laserRenderer.size.x, laserRenderer.size.y * 0.5f);

            // 🌟 核心修复 3：拿当前碰撞框和“理想尺寸”做对比！
            // 这样既能解决开局不触发的 Bug，又能避免因为 Y 轴乘了 0.5 导致条件永远成立而疯狂 Re-bake 物理的严重掉帧 Bug！
            if (laserCollider.size != targetColliderSize)
            {
                // 应用缩小的尺寸
                laserCollider.size = targetColliderSize;

                // 重新计算 Offset 偏移
                // 💡 提醒：如果你的激光 Sprite 锚点（Pivot）在【最左侧】，用下面这行：
                laserCollider.offset = new Vector2(targetColliderSize.x / 2f, 0f);

                // 💡 提醒：如果你的激光 Sprite 锚点（Pivot）在【正中心】，请把上面这行删掉，直接改成：
                // laserCollider.offset = Vector2.zero;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isShuttingDown) return;

        if (collision.CompareTag("Player"))
        {
            OnPlayerHitLaser?.Invoke(collision.gameObject);

            // 实体保底扣血
            CharacterHealth health = collision.GetComponent<CharacterHealth>();
            if (health != null) health.TakeDamage(damageAmount);
            // 🌟 新增：触发传送机制
            if (teleportOnHit && teleportTarget != null)
            {
                collision.transform.position = teleportTarget.position;

                // 刹车，防止玩家带着掉落的速度继续飞
                Rigidbody2D rb = collision.GetComponent<Rigidbody2D>();
                if (rb != null) rb.linearVelocity = Vector2.zero; // Unity 6 语法
            }
        }
    }

    public void TurnOffLaser()
    {
        if (!isShuttingDown && Application.isPlaying)
        {
            if (behaviorCoroutine != null) StopCoroutine(behaviorCoroutine);
            StartCoroutine(TransitionRoutine(true));
        }
    }

    // ==============================================================
    // 🌟 Boss 专用的动态生长接口 (完美复刻你的缓动代码) 
    // ==============================================================
    public void SetupBossLaser(float targetLength, float stretchTime, float stayTime, float vanishTime)
    {
        if (laserRenderer == null) laserRenderer = GetComponent<SpriteRenderer>();

        laserRenderer.size = new Vector2(0f, laserRenderer.size.y);

        StartCoroutine(DynamicGrowRoutine(targetLength, stretchTime, stayTime, vanishTime));
    }

    private IEnumerator DynamicGrowRoutine(float targetLength, float stretchTime, float stayTime, float vanishTime)
    {
        float elapsed = 0f;

        // 1. 伸长阶段
        while (elapsed < stretchTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / stretchTime;
            float easeOutT = 1f - Mathf.Pow(1f - t, 4f);

            float currentLength = Mathf.Lerp(0f, targetLength, easeOutT);
            laserRenderer.size = new Vector2(currentLength, laserRenderer.size.y);
            yield return null;
        }
        laserRenderer.size = new Vector2(targetLength, laserRenderer.size.y);

        // 2. 停留阶段
        yield return new WaitForSeconds(stayTime);

        // 3. 消失阶段
        yield return StartCoroutine(TransitionRoutine(true, vanishTime));

        Destroy(gameObject);
    }

    // ==============================================================
    // 🚨 找回刚才被我脑残弄丢的协程：你原有的闪烁和移动逻辑全在这！🚨
    // ==============================================================

    private IEnumerator TimerLoopRoutine()
    {
        if (startDelay > 0f) yield return new WaitForSeconds(startDelay);

        while (true)
        {
            yield return new WaitForSeconds(timeOn);
            yield return StartCoroutine(TransitionRoutine(true));
            yield return new WaitForSeconds(timeOff);
            yield return StartCoroutine(TransitionRoutine(false));
        }
    }

    private IEnumerator MoveLoopRoutine()
    {
        if (startDelay > 0f) yield return new WaitForSeconds(startDelay);

        while (true)
        {
            yield return StartCoroutine(MoveToPosition(targetPos));
            if (movePauseTime > 0f) yield return new WaitForSeconds(movePauseTime);

            yield return StartCoroutine(MoveToPosition(startPos));
            if (movePauseTime > 0f) yield return new WaitForSeconds(movePauseTime);
        }
    }

    private IEnumerator MoveToPosition(Vector3 destination)
    {
        Vector3 origin = transform.position;
        float percent = 0f;

        while (percent < 1f)
        {
            percent += Time.deltaTime / moveDuration;
            float easedPercent = Mathf.SmoothStep(0f, 1f, percent);
            transform.position = Vector3.Lerp(origin, destination, easedPercent);
            yield return null;
        }
        transform.position = destination;
    }

    private IEnumerator TransitionRoutine(bool turningOff, float overrideDuration = -1f)
    {
        float durationToUse = overrideDuration > 0f ? overrideDuration : transitionDuration;

        if (turningOff)
        {
            isShuttingDown = true;
            if (laserCollider != null) laserCollider.enabled = false;
        }
        else
        {
            if (laserRenderer != null) laserRenderer.enabled = true;
        }

        float timer = 0f;
        float startDissolve = turningOff ? 0f : 1.1f;
        float endDissolve = turningOff ? 1.1f : 0f;

        while (timer < durationToUse)
        {
            timer += Time.deltaTime;
            float currentDissolve = Mathf.Lerp(startDissolve, endDissolve, timer / durationToUse);

            if (laserRenderer != null && uniqueMaterial != null)
            {
                uniqueMaterial.SetFloat(DissolveID, currentDissolve);
            }

            yield return null;
        }

        if (turningOff)
        {
            if (laserRenderer != null) laserRenderer.enabled = false;
        }
        else
        {
            if (laserCollider != null) laserCollider.enabled = true;
            isShuttingDown = false;
        }
    }
}