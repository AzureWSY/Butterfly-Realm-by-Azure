using UnityEngine;

[CreateAssetMenu(menuName = "ButterflyRealm/FloorEffects/VisionExpansion")]
public class VisionExpansionSO : FloorEffectSO
{
    [Header("视野特有设置")]
    public float expandedSize = 10f;
    public float duration = 5f;

    public override void Execute(PlayerFloorInteraction player, AttributeFloor floor)
    {
        if (floor.isConsumed) return;
        floor.PlayFloorFeedback();

        // 🌟 核心魔法：让玩家的 Buff管理器 塞入一个“视野Buff”
        PlayerBuffManager buffManager = player.GetComponent<PlayerBuffManager>();
        if (buffManager != null)
        {
            buffManager.AddBuff(new VisionExpansionBuff(duration, expandedSize));
        }

        if (isOneTimeUse) floor.Consume();
    }
}

// =======================================================
// 具体的视野扩增 Buff 逻辑（全部封装在这里面）
// =======================================================
public class VisionExpansionBuff : PlayerBuff
{
    private float duration;
    private float expandedSize;
    private float timer;
    private bool isActive = false;

    // 构造函数：接收图纸传来的数据
    public VisionExpansionBuff(float duration, float expandedSize)
    {
        this.duration = duration;
        this.expandedSize = expandedSize;
        this.timer = duration;
    }

    public override void OnApply()
    {
        Debug.Log($"获得视野扩增！持续时间 {duration}s，按 P 键切换！");
    }

    public override void OnTick()
    {
        // 监听玩家按 P 键
        if (Input.GetKeyDown(KeyCode.P))
        {
            isActive = !isActive;
            if (isActive) CameraManager.Instance.SetCameraSize(expandedSize);
            else CameraManager.Instance.ResetCameraSize();
        }

        // 如果目前是扩增状态，就扣除倒计时
        if (isActive)
        {
            timer -= Time.deltaTime;
            if (timer <= 0)
            {
                IsFinished = true; // 时间到了，标记为结束，Manager会自动清除它
            }
        }
    }

    public override void OnRemove()
    {
        // 不管是时间到了，还是玩家死了，只要 Buff 拔除，必须恢复摄像机！
        CameraManager.Instance.ResetCameraSize();
        Debug.Log("视野扩增权限已失效或被回收！");
    }
}