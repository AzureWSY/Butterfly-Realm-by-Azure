using UnityEngine;

public class CameraFocus : MonoBehaviour
{
    [Header("必须拖入你的猴子")]
    public Transform player;
    public Rigidbody2D playerRb;

    [Header("神级防晕眩参数")]
    public float lookDownDistance = 2f;     // 【改小】不要4了，2个单位的偏移足够看清脚下，不会太突兀
    public float fallSpeedThreshold = -10f; // 【核心调大】必须设为 -10 左右！过滤掉普通跳跃，只在真正跳崖时触发！
    public float lookDownSpeed = 4f;        // 往下探路的速度（中等）
    public float recoverSpeed = 1f;         // 【灵魂机制】落地后恢复居中的速度，必须极慢！防止画面回弹！

    private float currentYOffset = 0f;

    void LateUpdate()
    {
        if (player == null || playerRb == null) return;

        float targetYOffset = 0f;

        // 只有当真正的“高速坠落”时，才触发探路！(避开了平地跑跳)
        if (playerRb.linearVelocityY < fallSpeedThreshold)
        {
            targetYOffset = -lookDownDistance;
        }

        // 根据状态选用不同的顺滑度：掉下悬崖时看路要快，落地后镜头回正要慢！
        float currentSmooth = (targetYOffset < currentYOffset) ? lookDownSpeed : recoverSpeed;

        currentYOffset = Mathf.Lerp(currentYOffset, targetYOffset, currentSmooth * Time.deltaTime);

        // 绑定替身位置
        transform.position = player.position + new Vector3(0, currentYOffset, 0);
    }
}