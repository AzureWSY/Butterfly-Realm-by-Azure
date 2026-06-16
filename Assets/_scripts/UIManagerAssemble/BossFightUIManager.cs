using UnityEngine;
using UnityEngine.UI; // 必须引入基础 UI 库
using TMPro;          // 必须引入 TextMeshPro 库
using System;

public class BossFightUIManager : MonoBehaviour
{
    [Header("🔌 核心演员数据引用")]
    public EyeBossController bossEntity;
    public ButterflyAltar altarEntity;
    

    [Header("👁️ Boss 实时视觉元素")]
    public TextMeshProUGUI bossStateText;      // 实时状态文本
    public Slider bossHealthSlider;           // 🌟 顺手帮你把 Boss 自身的血条也接上！

    [Header("🦋 蝴蝶祭坛 实时视觉元素")]
    public Slider altarDurabilitySlider;       // 祭坛耐久值血条
    public TextMeshProUGUI altarDurabilityText; // 耐久数值显示 (如 1000/1000)
    public Slider altarEnergySlider;           // 祭坛能量条
    public TextMeshProUGUI altarEnergyText;     // 能量数值显示 (如 240/400)
    public TextMeshProUGUI altarPhaseText;      // 实时阶段名称显示

    // 🌟 全局唯一入口，谁都可以通过 BossFightUIManager.Instance 找到我
    public static BossFightUIManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }
    private void Start()
    {
        // 🌟 游戏开局或重置时，强制从实体身上抽取一次数值，防止开局空条
        InitializeUI();
    }

    private void OnEnable()
    {
        // 🌟 核心：挂上祭坛的事件订阅线
        if (altarEntity != null)
        {
            altarEntity.OnDurabilityChanged += UpdateAltarDurabilityUI;
            altarEntity.OnEnergyChanged += UpdateAltarEnergyUI;
            altarEntity.OnPhaseUpgraded += UpdateAltarPhaseUI;
            // 🌟 2. 跨越断层：开局立刻强制手动拉取一次祭坛当前的真实数据，确保黑屏出来 UI 就是对的！
            UpdateAltarDurabilityUI(altarEntity.currentDurability, altarEntity.maxDurability);
            UpdateAltarEnergyUI(altarEntity.currentEnergy, altarEntity.maxEnergy);
            UpdateAltarPhaseUI(altarEntity.currentPhase);

        }

       

        // 🌟 核心：挂上 Boss 的事件订阅线
        if (bossEntity != null)
        {
            bossEntity.OnStateChanged += UpdateBossStateUI;
        }
    }

    private void OnDisable()
    {
        // 🌟 防御性编程：注销所有订阅，100% 杜绝切换场景时的内存泄漏
        if (altarEntity != null)
        {
            altarEntity.OnDurabilityChanged -= UpdateAltarDurabilityUI;
            altarEntity.OnEnergyChanged -= UpdateAltarEnergyUI;
            altarEntity.OnPhaseUpgraded -= UpdateAltarPhaseUI;
           
        }
        

        if (bossEntity != null)
        {
            bossEntity.OnStateChanged -= UpdateBossStateUI;
        }
    }

    private void Update()
    {
        // 🌟 顺手把 Boss 自身的实时血量用传统方法更新（CharacterHealth 暂时没写广播时可用）
        if (bossEntity != null && bossHealthSlider != null)
        {
            bossHealthSlider.maxValue = bossEntity.MaxHealth;
            bossHealthSlider.value = bossEntity.CurrentHealth;
        }
    }

    // ==============================================================
    // 📢 表现层接收到广播后的通用 UI 渲染函数
    // ==============================================================

    // A. 响应祭坛耐久广播
    private void UpdateAltarDurabilityUI(int curDur, int maxDur)
    {
        if (altarDurabilitySlider != null)
        {
            altarDurabilitySlider.maxValue = maxDur;
            altarDurabilitySlider.value = curDur;
        }
        if (altarDurabilityText != null)
        {
            altarDurabilityText.text = $"Altar Durability: {curDur} / {maxDur}";
        }
    }

    // B. 响应祭坛能量广播
    private void UpdateAltarEnergyUI(int curEnergy, int maxEnergy)
    {
        if (altarEnergySlider != null)
        {
            altarEnergySlider.maxValue = maxEnergy;
            altarEnergySlider.value = curEnergy;
        }
        if (altarEnergyText != null)
        {
            altarEnergyText.text = $"Altar Energy: {curEnergy} / {maxEnergy}";
        }
    }

    // C. 响应祭坛阶段演进广播
    private void UpdateAltarPhaseUI(int currentPhaseIndex)
    {
        if (altarPhaseText != null && altarEntity != null)
        {
            
            altarPhaseText.text = $"Altar: Phase {currentPhaseIndex})";
        }
    }

    // D. 响应 Boss 行为状态切换广播
    private void UpdateBossStateUI(EyeBossController.BossState newState)
    {
        if (bossStateText == null) return;

        // 根据深渊眼球的不同枚举状态，翻译成恐怖而沉浸的游戏内中文字幕
        switch (newState)
        {
            case EyeBossController.BossState.Calm:
                bossStateText.text = "EvilEye: <color=green>Calm...</color>";
                break;
            case EyeBossController.BossState.SingleLaser:
                bossStateText.text = "EvilEye: <color=yellow>SingleLaser</color>";
                break;
            case EyeBossController.BossState.CircleLaser:
                bossStateText.text = "EvilEye: <color=orange>CircleLaser</color>";
                break;
            case EyeBossController.BossState.MildAnger:
                bossStateText.text = "EvilEye: <color=#FF00FF>MildAnger</color>";
                break;
            case EyeBossController.BossState.Enrage:
                bossStateText.text = "EvilEye: <color=red>Enrage</color>";
                break;
        }
    }

    // 初始化/回档重置时使用
    private void InitializeUI()
    {
        if (altarEntity != null)
        {
            UpdateAltarDurabilityUI(altarEntity.currentDurability, altarEntity.maxDurability);
            UpdateAltarEnergyUI(altarEntity.currentEnergy, altarEntity.maxEnergy);
            // 默认初始为0阶段
            UpdateAltarPhaseUI(0);
        }
        if (bossEntity != null)
        {
            UpdateBossStateUI(bossEntity.currentState);
        }
    }
}