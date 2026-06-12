using UnityEngine;

public class ButterflyEyeTracker : MonoBehaviour
{
    [Header("追踪目标")]
    public Transform player;

    [Header("平移限制")]
    [Tooltip("眼球在透明坑位里能够平移的最大半径")]
    public float moveRadius = 0.2f;

    private Vector3 initialLocalPos;

    private void Start()
    {
        // 记录在坑洞中的初始中心点
        initialLocalPos = transform.localPosition;
    }

    private void Update()
    {
        if (player == null) return;

        // 算方向
        Vector3 dir = (player.position - transform.position).normalized;

        // 【平移】在局部坐标下，围绕透明坑的中心点进行平移，实现真正的 3D 错觉
        transform.localPosition = initialLocalPos + dir * moveRadius;

        // 【旋转】始终让顶部对准玩家 (如果方向反了，修改 -90f)
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle + 90f);
    }
}