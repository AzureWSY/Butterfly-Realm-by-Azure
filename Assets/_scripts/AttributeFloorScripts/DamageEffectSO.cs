using UnityEngine;

[CreateAssetMenu(menuName = "ButterflyRealm/Effects/Damage")]
public class DamageEffectSO : FloorEffectSO
{
    public int damageAmount = 25;
    public override void Execute(PlayerFloorInteraction player, AttributeFloor floor)
    {
        floor.PlayFloorFeedback();
        player.HealthSystem.TakeDamage(damageAmount);
        Debug.Log($"[数据驱动]: 造成伤害 {damageAmount}");
    }
}