using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(menuName = "ButterflyRealm/Effects/Impact")]
public class ImpactEffectSO : FloorEffectSO
{
    [Header("冲击地板专属配置")]
    [Tooltip("原本的 LowDamageValue")]
    public int damageAmount = 5;

    public float pushForce = 15f;
    public Vector2 pushDirection = new Vector2(-1f, 1f); // 默认左上


    public override void Execute(PlayerFloorInteraction player, AttributeFloor floor)
    {
        if (floor.isConsumed) { return; }
        

        // 1. 死亡预判
        if (player.HealthSystem.CurrentHealth <= damageAmount)
        {
            Debug.Log("<color=cyan>预判玩家必死，直接执行死亡，拦截所有冲击力！</color>");
            player.HealthSystem.TakeDamage(damageAmount);
            return;
        }

        // 2. 空间快照
        Vector3 playerSnapshot = player.transform.position;
        Vector3 floorSnapshot = floor.transform.position;

        float dx = playerSnapshot.x - floorSnapshot.x;
        float dy = playerSnapshot.y - floorSnapshot.y;

        Vector2 finalForce;

        // 当前配置方向
        Vector2 diagonalDirection = pushDirection.normalized;

        // true = 朝右
        // false = 朝左
        bool pushingRight = pushDirection.x > 0f;

        // 3. 防穿模判定
        if (Mathf.Abs(dx) > 0.65f && dy < 0.2f)
        {
            bool shouldPushUpOnly = pushingRight ? dx < 0f : dx > 0f;  // 左上地板：玩家在右边，右上地板：玩家在左边

            finalForce = shouldPushUpOnly ? (new Vector2(0,pushDirection.y)) * pushForce * 0.75f : diagonalDirection * pushForce;
        }
        else
        {
            finalForce = diagonalDirection * pushForce;
        }

        floor.PlayFloorFeedback();
        // 4. 扣血 + 击飞
        player.HealthSystem.TakeDamage(damageAmount);
        player.StartKnockback(finalForce);

        Vector2 playerPreVelocity = player.rb.linearVelocity;

        IImpactSignalRecevier recevier = floor.GetComponentInParent<IImpactSignalRecevier>();

        if (recevier != null) recevier.OnImpact(playerPreVelocity);

        if (isOneTimeUse) floor.Consume();

    }
}