using UnityEngine;
using System; // 🌟 必须引入 System 才能使用 Action 和 Serializable
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(LineRenderer))]
public class ButterflyAltar : MonoBehaviour
{
    public static List<ButterflyAltar> ActiveAltars = new List<ButterflyAltar>();

    // ==============================================================
    // 🌟🌟🌟 数据驱动：核心结构体定义 🌟🌟🌟
    // ==============================================================
    [System.Serializable]
    public struct AltarTierConfig
    {
        public string tierName;       // 阶段名称（辅助注释，如 "初始态", "微能充能", "狂暴超载"）
        public int energyThreshold;   // 达到该阶段所需的能量阈值
        public int laserDamage;       // 该阶段每秒对 Boss 造成的伤害值
        public int regenPerSecond;    // 该阶段每秒自我修复的耐久度值
    }

    [Header("📊 数据驱动阶段配置表")]
    [Tooltip("请在面板中配置 6 个元素（对应阶段 0 到 5）。代码将完全根据此表提供的数据运作！")]
    public AltarTierConfig[] altarTiers = new AltarTierConfig[6];

    [Header("🛡️ 祭坛基础实时状态")]
    public int maxDurability = 1000;
    public int currentDurability;
    public int maxEnergy = 400;
    public int currentEnergy = 0;
    public int currentPhase = 0;

    
    public event Action OnAltarDestroyed;                  // 祭坛毁灭广播
    public event Action<int, int> OnDurabilityChanged;     // 耐久度变化广播 (当前值, 最大值)
    public event Action<int, int> OnEnergyChanged;         // 能量变化广播 (当前值, 最大值)
    public event Action<int> OnPhaseUpgraded;              // 阶段提升广播 (当前阶段索引)

    [Header("⏱️ 战斗与特效配置")]
    public float attackInterval = 1.0f;
    public EyeBossController bossEye;
    [ColorUsage(true, true)] public Color attackLaserColor = Color.cyan;

    private LineRenderer lineRenderer;
    
    private bool isDestroyed = false;

    [Header("🏆 胜利奖励引用")]
    public GameObject trappedButterfly;
    public GameObject shuttleOrb;
    public GameObject ashParticlePrefab; // 损坏时的灰烬预制体

    private void Awake()
    {
        currentDurability = maxDurability;
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.enabled = false;
        lineRenderer.startColor = attackLaserColor;
        lineRenderer.endColor = attackLaserColor;
    }

    private void OnEnable() { if (!ActiveAltars.Contains(this)) ActiveAltars.Add(this); }
    private void OnDisable() { if (ActiveAltars.Contains(this)) ActiveAltars.Remove(this); }

    private void Start()
    {
        // 游戏开局：双线程分别启动自动攻击与自动修复
        StartCoroutine(AutoAttackRoutine());
        StartCoroutine(AutoRegenRoutine());
    }

    public void AddEnergy(int amount)
    {
        currentEnergy = Mathf.Clamp(currentEnergy + amount, 0, maxEnergy);

        // 广播能量变化（用于刷新 UI 能量条）
        OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);

        UpdatePhase();
    }

    public void TakeDamage(int amount)
    {
        if (isDestroyed) return;

        currentDurability -= amount;

        // 广播耐久度变化（用于刷新 UI 血条）
        OnDurabilityChanged?.Invoke(currentDurability, maxDurability);

        if (currentDurability <= 0)
        {
            Debug.LogError("!!! 祭坛已彻底摧毁 !!!");
            isDestroyed = true;
            OnAltarDestroyed?.Invoke(); // 全局广播死亡，交给大导演处理
        }
    }

    // 🌟 优化：基于数据表的通用阶段判断
    private void UpdatePhase()
    {
        // 从最高阶开始向下比对能量阈值
        for (int i = altarTiers.Length - 1; i >= 0; i--)
        {
            if (currentEnergy >= altarTiers[i].energyThreshold)
            {
                if (currentPhase != i)
                {
                    currentPhase = i;
                    OnPhaseUpgraded?.Invoke(currentPhase); // 广播升级事件，未来可用于触发震屏或特效
                    Debug.Log($"<color=cyan>[祭坛状态演进]</color> 进入：{altarTiers[currentPhase].tierName} (阶段 {currentPhase})");
                }
                break;
            }
        }
    }

    // 🌟 优化：基于数据表的数据驱动攻击线
    private IEnumerator AutoAttackRoutine()
    {
        while (currentDurability > 0)
        {
            yield return new WaitForSeconds(attackInterval);

            // 安全判定：直接去数据配置表里捞对应阶段的伤害，0个if-else判断！
            int damage = altarTiers[currentPhase].laserDamage;

            if (damage > 0 && bossEye != null && bossEye.CurrentHealth > 0)
            {
                StartCoroutine(FireLaserAtBoss(damage));
            }
        }
    }

    // 🌟 优化：基于数据表的数据驱动自动回血线
    private IEnumerator AutoRegenRoutine()
    {
        while (!isDestroyed)
        {
            yield return new WaitForSeconds(1.0f); // 固定每秒检测一次

            // 只有当受损了，且祭坛还活着时执行修复
            if (currentDurability > 0 && currentDurability < maxDurability)
            {
                // 核心：直接读取当前阶段配置的回血速度，完全抹除硬编码数值！
                int regenAmount = altarTiers[currentPhase].regenPerSecond;

                if (regenAmount > 0)
                {
                    currentDurability = Mathf.Min(currentDurability + regenAmount, maxDurability);

                    // 必须广播通知 UI 刷新血条
                    OnDurabilityChanged?.Invoke(currentDurability, maxDurability);

                    Debug.Log($"<color=green>[祭坛自修]</color> 触发 {altarTiers[currentPhase].tierName} 能力，自动修复 +{regenAmount}! 当前耐久:{currentDurability}/{maxDurability}");
                }
            }
        }
    }

    private IEnumerator FireLaserAtBoss(int damage)
    {
        lineRenderer.enabled = true;
        lineRenderer.SetPosition(0, transform.position);
        lineRenderer.SetPosition(1, bossEye.transform.position);
        bossEye.TakeDamage(damage);
        yield return new WaitForSeconds(0.15f);
        lineRenderer.enabled = false;
    }

    // 大导演在大回合重置时调用
    public void ResetAltar()
    {
        // 强力擦屁股：由于腰斩了协程，必须同时重新拉起攻击和修复两个线程！
        StopAllCoroutines();
        isDestroyed = false;
        currentDurability = maxDurability;
        currentEnergy = 0;
        currentPhase = 0;

        // 数据归零重置广播
        OnDurabilityChanged?.Invoke(currentDurability, maxDurability);
        OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
        OnPhaseUpgraded?.Invoke(currentPhase); // 广播升级事件，未来可用于触发震屏或特效

        if (trappedButterfly != null) trappedButterfly.SetActive(true);
        GetComponent<SpriteRenderer>().enabled = true;

        
        if (lineRenderer != null) lineRenderer.enabled = false;

        StartCoroutine(AutoAttackRoutine());
        StartCoroutine(AutoRegenRoutine());
    }

    public void ChangeAltarGlowColor(Color newColor)
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.material.SetColor("_GlowColor", newColor);
    }
}