using UnityEngine;
using UnityEngine.Events; // 必须引入事件命名空间

[RequireComponent(typeof(Collider2D))]
public class HaloTrigger : MonoBehaviour
{
    [Header("触发设置")]
    [Tooltip("当玩家踩到光环时触发的事件")]
    public UnityEvent OnPlayerEnter;

    // 为了防止玩家在光环里反复横跳导致多次触发，加一个一次性锁
    public bool triggerOnlyOnce = true;
    private bool hasTriggered = false;

    

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasTriggered && triggerOnlyOnce) return;

        // 如果碰到的是玩家
        if (collision.CompareTag("Player"))
        {
            hasTriggered = true;
            OnPlayerEnter?.Invoke(); // 广播大喇叭：玩家进来了！

            // 可选：触发后让光环消失，或者改变颜色
            // gameObject.SetActive(false); 
        }
    }
}