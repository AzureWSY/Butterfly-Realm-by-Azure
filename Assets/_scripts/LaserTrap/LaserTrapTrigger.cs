using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class LaserChaseTrigger : MonoBehaviour
{
    public enum LaserTriggerAction { Activate, Destroy }

    // =========================================================================
    // 👑 究极名册优化：改用纯连续内存 List，彻底消灭 HashSet 的空间浪费与无序性
    // =========================================================================
    public static readonly List<LaserChaseTrigger> ActiveTriggersList = new List<LaserChaseTrigger>(32);

    [Header("🔗 联动目标配置")]
    public LaserChaseDriver targetLaser;

    [Header("🎛️ 触发器功能设定")]
    public LaserTriggerAction triggerAction = LaserTriggerAction.Activate;

    [Header("👁️ 视觉表现组件")]
    public GameObject customVisualObject;
    public bool oneTimeUse = true;

    private Collider2D myCollider;
    private SpriteRenderer myRenderer;
    private bool hasTriggered = false;

    // 🔥 核心底层外挂：背下自己当前在静态 List 里的物理客座门牌号（索引）
    private int myRegistryIndex = -1;

    // =========================================================================
    // 🏢 Swap-and-Pop 零分配打卡机制（大厂主程免检标配）
    // =========================================================================
    private void OnEnable()
    {
        // 1. 入册：直接追加入队
        ActiveTriggersList.Add(this);
        // 2. 记住自己被分配在第几个格子
        myRegistryIndex = ActiveTriggersList.Count - 1;
    }

    private void OnDisable()
    {
        // 防呆防御：如果没有成功打卡过，直接拦截
        if (myRegistryIndex < 0) return;

        int lastIndex = ActiveTriggersList.Count - 1;

        // 🌟 Swap-and-Pop 核心断路魔术
        if (myRegistryIndex < lastIndex)
        {
            // 1. 把全校最后一个人抓出来
            LaserChaseTrigger lastElement = ActiveTriggersList[lastIndex];

            // 2. 强行把最后一个人按到“我要空出来的”这个格子里
            ActiveTriggersList[myRegistryIndex] = lastElement;

            // 3. 通知最后一个人：“你的门牌号换成我这个位置了！”
            lastElement.myRegistryIndex = myRegistryIndex;
        }

        // 4. 直接把最后一个格子剪掉（因为此时最后格子的数据已经安全复制到前面去了）
        // List 剪掉最后一个格子在 C# 底层是绝对的 O(1) 零内存移动开销！
        ActiveTriggersList.RemoveAt(lastIndex);

        // 5. 抹平自身指针
        myRegistryIndex = -1;
    }

    private void Start()
    {
        myCollider = GetComponent<Collider2D>();
        myRenderer = GetComponent<SpriteRenderer>();
        if (myCollider != null) myCollider.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered) return;
        if (targetLaser == null) return;

        if (other.CompareTag("Player"))
        {
            hasTriggered = true;
            switch (triggerAction)
            {
                case LaserTriggerAction.Activate: targetLaser.ActivateLaserTracker(); break;
                case LaserTriggerAction.Destroy: targetLaser.DestroyLaser(); break;
            }
            ToggleTriggerAvailability(false);
        }
    }

    public void ResetTriggerState()
    {
        hasTriggered = false;
        ToggleTriggerAvailability(true);
    }

    private void ToggleTriggerAvailability(bool available)
    {
        if (myCollider != null) myCollider.enabled = available;
        if (customVisualObject != null) customVisualObject.SetActive(available);
        else if (myRenderer != null) myRenderer.enabled = available;
    }
}