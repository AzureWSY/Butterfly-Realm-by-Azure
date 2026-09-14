using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 动态冲击地板中央时序与生命周期管理器（单例模式 + 注册表机制）
/// 负责全场景动态平台的统一重置、暂停/恢复、以及多态批量调度
/// </summary>
public class DynamicFloorManager : MonoBehaviour
{
    public static DynamicFloorManager Instance { get; private set; }

    // 🌟 升级为 HashSet：天然支持 O(1) 检索、快速增删以及两端注册时的自动去重
    private readonly HashSet<DynamicFloorController> _registeredFloors = new HashSet<DynamicFloorController>();

    /*[Header("⚡ 全局状态监测")]
    [Tooltip("当前动态平台是否处于全局暂停状态")]
    [SerializeField] private bool isGlobalPaused = false;*/

    private void Awake()
    {
        // 经典单例保护
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        SweepAndRegisterExistingFloors();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            _registeredFloors.Clear();
        }
        
    }

    #region 📋 双保险注册表机制 (Dual-Insurance Registry)

    /// <summary>
    /// 开局主动扫描场景，抓取所有已摆放的地板（含 Inactive 物体）
    /// </summary>
    private void SweepAndRegisterExistingFloors()
    {
        // 仅在开局执行一次，零运行时性能负担
        DynamicFloorController[] sceneFloors = FindObjectsByType<DynamicFloorController>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < sceneFloors.Length; i++)
        {
            RegisterFloor(sceneFloors[i]);
        }
    }
    /// <summary>
    /// 动态地板激活时主动报到登记
    /// </summary>
    public void RegisterFloor(DynamicFloorController floor)
    {
        if (floor == null) return;
        _registeredFloors.Add(floor);// 同步当前管理器的全局暂停状态给新报到的地板
           
    }

    /// <summary>
    /// 动态地板失活/被销毁时主动注销，杜绝内存泄漏与悬空指针
    /// </summary>
    public void UnregisterFloor(DynamicFloorController floor)
    {
        if (floor == null) return;
        _registeredFloors.Remove(floor);
    }

    #endregion

    #region 集中调度指令 (Batch Command Dispatching)

    /// <summary>
    /// 🌟 统一重置所有动态地板：复位刚体坐标 + 归零所有状态机/速度/缓动时间轴
    /// （供玩家死亡复活、关卡重置、Boss战重新开局时一键调用）
    /// </summary>
    public void ResetAllFloors()
    {
        // 创建临时安全快照，防止遍历过程中集合被修改
        foreach (var floor in _registeredFloors)
        {
            if (floor != null)
            {
                floor.ResetToOrigin();
            }
        }

        // 防御性剔除意外销毁产生的空引用
        _registeredFloors.RemoveWhere(f => f == null);
    }

    /// <summary>
    /// ⏸️ 全局暂停 / 恢复所有动态平台的物理运动
    /// （供游戏暂停菜单、剧情对话、时间静止技能触发）
    /// </summary>
    /*/// <summary>
    /// ⏸️ 全局暂停/恢复
    /// </summary>
    public void SetAllPaused(bool paused)
    {
        isGlobalPaused = paused;

        foreach (var floor in _registeredFloors)
        {
            if (floor != null)
            {
                floor.SetPaused(paused);
            }
        }
    }
    */



    #endregion
}