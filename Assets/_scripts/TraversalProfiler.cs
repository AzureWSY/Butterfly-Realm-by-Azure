using UnityEngine;

public class TraversalProfiler : MonoBehaviour
{
    [Header("Debug")]
    public bool printResult = true;

    private float startTime;
    private bool timing;

    public void Begin()
    {
        startTime = Time.time;
        timing = true;

        if (printResult)
            Debug.Log("Traversal Start");
    }

    public void End(float distance)
    {
        if (!timing)
            return;

        timing = false;

        float travelTime = Time.time - startTime;
        float speed = distance / travelTime;

        Debug.Log(
            $"Travel Time : {travelTime:F3}s\n" +
            $"Distance    : {distance:F2} tiles\n" +
            $"Avg Speed   : {speed:F2} tiles/s");
    }
}