using UnityEngine;

[CreateAssetMenu(menuName = "ButterflyRealm/Effects/Damage")]
public class DamageEffectSO : FloorEffectSO
{
    public int damageAmount = 25;

    // 👑 伤害地板高调宣称：我产生的伤害效果必须能被无敌期免疫！
    public override bool CanBeImmunedByInvincibility => true;

    public override void Execute(PlayerFloorInteraction player, AttributeFloor floor)
    {
        floor.PlayFloorFeedback();
        player.HealthSystem.TakeDamage(damageAmount);
        Debug.Log($"[数据驱动]: 造成伤害 {damageAmount}");
    }
}