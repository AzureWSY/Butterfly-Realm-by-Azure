using UnityEngine;

[CreateAssetMenu(menuName = "ButterflyRealm/Effects/JumpHigh")]
public class JumpHighEffectSO : FloorEffectSO
{
    public override void Execute(PlayerFloorInteraction player, AttributeFloor floor)
    {
        floor.PlayFloorFeedback();
        if (player.PlayerControl != null)
        {
            player.PlayerControl.canSuperJump = true;
            Debug.Log("进入猛跳地板，已赋予猛冲权限");
        }
    }

    // 🌟 重写离开方法：收回权限！
    public override void ExecuteExit(PlayerFloorInteraction player, AttributeFloor floor)
    {
        if (player.PlayerControl != null)
        {
            player.PlayerControl.canSuperJump = false;
            Debug.Log("离开猛跳地板，已收回猛冲权限");
        }
    }
}