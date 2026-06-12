using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class AttributeFloor : MonoBehaviour
{
    // O(1) 全局高性能静态哈希名册

    public static readonly HashSet<AttributeFloor> ActiveEnergyFloors = new HashSet<AttributeFloor>();

    [Header("🌟 核心功能资产配置")]
    public FloorEffectSO effectConfig;
    public Collider2D interactionTrigger;

    [Header("⚙️ 变体表现配置（利用预制体重写Overrides控制画风）")]
    [SerializeField] private Color feedbackColor = Color.white;
    [SerializeField] private float intensityScale = 2.0f;
    [HideInInspector] public bool isConsumed = false;
    // 🌟 核心防御红线：2D 物理环境绝缘保护闸（解决死后瞬移、全图盲目爆亮的真凶）
    private bool isPhysicsGuarded = false;
    private FloorVisualBridge visualBridge;
    // 🌟 供图纸调用的安全通行证：如果正在复活隔离期，或者计时品已被吃掉，图纸直接拒绝执行
    public bool IsInteractable => !isConsumed && !isPhysicsGuarded;
    private void Awake()
    {
        visualBridge = GetComponentInChildren<FloorVisualBridge>();
    }
    private void Start()
    {
        if (interactionTrigger == null) interactionTrigger = GetComponent<Collider2D>();
        if (interactionTrigger != null) interactionTrigger.isTrigger = true; // 强制锁死为 Trigger
        if (visualBridge != null) visualBridge.HideClock();
    }
    private void OnEnable()
    {
        if (effectConfig != null && effectConfig is ButterflyEnergyFloorSO)
        {
            ActiveEnergyFloors.Add(this);
        }
    }
    private void OnDisable()
    {
        ActiveEnergyFloors.Remove(this);
    }
    
    //与可交互物体发生交互
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 不管进来的是玩家还是被抛掷的物体，只要实现了接口，一律开闸放行！
        if (other.TryGetComponent<IFloorInteractable>(out var entity))
        {
            entity.OnEnterFloor(this);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.TryGetComponent<IFloorInteractable>(out var entity))
        {
            entity.OnExitFloor(this);
        }
    }

    // ==============================================================
    // 🌟🌟🌟 原子级解耦接口：由各个 ScriptableObject 图纸自主调用 🌟🌟🌟
    // ==============================================================
    /// <summary>
    /// 原子接口一：无条件引爆视觉反馈效果（任何地板只要在Execute里调用，一瞬爆发并平滑暗淡）
    /// </summary>
    public void PlayFloorFeedback()
    {
        if (visualBridge != null && effectConfig != null)
        {
            // 顺着图纸配置（canRecover），告诉表现层是该“渐变后隐形”还是“渐变后褪色回归原貌”
            visualBridge.PlayJuicyFeedback(feedbackColor, intensityScale, effectConfig.isOneTimeUse && !effectConfig.canRecover);
        }
    }

    /// <summary>
    /// 原子接口二：单独执行回收消耗与关闭物理的大门逻辑
    /// </summary>

    public void Consume() //即消耗或进入冷却
    {
        if (isConsumed) return;
        isConsumed = true;
        if (interactionTrigger != null) interactionTrigger.enabled = false; // 关闭碰撞门
        if (effectConfig != null && effectConfig.canRecover && effectConfig.isOneTimeUse)
        {
            StartCoroutine(RecoverRoutine(effectConfig.recoverTime));
        }
    }
    // ==============================================================
    // 🌟🌟🌟 时序核心：大导演全局复位（彻底绝杀重置时无故爆亮、瞎变色Bug） 🌟🌟🌟
    // ==============================================================
    public void ResetFloorForBossFight()
    {
        StopAllCoroutines(); // 1. 强制掐断当前正在跑的任何逻辑倒计时表
        StartCoroutine(ResetPhysicsGuardRoutine()); // 2. 扔给时间绝缘防灾线程
    }

    private IEnumerator ResetPhysicsGuardRoutine()
    {
        isPhysicsGuarded = true; // 1. 【合上电闸】：强制地板化身绝缘体，让图纸的 early return 拦截生效
        isConsumed = false;
        // 2. 强行把表现层的一切残余变色和光照过载扼杀，数据无条件退回出厂原始基准值
        if (visualBridge != null)
        {
            visualBridge.ResetVisualsToDefault();
            visualBridge.HideClock();
        }
        if (interactionTrigger != null) interactionTrigger.enabled = true; // 触发器开门重新待命
        // 3. 【强行绝缘 0.25 秒】：彻底阻断由于玩家死亡位置突变、物理引擎空间离散判定的“位移扫街”假碰撞回调！
        yield return new WaitForSeconds(0.25f);
        isPhysicsGuarded = false; // 4. 【安全开闸】：空间已被清洗干净，完美迎接战斗
        if (effectConfig != null && effectConfig is ButterflyEnergyFloorSO)
        {
            ActiveEnergyFloors.Add(this);
        }
    }
    private IEnumerator RecoverRoutine(float waitTime)
    {
        float elapsed = 0f;
        if (visualBridge != null) visualBridge.ShowClock();
        while (elapsed < waitTime)
        {
            elapsed += Time.deltaTime;
            if (visualBridge != null) visualBridge.UpdateClockFill(elapsed / waitTime);
            yield return null;
        }
        if (visualBridge != null) visualBridge.HideClock();
        ResetFloorAfterCooldown();
    }
    private void ResetFloorAfterCooldown()
    {
        isConsumed = false;
        if (visualBridge != null) visualBridge.ResetVisualsToDefault();
        if (interactionTrigger != null) interactionTrigger.enabled = true;
        if (effectConfig != null && effectConfig is ButterflyEnergyFloorSO)
        {
            ActiveEnergyFloors.Add(this);
        }

    }
    public void PermanentDestroy()
    {
        if (isConsumed) return;
        isConsumed = true;
        if (visualBridge != null)
        {
            visualBridge.SetVisualsActive(false);
            visualBridge.HideClock();
            if (visualBridge.MainRenderer != null) visualBridge.MainRenderer.enabled = false;
        }
        if (interactionTrigger != null) interactionTrigger.enabled = false;
        ActiveEnergyFloors.Remove(this);
        StopAllCoroutines();
    }
    public void ForceReset() => ResetFloorForBossFight();

}