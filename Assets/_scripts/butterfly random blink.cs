using System.Collections;
using UnityEngine;

public class RandomBlinker : MonoBehaviour
{
    private Animator animator;

    [Header("眨眼时间间隔 (秒)")]
    public float minBlinkTime = 2.0f; // 最少等 2 秒
    public float maxBlinkTime = 6.0f; // 最多等 6 秒

    void Start()
    {
        // 获取蝴蝶身上的 Animator 组件
        animator = GetComponent<Animator>();

        // 游戏一开始，就启动隐藏的随机倒计时
        StartCoroutine(BlinkRoutine());
    }

    // 这是一个协程，专门用来处理“等待”逻辑，不会卡死游戏
    private IEnumerator BlinkRoutine()
    {
        // 这是一个死循环，只要蝴蝶还在场景里，就会一直倒数
        while (true)
        {
            // 1. 在 min 和 max 之间摇一个随机数字（比如摇到了 4.2 秒）
            float waitTime = Random.Range(minBlinkTime, maxBlinkTime);

            // 2. 脚本在这里暂停等待，什么都不做，直到 4.2 秒过去
            yield return new WaitForSeconds(waitTime);

            // 3. 时间到了！戳一下 Animator 里的 BlinkTrigger
            animator.SetTrigger("blink trigger");

            // 触发完之后，循环重新开始，再次摇一个新的随机时间继续等
        }
    }
}