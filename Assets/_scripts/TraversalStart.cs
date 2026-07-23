using UnityEngine;

public class TraversalStart : MonoBehaviour
{
    public TraversalProfiler profiler;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        profiler.Begin();
    }
}