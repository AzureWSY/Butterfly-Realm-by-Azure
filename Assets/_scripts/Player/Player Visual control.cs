using UnityEngine;

public class PlayerVisualcontrol : MonoBehaviour
{
    private Animator animator;
    private Rigidbody2D parentRb; // 找父级的刚体
    private Playercontrol parentCtrl; // 找父级的控制脚本

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int YVelocityHash = Animator.StringToHash("yVelocity");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");

    void Start()
    {
        animator = GetComponent<Animator>();
        // 因为脚本现在在子物体上，我们要用 GetComponentInParent 去拿父级的物理数据
        parentRb = GetComponentInParent<Rigidbody2D>();
        parentCtrl = GetComponentInParent<Playercontrol>();
    }

    void Update()
    {
        animator.SetFloat(SpeedHash, Mathf.Abs(parentRb.linearVelocityX));
        animator.SetFloat(YVelocityHash, parentRb.linearVelocityY);
        animator.SetBool(IsGroundedHash, parentCtrl.isGrounded);

        // 核心转身：现在你可以放心地使用 eulerAngles 了！
        // 因为这里转的是纯视觉子物体，完全不踩物理引擎的雷区！
        if (parentRb.linearVelocityX > 0.1f)
        {
            transform.eulerAngles = Vector3.zero;
        }
        else if (parentRb.linearVelocityX < -0.1f)
        {
            transform.eulerAngles = new Vector3(0f, 180f, 0f);
        }
    }
}