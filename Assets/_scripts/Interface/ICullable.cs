using UnityEngine;

/// <summary>
/// 🌟 通用视距/屏幕视锥剔除接口
/// 支持单点物体（如地砖）或多锚点长条物体（如超长蝶丝绳索、激光线等）
/// </summary>
public interface ICullable
{
    /// <summary>
    /// 供剔除器判定的关键特征点集（如单体为自身坐标，长条绳索为起点、终点、中点等多点采样）。
    /// 为杜绝运行时 GC，实现类应在内部持有固定数组缓存，只更新元素值而不动态 new 新数组。
    /// </summary>
    Vector3[] CullCheckPoints { get; }

    /// <summary>
    /// 当物体进入或离开“屏幕视野+过渡区”时触发
    /// </summary>
    /// <param name=isVisible>true 表示进入视野（应唤醒），false 表示离开视野（应休眠）</param>
    void OnCullingStateChanged(bool isVisible);
}
