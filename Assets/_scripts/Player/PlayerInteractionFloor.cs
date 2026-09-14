using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

// 🌟 核心进化：不再强制依赖Health和Control，玩家身体功能组件只依赖唯一枢纽中心！
[RequireComponent(typeof(PlayerHub))]
public class PlayerFloorInteraction : MonoBehaviour, IFloorInteractable
{
    // 🌟 引入中介者：只认大管家枢纽
    public PlayerHub Hub { get; private set; }

    // 🚀 降维打击：保持对外只读属性不变（不破坏任何已有的 FloorEffectSO 图纸代码！）
    // 但是底层全部重定向，实时向 Hub 索要绝对真理引用，消灭内存冗余与独立抓取开销！
    public PlayerHealth HealthSystem => Hub.Health;
    public Playercontrol PlayerControl => Hub.Control;
    public PlayerEnergySystem PlayerEnergy => Hub.Energy;
    public Rigidbody2D rb => Hub.Rb;
    
    // 👑 运行时缓存：只盯着上一次踩中的那一块具体地砖
    private AttributeFloor lastTriggeredFloor;
    private float lastTriggerTime;
    

    private void Awake()
    {
        // 🌟 灵魂净化：全网只抓这一个核心中介者，全盘瞬间打通！
        Hub = GetComponent<PlayerHub>();
    }

    // ================== 纯净的地板进入与离开契约 ==================

    public void OnEnterFloor(AttributeFloor floor)
    {
        // 1. 死亡防御线
        if (HealthSystem == null || HealthSystem.IsDead) return;

        // 2. 👑 绝杀手感粘滞 Bug：精准去重锁
        // 只有当玩家在 0.1 秒内连续碰撞【同一块地砖实例】时，才判定为物理多重碰撞体抖动，进行熔断拦截。
        // 如果碰到了【不同的相邻地砖】(floor != lastTriggeredFloor)，直接零延迟秒级放行，保证跑酷手感绝对丝滑！
        if (floor == lastTriggeredFloor && Time.time - lastTriggerTime < 0.1f)
        {
            return;
        }

        // 3. 伤害地板无敌期拦截 (顺应上一轮的多态解耦设计)
        if (floor.effectConfig != null && floor.effectConfig.CanBeImmunedByInvincibility && HealthSystem.IsInvincible)
        {
            return;
        }

        // 4. 绝对放行安全区：正式执行效果
        if (floor.IsInteractable && floor.effectConfig != null)
        {
            // 👑 运行时刷新：记录本次触发的绝对真理地砖和时间
            lastTriggeredFloor = floor;
            lastTriggerTime = Time.time;

            // 执行多态图纸逻辑
            floor.effectConfig.Execute(this, floor);
        }
    }

    public void OnExitFloor(AttributeFloor floor)
    {
        if (HealthSystem == null || HealthSystem.IsDead) return;
        floor.effectConfig?.ExecuteExit(this, floor);
    }

    // ================== 提供给图纸调用的公共方法 ==================

    public void StartKnockback(Vector2 force) => StartCoroutine(KnockbackRoutine(force));

    // ================== 异步协程 (通过重定向后的组件丝滑运行) ==================

    private IEnumerator KnockbackRoutine(Vector2 force)
    {
        if (PlayerControl == null || rb == null) yield break;

        PlayerControl.SetMovementPermission(false);
        rb.linearVelocity = Vector2.zero; // 使用重定向后的 rb 指针，完美清除冲量
        rb.AddForce(force, ForceMode2D.Impulse);

        yield return new WaitForSeconds(0.3f);

        PlayerControl.SetMovementPermission(true);
    }
}