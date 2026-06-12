using UnityEngine;
using UnityEngine.Events;

public class PlayerEnergySystem : MonoBehaviour
{
    [Header("玩家核心数据")]
    public int currentEnergy = 0;
    public int maxEnergy = 100;        // ?? 放开上限！以后吃道具可以直接扩充这个值

    [System.Serializable]
    public class OnEnergyChangedEvent : UnityEvent<int, int> { }

    [Header("数据变化事件")]
    public OnEnergyChangedEvent OnEnergyChanged;

    private void Start()
    {
        if (OnEnergyChanged != null)
        {
            OnEnergyChanged.Invoke(currentEnergy, maxEnergy);
        }
    }

    public void AddButterflyEnergy(int amount)
    {
        currentEnergy += amount;
        currentEnergy = Mathf.Clamp(currentEnergy, 0, maxEnergy);

        if (OnEnergyChanged != null)
        {
            OnEnergyChanged.Invoke(currentEnergy, maxEnergy);
        }

        Debug.Log("<color=green>数据系统：能量已变为: " + currentEnergy + " / " + maxEnergy + "</color>");
    }
    // ?? 消耗能量接口 (供你的攻击、冲刺、特殊技能调用)
    public bool ConsumeButterflyEnergy(int amount)
    {
        // 1. 先查余额：钱不够，直接拒绝交易！
        if (currentEnergy < amount)
        {
            Debug.Log("<color=red>能量不足！需要: " + amount + "，但只有: " + currentEnergy + "</color>");
            return false; // 返回 false，外面的代码收到 false 就不会播放技能动画
        }

        // 2. 余额充足，正式扣钱
        currentEnergy -= amount;

        // 这行其实可以不写，因为上面已经拦截了不够扣的情况。
        // 但作为底层数据类，留着 Clamp 是个好习惯，双重保险防穿模。
        currentEnergy = Mathf.Clamp(currentEnergy, 0, maxEnergy);

        // 3. 扣动扳机，通知 UI 掉血（UI 脚本会自动复用之前的逻辑，平滑往下掉！）
        if (OnEnergyChanged != null)
        {
            OnEnergyChanged.Invoke(currentEnergy, maxEnergy);
        }

        Debug.Log("<color=yellow>消耗了 " + amount + " 点能量。剩余: " + currentEnergy + "</color>");
        return true; // 返回 true，告诉技能系统：“扣费成功，尽情放技能吧！”
    }
}