using UnityEngine;

[DisallowMultipleComponent]
public class PlayerHub : MonoBehaviour
{
    // 利用 C# 自动属性，对外“只读封装”，严防外部恶意篡改破坏系统
    public PlayerHealth Health { get; private set; }
    public Playercontrol Control { get; private set; }

    public Collider2D Collider {  get; private set; }
    public Rigidbody2D Rb {  get; private set; }
    public PlayerEnergySystem Energy { get; private set; } // 🚀 最新收编：统一管理的能量系统


    private void Awake()
    {
        // 防御性底层扫描：不管零件在根节点还是子节点，Awake时一次性强行捕获并缓存
        Health = GetComponent<PlayerHealth>();
        Control = GetComponent<Playercontrol>();
        Collider = GetComponent<Collider2D>();
        Rb = GetComponent<Rigidbody2D>();
        Energy = GetComponent<PlayerEnergySystem>();

        // 安全气囊：开局断言检查，严防策划漏挂组件
        if (Health == null || Control == null || Collider == null || Rb == null || Energy == null)
        {
            Debug.LogError($"<color=red>【PlayerHub】致命错误：主角身上缺少核心解耦零件！请检查层级！</color>");
        }
    }

    private void Start()
    {
        // 出生即报到：主角一诞生，直接把自己注册给总指挥官，切断所有强耦合
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterPlayerHub(this);
        }
    }
    //对外状态接口

    public Vector3 GetPosition() => (Rb != null) ? (Vector3)Rb.position : transform.position;
    public Vector3 GetBoundsCenter() => (Collider != null) ? Collider.bounds.center : transform.position;
    public bool CanBeTracked => Collider != null && Collider.enabled;

    //对外行为接口
    public void Teleport(Vector3 targetPos)
    {
        if (Rb == null) return;
        Rb.linearVelocity = Vector2.zero;
        Rb.angularVelocity = 0f;

        Rb.position = targetPos;
    }

    public void Respawn(Vector3 respawnPosition)
    {
        if (Rb == null) return;
        if (Control != null) Control.SetMovementPermission(false);



        if (Collider != null) Collider.enabled = false;

        Teleport(respawnPosition);

        if (Collider != null) Collider.enabled = true;

        if (Health != null) Health.Heal(Health.MaxHealth);

        if (Control != null) Control.SetMovementPermission(true);
    }
}
