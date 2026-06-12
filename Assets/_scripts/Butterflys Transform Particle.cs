using UnityEngine;
using System.Collections;

public class SwarmPathFollower : MonoBehaviour
{
   
    [Header("路径设置")]
    public Transform p0, p1, p2, p3;

    [Header("飞行参数")]
    public float speed = 5.0f;       // 【修改点1】改为按真实速度控制

    [Header("表现控制")]
    private ParticleSystem pSystem;
    public AnimationCurve speedCurve; // 【进阶】如果想让起步慢、中间快，可以通过曲线控制 t

    void Awake()
    {
        if (pSystem == null) pSystem = GetComponent<ParticleSystem>();
    }

    public void StartFlying()
    {
        StartCoroutine(FollowPathRoutine());
    }

    // 接收机关传来的 4 个控制点
    public void SetPath(Transform start, Transform control1, Transform control2, Transform end)
    {
        p0 = start; p1 = control1; p2 = control2; p3 = end;
        StartFlying();
    }

    private IEnumerator FollowPathRoutine()
    {
        float estimatedLength = EstimateBezierLength(20);
        float duration = estimatedLength / speed;
        float elapsedTime = 0;

        // 【新增 1】获取发射模块，记录下你设置的初始发射频率（比如 10）
        var emission = pSystem.emission;
        float originalRate = emission.rateOverTime.constant;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;

            // 基础的线性进度 (0 到 1)
            float rawT = elapsedTime / duration;

            // 【新增 2：运动平滑】使用 Unity 内置的 SmoothStep 实现“丝滑起步与刹车”
            // 如果你画了 speedCurve，优先用你的曲线；否则用自带的完美平滑曲线
            float smoothT = Mathf.SmoothStep(0f, 1f, rawT);
            if (speedCurve != null && speedCurve.keys.Length > 0) smoothT = speedCurve.Evaluate(rawT);

            transform.position = CalculateBezierPoint(smoothT, p0.position, p1.position, p2.position, p3.position);

            // 【新增 3：发射衰减】当路程走到最后 20% 时，开始让蝴蝶变得稀疏
            if (rawT > 0.8f)
            {
                // 将进度 0.8~1.0 映射为比例 1.0~0.0
                float fadeRatio = 1f - ((rawT - 0.8f) / 0.2f);
                // 动态修改发射率，慢慢降到 0
                emission.rateOverTime = originalRate * fadeRatio;
            }

            yield return null;
        }

        // 运动彻底结束，确保彻底停止发射
        pSystem.Stop();

        // 停留并等待最后的蝴蝶全部淡出消失后，再销毁物体
        Destroy(gameObject, 3f);
    }

    // 【核心算法】将曲线切成 segments 段直线，累加求出近似总长度
    private float EstimateBezierLength(int segments)
    {
        float length = 0f;
        Vector3 previousPoint = p0.position;

        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            Vector3 currentPoint = CalculateBezierPoint(t, p0.position, p1.position, p2.position, p3.position);
            length += Vector3.Distance(previousPoint, currentPoint);
            previousPoint = currentPoint;
        }
        return length;
    }

    // 贝塞尔插值公式
    private Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;

        Vector3 p = uuu * p0;
        p += 3 * uu * t * p1;
        p += 3 * u * tt * p2;
        p += ttt * p3;
        return p;
    }
}