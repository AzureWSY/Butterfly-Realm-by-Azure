using UnityEngine;

public class ParallaxLayer : MonoBehaviour
{
    [Header("核心引用")]
    [Tooltip("拖入主摄像机 (如果留空则自动获取 MainCamera)")]
    public Transform cam;

    [Header("视差深度配置")]
    [Tooltip("0 = 像前景一样正常移动 (近景)\n1 = 完全锁定跟随摄像机 (无限远景)")]
    [Range(0f, 1f)] public float parallaxFactorX = 0.5f;

    [Tooltip("Y轴通常比X轴系数大一点，防止跳跃时看到背景穿帮")]
    [Range(0f, 1f)] public float parallaxFactorY = 0.8f;

    private Vector3 startPosition;
    private Vector3 camStartPos;

    void Start()
    {
        // 如果没手动拖拽摄像机，就自动寻找
        if (cam == null) cam = Camera.main.transform;

        // 记录游戏刚开始时，背景和摄像机的绝对初始坐标
        startPosition = transform.position;
        camStartPos = cam.position;
    }

    // 【极其关键】必须使用 LateUpdate 而不是 Update 或 FixedUpdate
    // 这是为了确保摄像机（如果用 Cinemachine）移动完之后，背景再移动，绝对防止画面抖动！
    void LateUpdate()
    {
        if (cam == null) return;

        // 1. 计算出摄像机从出生点到现在，一共走了多远
        float camDistanceX = cam.position.x - camStartPos.x;
        float camDistanceY = cam.position.y - camStartPos.y;

        // 2. 根据视差系数，计算出背景“应该跟过去多少”
        float bgMoveX = camDistanceX * parallaxFactorX;
        float bgMoveY = camDistanceY * parallaxFactorY;

        // 3. 把背景放到全新的位置上
        transform.position = new Vector3(startPosition.x + bgMoveX, startPosition.y + bgMoveY, transform.position.z);
    }
}