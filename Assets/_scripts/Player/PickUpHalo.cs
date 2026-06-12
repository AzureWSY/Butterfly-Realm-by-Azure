using System.Collections;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class PickupHalo : MonoBehaviour
{
    [Header("光环基础配置")]
    [SerializeField] private int segments = 36;   // 圆形边数
    [SerializeField] private float radius = 0.4f; // 光环大小

    [Header("HDR 渐变配置")]
    [SerializeField] private Color haloColor = Color.white; // ?? 绝杀1：现在在面板上改颜色，100% 绝对生效！
    [SerializeField] private float targetIntensity = 4.0f; // 激活时的目标发光强度
    [SerializeField] private float fadeDuration = 0.25f;    // 平滑过渡时间（秒）

    private LineRenderer _line;
    private Coroutine _fadeCoroutine;
    private float _currentIntensity = 0f;

    private void Awake()
    {
        _line = GetComponent<LineRenderer>();


        _line.positionCount = segments + 1;
        _line.useWorldSpace = false;
        _line.startWidth = 0.04f;
        _line.endWidth = 0.04f;
        _line.loop = true;

        // ?? 绝杀2：更换为 URP 原生完美适配 LineRenderer 顶点色彩与阿尔法融合的粒子 Unlit 着色器
        _line.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));

        // ?? 绝杀3：强力防御红线！开局直接物理关闭 LineRenderer 组件，死都不允许它在没按 O 时偷跑！
        _line.enabled = false;

        UpdateHaloColor(0f);
        DrawHaloCircle();
    }

    /// <summary>
    /// 外部控制平滑淡入淡出
    /// </summary>
    public void FadeHalo(bool show)
    {
        Debug.Log($"FadeHalo Called : {show}");
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);

        // ?? 强力双保险：如果要开启搜寻，第一时间物理打开 LineRenderer 组件
        if (show) _line.enabled = true;

        float destination = show ? targetIntensity : 0f;
        _fadeCoroutine = StartCoroutine(FadeIntensityRoutine(destination, show));
    }

    private IEnumerator FadeIntensityRoutine(float target, bool show)
    {
        float start = _currentIntensity;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            _currentIntensity = Mathf.Lerp(start, target, elapsed / fadeDuration);
            UpdateHaloColor(_currentIntensity);
            yield return null;
        }

        _currentIntensity = target;
        UpdateHaloColor(_currentIntensity);

        // ?? 强力双保险：如果是关闭搜寻且已经完全淡出，直接物理关闭组件，省下渲染开销
        if (!show) _line.enabled = false;

        _fadeCoroutine = null;
    }

    private void UpdateHaloColor(float intensity)
    {
        // 计算阿尔法透明度的平滑渐变比例
        float alphaRatio = targetIntensity > 0 ? (intensity / targetIntensity) : 0f;

        // 顶点颜色完美融合：RGB 控制发光过载，Alpha 控制平滑隐形
        Color hdrColor = new Color(
            haloColor.r * intensity,
            haloColor.g * intensity,
            haloColor.b * intensity,
            haloColor.a * alphaRatio
        );

        _line.startColor = hdrColor;
        _line.endColor = hdrColor;
    }

    private void DrawHaloCircle()
    {
        float angle = 0f;
        for (int i = 0; i <= segments; i++)
        {
            float x = Mathf.Sin(Mathf.Deg2Rad * angle) * radius;
            float y = Mathf.Cos(Mathf.Deg2Rad * angle) * radius;
            _line.SetPosition(i, new Vector3(x, y, 0));
            angle += (360f / segments);
        }
    }
}