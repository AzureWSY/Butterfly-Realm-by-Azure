using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
[ExecuteAlways]
public class LaserTrapCore : MonoBehaviour
{
    [Header("🛡️ 激光核心基础设置")]
    public int damageAmount = 999;
    public float transitionDuration = 0.5f;

    [Header("📡 事件配置 (Event-Driven)")]
    public UnityEvent<GameObject> OnPlayerHitLaser;

    [Header("🔮 坠落惩罚设置")]
    public bool teleportOnHit = false;
    public Transform teleportTarget;

    private SpriteRenderer laserRenderer;
    private BoxCollider2D laserCollider;
    private Material uniqueMaterial;

    private bool isShuttingDown = false;
    private float lastSizeX = -1f;
    private Coroutine transitionCoroutine;

    private static readonly int LengthID = Shader.PropertyToID("_LaserLength");
    private static readonly int DissolveID = Shader.PropertyToID("_DissolveAmount");

    public SpriteRenderer LaserRenderer => laserRenderer;
    public bool IsShuttingDown => isShuttingDown;

    private void Awake()
    {
        laserRenderer = GetComponent<SpriteRenderer>();
        laserCollider = GetComponent<BoxCollider2D>();
        CacheMaterial();
    }

    private void OnEnable()
    {
        // 🌟 强行标记一次脏，确保生成或激活动态注册到中央管理器刷新
        MarkGeometryDirty();
    }

    // 🌟 编辑器专属保底：确保关卡设计师在摆放拉伸大小时，不需要运行就能实时对齐物理和 Shader
#if UNITY_EDITOR
    private void Update()
    {
        if (Application.isPlaying) return; // 运行期直接熔断！绝不上报 CPU 主循环开销

        if (laserRenderer == null) laserRenderer = GetComponent<SpriteRenderer>();
        if (laserCollider == null) laserCollider = GetComponent<BoxCollider2D>();
        CacheMaterial();
        ResolveGeometryUpdate();
    }
#endif

    /// <summary>
    /// 👑 核心切入点：任何行为驱动器（如位移、拉伸脚本）修改了我的大小时，调用此方法上报中央大管家
    /// </summary>
    public void MarkGeometryDirty()
    {
        if (!Application.isPlaying) return;

        // 向上交税：直接把自己塞进中央管理器的待刷新脏队列中
        if (LaserTrapManager.Instance != null)
        {
            LaserTrapManager.Instance.RegisterDirtyCore(this);
        }
    }

    /// <summary>
    /// 🔮 批处理原子接口：由中央管理器在安全时序下统一调用，完成物理盒与 Shader 的最终合并同步
    /// </summary>
    public void ResolveGeometryUpdate()
    {
        if (laserRenderer == null) return;

        // 🎨 渲染层脏拦截
        if (uniqueMaterial != null && !Mathf.Approximately(laserRenderer.size.x, lastSizeX))
        {
            lastSizeX = laserRenderer.size.x;
            uniqueMaterial.SetFloat(LengthID, laserRenderer.size.x);
        }

        // 🛡️ 物理层脏拦截
        if (laserCollider != null)
        {
            Vector2 targetColliderSize = new Vector2(laserRenderer.size.x, laserRenderer.size.y * 0.5f);
            if (laserCollider.size != targetColliderSize)
            {
                laserCollider.size = targetColliderSize;
                laserCollider.offset = new Vector2(targetColliderSize.x / 2f, 0f);
            }
        }
    }

    private void CacheMaterial()
    {
        if (uniqueMaterial == null && laserRenderer != null)
        {
            uniqueMaterial = Application.isPlaying ? laserRenderer.material : laserRenderer.sharedMaterial;
        }
    }

    public void TurnOnLaser()
    {
        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
        if (Application.isPlaying)
            transitionCoroutine = StartCoroutine(TransitionRoutine(false, transitionDuration));
        else
            SetImmediateDissolve(0f);
    }

    public void TurnOffLaser()
    {
        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
        if (Application.isPlaying)
            transitionCoroutine = StartCoroutine(TransitionRoutine(true, transitionDuration));
        else
            SetImmediateDissolve(1.1f);
    }

    public void SetImmediateDissolve(float amount)
    {
        CacheMaterial();
        if (uniqueMaterial != null) uniqueMaterial.SetFloat(DissolveID, amount);
    }

    public IEnumerator TransitionRoutine(bool turningOff, float duration)
    {
        CacheMaterial();
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

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float currentDissolve = Mathf.Lerp(startDissolve, endDissolve, timer / duration);
            if (uniqueMaterial != null) uniqueMaterial.SetFloat(DissolveID, currentDissolve);
            yield return null;
        }

        if (uniqueMaterial != null) uniqueMaterial.SetFloat(DissolveID, endDissolve);

        if (turningOff)
        {
            if (laserRenderer != null) laserRenderer.enabled = false;
        }
        else
        {
            if (laserCollider != null) laserCollider.enabled = true;
            isShuttingDown = false;
        }
        transitionCoroutine = null;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isShuttingDown) return;
        if (!collision.gameObject.CompareTag("Player"))  return;

        OnPlayerHitLaser?.Invoke(collision.gameObject);
        if (collision.gameObject.TryGetComponent<CharacterHealth>(out var health))  health.TakeDamage(damageAmount);

        if (teleportOnHit && teleportTarget != null)
        {
            if (collision.gameObject.TryGetComponent<PlayerHub>(out var hub)) hub.Teleport(teleportTarget.position);
        }
    }
}