using UnityEngine;
using System;

public class PlayerHealth : CharacterHealth
{
    public event Action OnPlayerDeath;

    [Header("🌟 玩家数据源 (请将 PlayerStatsData 拖入此处)")]
    public PlayerStatsData playerStats;

    // 状态控制 (纯粹的运行时逻辑，不存入SO)
    private float invincibilityTimer = 0f;
    private float absoluteSafeTime = 0f;
    private bool isDead = false; // 👑 新增死亡状态锁：彻底杜绝重复死亡/鞭尸Bug

    // 视觉表现组件引用
    private PlayerGlowController glowController;

    // 👑 暴露给外部（如交互系统）的只读真理：玩家当前是否处于无敌保护期
    public bool IsInvincible => invincibilityTimer > 0 || Time.time < absoluteSafeTime;
    // 👑 暴露给外部的只读真理：玩家是否已经死亡
    public bool IsDead => isDead;

    // ==========================================
    // 👑 核心魔法：重写当前血量属性
    // ==========================================
    public override int CurrentHealth
    {
        get
        {
            if (playerStats == null) return base.CurrentHealth;
            return playerStats.currentHealth;
        }
        set
        {
            if (playerStats != null)
            {
                // 1. 将变化后的血量存入 SO 文件中 (限制在0和最大血量之间)
                playerStats.currentHealth = Mathf.Clamp(value, 0, MaxHealth);

                // 2. 调用基类属性，自动触发基类里的 OnHealthChanged 广播让 UI 更新
                base.CurrentHealth = playerStats.currentHealth;
            }
            else
            {
                base.CurrentHealth = value; // 防呆设计
            }
        }
    }

    // 重写最大血量，指向 SO
    public override int MaxHealth => playerStats != null ? playerStats.maxHealth : base.MaxHealth;

    // ==========================================

    protected override void Start()
    {
        base.Start(); // 确保执行基类初始化
        isDead = false;
        glowController = GetComponent<PlayerGlowController>();

        // 进入新场景时，读取 SO 里的残留血量并广播给 UI
        if (playerStats != null)
        {
            base.CurrentHealth = playerStats.currentHealth;
        }
    }

    public override void Heal(int amount)
    {
        if (isDead) return; // 死了就别奶了
        base.Heal(amount);
        absoluteSafeTime = Time.time + 0.2f;
    }

    public override void TakeDamage(int damage)
    {
        if (isDead) return; // 如果已经死亡，直接拦截所有后续伤害

        // 在绝对保护期内或无敌期内，无视伤害
        if (Time.time < absoluteSafeTime || invincibilityTimer > 0) return;

        // 预判扣血后是否能存活
        bool willSurvive = (CurrentHealth - damage) > 0;

        base.TakeDamage(damage);

        if (willSurvive)
        {
            // 读取数据源里的无敌时间配置
            invincibilityTimer = playerStats != null ? playerStats.invincibilityDuration : 1f;

            // 通知控制器播放受击动画
            if (glowController != null)
            {
                glowController.PlayDamageFlash();
            }
        }
    }

    void Update()
    {
        if (isDead) return;

        // 无敌时间倒计时
        if (invincibilityTimer > 0)
        {
            invincibilityTimer -= Time.deltaTime;
        }
    }

    protected override void Die()
    {
        if (isDead) return; // 状态锁拦截，防止被多次调用
        isDead = true;

        absoluteSafeTime = Time.time + 0.1f; // 正常死亡时的微小保护，防止当前帧被连续鞭尸
        invincibilityTimer = 0f;

        // 死亡时，通知视觉层关闭所有特效
        if (glowController != null)
        {
            glowController.TurnOffAllGlows();
        }

        StopAllCoroutines();

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        base.Die();

        // 发出玩家死亡独有的广播
        OnPlayerDeath?.Invoke();
    }

    // 👑 强制处决方法（专治各种死亡地板，无视任何无敌帧和保护期）
    public void InstantKill()
    {
        if (isDead) return; // 如果已经死了，不再重复执行

        Debug.Log("⚠️ 玩家触发即死机制，开始强制处决...");

        // 1. 必须【最先】将无敌时间和保护时间绝对清零
        invincibilityTimer = 0f;
        absoluteSafeTime = 0f;

        // 2. 强制将血量清零，瞬间触发 UI 归零广播
        CurrentHealth = 0;

        // 3. 调用死亡逻辑
        Die();

        // 4. 强行覆盖掉 Die() 里面加上的 0.1f 保护时间，确保绝对为 0
        absoluteSafeTime = 0f;
    }

    // 👑 🌟 核心救命接口：专门供给 PlayerHub 在玩家复活那一个帧调用！
    public void ResetStatusOnRespawn()
    {
        Debug.Log("<color=green>🔄 [血量系统] 玩家复活，全盘重置状态锁与计时器！</color>");

        isDead = false;             // 1. 核心解闸！玩家重新活过来
        invincibilityTimer = 0f;    // 2. 清空残留的受击无敌
        absoluteSafeTime = Time.time + 0.5f; // 3. 极其专业的微小保护期：复活时给 0.5 秒绝对保护，防止复活点有怪当场暴毙

        // 4. 血量拉满（顺便自动触发 UI 刷新广播）
        CurrentHealth = MaxHealth;
    }
}