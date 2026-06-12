using UnityEngine;

[CreateAssetMenu(menuName = "ButterflyRealm/Effects/Heal")]
public class HealEffectSO : FloorEffectSO
{
    public int healAmount = 15;
    public override void Execute(PlayerFloorInteraction player, AttributeFloor floor)
    {
        if (floor.isConsumed) return;
        floor.PlayFloorFeedback();
        player.HealthSystem.Heal(healAmount);
        floor.Consume(); // 通知地板执行“吸干”逻辑
    }
}