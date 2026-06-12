using UnityEngine;

[CreateAssetMenu(menuName = "ButterflyRealm/Effects/Death")]
public class DeathEffectSO : FloorEffectSO
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public override void Execute(PlayerFloorInteraction player, AttributeFloor floor)
    {
        floor.PlayFloorFeedback();
        player.HealthSystem.InstantKill();
        Debug.Log($"[Êý¾ÝÇý¶¯]: ËÀÍö");
    }
}
