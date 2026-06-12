using UnityEngine;

public class FragileBlock : MonoBehaviour
{
    [Header("组件引用")]
    [Tooltip("方块的实体碰撞体（非 Trigger）")]
    public BoxCollider2D solidCollider;

    [Header("特效")]
    public GameObject breakEffect;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 【核心优化】：TryGetComponent 一步到位
        // 如果碰到我的物体身上有 Playercontrol 脚本，就把它提取出来放到 pc 变量里
        if (other.TryGetComponent<Playercontrol>(out Playercontrol pc))
        {
            // 【架构胜利】：状态驱动！
            // 不再查户口测速度，只要你身上有“猛冲无敌”的 Buff，直接碎！
            if (pc.isSuperJumping)
            {
                DoBreak();
            }
        }
    }

    private void DoBreak()
    {
        // 先关掉实体碰撞体，让玩家无阻力穿过，保持丝滑
        if (solidCollider != null)
        {
            solidCollider.enabled = false;
        }

        // 播放碎裂特效
        if (breakEffect != null)
        {
            Instantiate(breakEffect, transform.position, Quaternion.identity);
        }

        // 销毁自身
        Destroy(gameObject);
    }
}