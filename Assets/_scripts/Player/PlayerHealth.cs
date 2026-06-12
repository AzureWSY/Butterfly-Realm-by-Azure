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

    // 视觉表现组件引用
    private PlayerGlowController glowController;

    // ==========================================
    // 👑 核心魔法：重写当前血量属性
    // ==========================================
    public override int CurrentHealth
    {
        get
        {
            // 读血时，直接去读 SO 文件里的跨场景血量
            if (playerStats == null) return base.CurrentHealth;
            return playerStats.currentHealth;
        }
        set
        {
            if (playerStats != null)
            {
                // 1. 将变化后的血量存入 SO 硬盘文件中 (限制在0和最大血量之间)
                playerStats.currentHealth = Mathf.Clamp(value, 0, MaxHealth);

                // 2. 极其巧妙的一步：调用 base.CurrentHealth 
                // 这样能自动触发基类 CharacterHealth 里的 OnHealthChanged 广播，让 UI 血条更新！
                base.CurrentHealth = playerStats.currentHealth;
            }
            else
            {
                base.CurrentHealth = value; // 防呆设计：如果没拖拽SO文件，就用老方法
            }
        }
    }

    // 重写最大血量，指向 SO
    public override int MaxHealth => playerStats != null ? playerStats.maxHealth : base.MaxHealth;

    // ==========================================

    protected override void Start()
    {
        glowController = GetComponent<PlayerGlowController>();

        // 注意：这里我们【不再】重置血量为满血！
        // 这样当你从场景A进入场景B时，读取到的依旧是 SO 里残留的血量（残血状态）。
        // 仅仅发一次广播，让新场景的 UI 血条知道该显示多少血。
        if (playerStats != null)
        {
            base.CurrentHealth = playerStats.currentHealth;
        }
    }

    public override void Heal(int amount)
    {
        base.Heal(amount); // 调用基类方法，它会自动执行 CurrentHealth += amount，并触发上面的 set 魔法
        absoluteSafeTime = Time.time + 0.2f;
    }

    public override void TakeDamage(int damage)
    {
        // 1. 在绝对保护期内或无敌期内，无视伤害
        if (Time.time < absoluteSafeTime || invincibilityTimer > 0) return;

        // 预判扣血后是否能存活 (读取重写后的 CurrentHealth)
        bool willSurvive = (CurrentHealth - damage) > 0;

        // 2. 调用基类的扣血逻辑 (会自动发广播、判断死亡)
        base.TakeDamage(damage);

        if (willSurvive)
        {
            // 3. 读取数据源里的无敌时间配置
            invincibilityTimer = playerStats != null ? playerStats.invincibilityDuration : 1f;

            // 4. 通知控制器播放受击动画
            if (glowController != null)
            {
                glowController.PlayDamageFlash();
            }
        }
    }

    void Update()
    {
        // 只有倒计时，极其干净
        if (invincibilityTimer > 0)
        {
            invincibilityTimer -= Time.deltaTime;
        }
    }

    protected override void Die()
    {
        absoluteSafeTime = Time.time + 0.1f;
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

        // 调用基类的 Die() (会输出 Log)
        base.Die();

        // 发出玩家死亡独有的广播，通知 GameManager 或 结算 UI
        OnPlayerDeath?.Invoke();
    }

    // 强制处决方法（专治各种死亡地板，无视任何无敌帧和保护期）
    public void InstantKill()
    {
        CurrentHealth = 0; // 直接给重写的属性赋值 0，立刻触发 UI 归零广播

        invincibilityTimer = 0f;
        absoluteSafeTime = 0f;

        Die();

        Debug.Log("玩家触发即死机制，无视所有无敌状态，强制处决！");
    }
}