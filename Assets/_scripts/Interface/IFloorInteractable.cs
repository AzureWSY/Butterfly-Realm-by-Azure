using UnityEngine;

public interface IFloorInteractable
{
    void OnEnterFloor(AttributeFloor floor);
    void OnExitFloor(AttributeFloor floor);
}