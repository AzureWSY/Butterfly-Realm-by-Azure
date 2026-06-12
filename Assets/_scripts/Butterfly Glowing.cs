using UnityEngine;
using System.Collections;
using System;

public class ButterflyGlowController : MonoBehaviour
{
    [Header("Shader 配置")]
    public string glowColorParam = "_GlowColor";

    [Header("发光颜色与设置")]
    [Tooltip("你想让蝴蝶发出什么颜色的光？（直接在这里选！）")]
    [ColorUsage(true, true)] // ?? 就是这句魔法代码！第一个 true 显示透明度，第二个 true 开启 HDR 面板
    public Color targetGlowColor = Color.cyan;

    public float glowUpDuration = 0.5f;
    public float maxIntensity = 3.0f;

    private Material butterflyMaterial;
    private Color normalColor;

    void Awake()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            butterflyMaterial = sr.material;

            // ?? 强行记录当前材质的颜色（只要你材质球是白的，这里记录的就是白的）
            if (butterflyMaterial.HasProperty(glowColorParam))
            {
                normalColor = butterflyMaterial.GetColor(glowColorParam);
            }
            else
            {
                normalColor = Color.white;
            }
        }
    }

    public void PlayChargeAndFade(Action onChargeComplete)
    {
        StopAllCoroutines();
        StartCoroutine(ChargeAndFadeSequence(onChargeComplete));
    }

    private IEnumerator ChargeAndFadeSequence(Action onChargeComplete)
    {
        // ?? 核心改变：发光目标色不再用白色去乘，而是直接使用你面板里指定的颜色！
        // ?? 因为你在面板里选的已经是带强度的 HDR 颜色了，直接拿来用就行！
        Color targetHDRColor = targetGlowColor;

        // --- 阶段 A：从平时的颜色（白色/原色）渐变到你指定的发光色 ---
        yield return StartCoroutine(AnimateColor(normalColor, targetHDRColor, glowUpDuration));

        // --- 阶段 B：顶点爆发！ ---
        onChargeComplete?.Invoke();

        // --- 阶段 C：从发光色渐变回平时的颜色 ---
        yield return StartCoroutine(AnimateColor(targetHDRColor, normalColor, glowUpDuration));
    }

    private IEnumerator AnimateColor(Color start, Color end, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            butterflyMaterial.SetColor(glowColorParam, Color.Lerp(start, end, elapsed / duration));
            yield return null;
        }
        butterflyMaterial.SetColor(glowColorParam, end);
    }
}