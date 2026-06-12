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
    private Rigidbody2D rb => Hub.Rb;
    

    private float lastTriggerTime;

    private void Awake()
    {
        // 🌟 灵魂净化：全网只抓这一个核心中介者，全盘瞬间打通！
        Hub = GetComponent<PlayerHub>();
    }

    // ================== 纯净的地板进入与离开契约 ==================

    public void OnEnterFloor(AttributeFloor floor)
    {
        // 顺着 Hub 转发判空防御线（死人踩踏直接熔断）
        if (HealthSystem == null || HealthSystem.CurrentHealth <= 0 || Time.time - lastTriggerTime < 0.1f) return;

        if (floor.IsInteractable && floor.effectConfig != null)
        {
            lastTriggerTime = Time.time;
            // 执行多态图纸逻辑，安全地把自己（this）送过去
            floor.effectConfig.Execute(this, floor);
        }
    }

    public void OnExitFloor(AttributeFloor floor)
    {
        if (HealthSystem == null || HealthSystem.CurrentHealth <= 0) return;
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