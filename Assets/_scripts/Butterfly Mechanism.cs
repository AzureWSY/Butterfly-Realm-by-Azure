using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class ButterflyMechanism : MonoBehaviour
{
    [Header("交互设置")]
    public KeyCode interactKey = KeyCode.E;

    [Header("视觉表现(发光)")]
    public SpriteRenderer mechanismRenderer;
    public float targetGlowIntensity = 3.0f; // 最终发光强度
    public float glowDuration = 1.0f;        // 渐亮耗时(秒)

    [Header("专业双层法")]
    public GameObject burstPrefab;  // 拖入 FX_Butterfly_Burst
    public GameObject streamPrefab; // 拖入 FX_Butterfly_Stream


    // 新增曲线的四个控制点
    public Transform p0_Start;
    public Transform p1_Control;
    public Transform p2_Control;
    public Transform p3_End;

    private bool isActivated = false;
    private bool isPlayerInRange = false;

    // 性能神器：属性块，修改Shader参数且不破坏合批
    private MaterialPropertyBlock propBlock;
    private static readonly int GlowIntensityProperty = Shader.PropertyToID("_GlowIntensity"); // 缓存ID提速

    [Header("交互成功后触发的事件")]
    public UnityEvent OnTerminalHacked; // 这里挂 UnityEvent！

    void Start()
    {
        // 初始化属性块，确保初始状态为不发光 (0)
        propBlock = new MaterialPropertyBlock();
        mechanismRenderer.GetPropertyBlock(propBlock);
        propBlock.SetFloat(GlowIntensityProperty, 0f);
        mechanismRenderer.SetPropertyBlock(propBlock);
    }

    void Update()
    {
        if (isPlayerInRange && !isActivated && Input.GetKeyDown(interactKey))
        {
            ActivateMechanism();
        }
    }

    private void ActivateMechanism()
    {
        isActivated = true;
        StartCoroutine(SmoothGlowRoutine());

        // 1. 生成原地爆发层（不需要路径脚本，炸完自毁）
        if (burstPrefab != null)
        {
            GameObject burst = Instantiate(burstPrefab, transform.position, Quaternion.identity);
            Destroy(burst, 8.0f); // 2秒后清理
        }

        OnTerminalHacked?.Invoke();
        // 2. 生成流向层（执行贝塞尔曲线逻辑）
        if (streamPrefab != null)
        {
            GameObject stream = Instantiate(streamPrefab, transform.position, Quaternion.identity);
            SwarmPathFollower follower = stream.GetComponent<SwarmPathFollower>();
            if (follower != null)
            {
                // 给它点位，让它飞！
                follower.SetPath(p0_Start, p1_Control, p2_Control, p3_End);
            }
        }
    }

   
    // 协程：处理平滑的数值变化
    private IEnumerator SmoothGlowRoutine()
    {
        float elapsedTime = 0f;
        while (elapsedTime < glowDuration)
        {
            elapsedTime += Time.deltaTime;

            // 【升级：引入 SmoothStep】让发光的视觉过渡具备更自然的物理惯性感
            float t = elapsedTime / glowDuration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            // Mathf.Lerp 用于平滑插值，传入处理过的 smoothT
            float currentIntensity = Mathf.Lerp(0f, targetGlowIntensity, smoothT);

            // 使用 PropertyBlock 应用新数值
            mechanismRenderer.GetPropertyBlock(propBlock);
            propBlock.SetFloat(GlowIntensityProperty, currentIntensity);
            mechanismRenderer.SetPropertyBlock(propBlock);

            // 等待下一帧
            yield return null;
        }

        // 兜底：确保最终发光值极其精准地等于目标值，防止浮点数误差
        mechanismRenderer.GetPropertyBlock(propBlock);
        propBlock.SetFloat(GlowIntensityProperty, targetGlowIntensity);
        mechanismRenderer.SetPropertyBlock(propBlock);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInRange = true;
            GameManager.Instance.SetNewRespawnPoint(transform.position);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player")) isPlayerInRange = false;
    }
}