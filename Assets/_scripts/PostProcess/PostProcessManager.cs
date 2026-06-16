using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;

public class PostProcessManager : MonoBehaviour
{
    public static PostProcessManager Instance { get; private set; }

    [Header("后处理引用")]
    [SerializeField] private Volume nightVisionVolume;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // 暴露给外部开启夜视的方法
    public void EnableNightVision(bool enable)
    {
        if (nightVisionVolume == null) return;

        StopAllCoroutines(); // 停掉可能正在运行的过渡效果
        StartCoroutine(FadeVolumeRoutine(enable ? 1f : 0f));
    }

    // 负责平滑过渡滤镜
    private IEnumerator FadeVolumeRoutine(float targetWeight)
    {
        float startWeight = nightVisionVolume.weight;
        float elapsedTime = 0f;
        float transitionTime = 0.5f;

        while (elapsedTime < transitionTime)
        {
            elapsedTime += Time.deltaTime;
            nightVisionVolume.weight = Mathf.Lerp(startWeight, targetWeight, elapsedTime / transitionTime);
            yield return null;
        }
        nightVisionVolume.weight = targetWeight;
    }
}