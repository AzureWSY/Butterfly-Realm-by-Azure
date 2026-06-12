using UnityEngine;

// 确保物体上必须有粒子系统组件
[RequireComponent(typeof(ParticleSystem))]
public class EnergyPaticleFollow : MonoBehaviour
{
    [Header("追踪飞弹设置")]
    [Tooltip("刚出生时的速度（建议设小一点，比如 2，制造起步滞空感）")]
    public float currentSpeed = 2f;
    [Tooltip("加速度（每一秒增加的速度，建议设 25 到 40，让它越飞越狂暴）")]
    public float acceleration = 30f;
    [Tooltip("极限速度（必须绝对碾压玩家的最大移速！比如 50）")]
    public float maxSpeed = 50f;

    private Transform target; // 导弹的目标（玩家）
    private float travelSpeed;  // 飞行速度
    private float stopDistance = 0.2f; // 距离目标多近算打中

    [Header("瞄准校准")]
    [Tooltip("目标坐标偏移量（比如填 Y=1，就会飞向玩家脚底往上 1 单位的位置）")]
    public Vector3 targetOffset = new Vector3(0, 1f, 0); // ?? 新增这行！默认往上偏 1 米

    // ?? 核心回调：当粒子打中玩家时，通知外面的系统给玩家加能量！
    public System.Action OnArrival;

    private bool hasArrived = false;
    private ParticleSystem ps;

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
    }

    // ?? 初始化入口：由蝴蝶脚本调用
    public void Setup(Transform endTarget)
    {
        target = endTarget;

        // 确保粒子系统开启，并且是世界坐标系（否则发射器动了，粒子会跟着全家搬迁）
        var main = ps.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        ps.Play();
    }

    void Update()
    {
        if (target == null || hasArrived) return;

        Vector3 realTargetPos = target.position + targetOffset;

        // 【核心魔法：狂暴加速】
        // 每一帧都把速度往上加，直到达到极限速度
        currentSpeed += acceleration * Time.deltaTime;
        if (currentSpeed > maxSpeed)
        {
            currentSpeed = maxSpeed;
        }

        // 用狂暴后的当前速度去 MoveTowards（硬追，绝不减速！）
        transform.position = Vector3.MoveTowards(transform.position, realTargetPos, currentSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, realTargetPos) < stopDistance)
        {
            ArriveAtTarget();
        }
    }

    void ArriveAtTarget()
    {
        if (hasArrived) return; // 防止重复触发
        hasArrived = true;

        // ?? 1. 触发逻辑奖励：给玩家加能量！
        if (OnArrival != null) OnArrival.Invoke();

        // ?? 2. 视觉上的温柔清理
        // 如果直接 Destroy(gameObject)，画面上的粒子会瞬间凭空消失，非常生硬！
        // 优雅的做法是：停止发射新粒子，让现有粒子活完最后零点几秒，然后物体自动销毁。

        var emission = ps.emission;
        emission.enabled = false; // 关掉水龙头

        // 销毁整个物体，延时 X 秒（这个 X 取决于你粒子系统 main 模块里的 Max Lifetime）
        // 假设 Max Lifetime 是 0.5 秒，这里写 0.6 秒就很安全。
        Destroy(gameObject, 0.6f);
    }
}