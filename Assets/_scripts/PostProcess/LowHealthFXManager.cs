using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Volume))]
public class LowHealthFXManager : MonoBehaviour
{
    [Header("❤️ 玩家血量实体")]
    public PlayerHealth playerHealth;

    [Header("🎨 闪烁动画设置")]
    public float transitionSpeed = 5f;
    public float flashSpeed = 4f;

    private Volume lowHealthVolume;
    private bool isLowHealth = false; // 🌟 核心防线：由 Action 严格控制的状态开关

    private void Start()
    {
        lowHealthVolume = GetComponent<Volume>();
        lowHealthVolume.weight = 0f;
    }

    private void OnEnable()
    {
        // 🌟 牵起事件电话线：只要血量一变，立刻通知我
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += CheckLowHealthStatus;
            CheckLowHealthStatus(playerHealth.CurrentHealth, playerHealth.MaxHealth);
        }
    }

    private void OnDisable()
    {
        // 🚨 记得掐断电话线，防止幽灵残留监听
        if (playerHealth != null) playerHealth.OnHealthChanged -= CheckLowHealthStatus;
    }

    // 🌟 只有当血量真正改变时，这个函数才会被动触发一次！（平时死寂，0开销）
    private void CheckLowHealthStatus(int currentHealth, int maxHealth)
    {
        float healthPercent = (float)currentHealth / maxHealth;

        // 改变开关状态
        isLowHealth = (healthPercent <= 0.2f && currentHealth > 0);
    }

    private void Update()
    {
        // 🌟 性能大跨越：平时玩家健康时，Update 跑到这一行直接被强行拦截返回！
        // 每一帧不需要做任何除法，也不需要读取复杂的血量变量，性能极其强悍！
        if (!isLowHealth && lowHealthVolume.weight <= 0f) return;

        // ==============================================================
        // 🎨 以下是纯表现层动画（只在低血量、或者红圈正在淡出的短暂时间内执行）
        // ==============================================================
        float flashTarget = 0f;

        if (isLowHealth)
        {
            // 算心跳闪烁值
            flashTarget = 0.65f + Mathf.Sin(Time.time * flashSpeed) * 0.25f;
        }
        else
        {
            // 如果脱离了低血量状态，目标就是 0f（让红圈淡出）
            flashTarget = 0f;
        }

        // 丝滑融合
        lowHealthVolume.weight = Mathf.MoveTowards(lowHealthVolume.weight, flashTarget, Time.deltaTime * transitionSpeed);
    }
}