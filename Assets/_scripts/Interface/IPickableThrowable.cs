using UnityEngine;

public interface IPickableThrowable
{
    bool IsPickedUp { get; }
    void ToggleHighlight(bool active);
    void OnPickedUp(Transform holdPoint);
    void OnThrown(Vector2 velocity);
}