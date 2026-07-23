using UnityEngine;

public class TraversalEnd : MonoBehaviour
{
    public TraversalProfiler profiler;

    [Tooltip("起点到终点之间距离(Tile)")]
    public float distance = 10f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        profiler.End(distance);
    }
}