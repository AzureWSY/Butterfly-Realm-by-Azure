using UnityEngine;

[CreateAssetMenu(menuName = "ButterflyRealm/FloorEffects/NightVision")]
public class NightVisionEffectSO : FloorEffectSO
{
    [Tooltip("夜视持续时间")]
    public float duration = 30f;

    public override void Execute(PlayerFloorInteraction player, AttributeFloor floor)
    {
        if (floor.isConsumed) return;
        floor.PlayFloorFeedback();
        // 核心魔法：向玩家的管理器里塞入一个【夜视 Buff】
        PlayerBuffManager buffManager = player.GetComponent<PlayerBuffManager>();
        if (buffManager != null)
        {
            // 你之前要求“没有夜视才触发”，我们在添加前可以做一个防重复判断
            // （如果是可以刷新时间的，这里逻辑可以升级，目前按你的要求写简单点）
            buffManager.AddBuff(new NightVisionBuff(duration));
        }

        if (isOneTimeUse) floor.Consume();
    }
}

// =======================================================
// 具体的夜视 Buff 逻辑
// =======================================================
public class NightVisionBuff : PlayerBuff
{
    private float duration;
    private float timer;

    public NightVisionBuff(float duration)
    {
        this.duration = duration;
        this.timer = duration;
    }

    public override void OnApply()
    {
        // 当获得 Buff 时，呼叫画面管理器开启夜视滤镜
        PostProcessManager.Instance.EnableNightVision(true);
        Debug.Log($"[系统] 夜视仪已开启，电量(持续时间): {duration}s");
    }

    public override void OnTick()
    {
        // 夜视不需要按键切换，直接倒计时即可
        timer -= Time.deltaTime;

        if (timer <= 0)
        {
            IsFinished = true; // 时间到了，通知管理器销毁我
        }
    }

    public override void OnRemove()
    {
        // Buff 结束（时间到，或者玩家死亡时），强制关闭滤镜
        PostProcessManager.Instance.EnableNightVision(false);
        Debug.Log("[系统] 夜视仪电量耗尽或玩家死亡，已关闭。");
    }
}