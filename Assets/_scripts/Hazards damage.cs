using UnityEngine;

public class Hazardsdamage : MonoBehaviour
{
    public int damageAmount = 30;

    // 当主角在地刺内部移动或停留时，每帧都会尝试调用
    private void OnTriggerStay2D(Collider2D collision)
    {
        CharacterHealth health = collision.GetComponent<CharacterHealth>();
        if (health != null)
        {
            // 虽然每帧都在调用，但因为 PlayerHealth 内部有 isInvincible 锁
            // 所以实际上只会每隔 1 秒（无敌时间）扣一次血
            health.TakeDamage(damageAmount);
        }
    }
}