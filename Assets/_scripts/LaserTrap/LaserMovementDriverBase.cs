using UnityEngine;

public abstract class LaserMovementDriverBase : LaserBehaviorDriverBase
{
    public enum MovementAxis { AlongBeam, Perpendicular }

    [Header("??? 移动方向轴向配置")]
    public MovementAxis moveAxis = MovementAxis.AlongBeam;

    // 核心数学工具：供位移儿子们高效率调用的局部方向向量计算
    protected Vector3 GetMovementDirection()
    {
        // AlongBeam 沿着光束方向移动用 transform.right (红轴)
        // Perpendicular 垂直于光束横向切割平移用 transform.up (绿轴)
        return (moveAxis == MovementAxis.AlongBeam) ? transform.right : transform.up;
    }
}