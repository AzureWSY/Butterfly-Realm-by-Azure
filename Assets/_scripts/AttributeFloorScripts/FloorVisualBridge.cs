using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class FloorVisualBridge : MonoBehaviour
{
    [System.Serializable]
    public struct VisualReferences
    {
        [Header("核心视觉组件")]
        public SpriteRenderer mainRenderer;
        [Header("Shader/光照发光节点")]
        public GameObject glowLayer;
        public GameObject glowlight;
        [Header("UI 冷却时钟图片")]
        public Image cooldownClockImage;
        public Image cooldownClockRestImage;
    }

    [Header("🧩 视觉与UI资产绑定")]
    [SerializeField] private VisualReferences refs;

    public SpriteRenderer MainRenderer => refs.mainRenderer;

    private float baseLightIntensity = 1.0f;
    private Color baseSpriteColor = Color.white;
    private Coroutine activeFeedbackVisualRoutine;

    private void Awake()
    {
        if (refs.glowlight != null && refs.glowlight.TryGetComponent<Light2D>(out var light2D))
        {
            baseLightIntensity = light2D.intensity;
        }
        if (refs.mainRenderer != null)
        {
            baseSpriteColor = refs.mainRenderer.color;
        }
    }

    public void SetVisualsActive(bool active)
    {
        if (refs.glowLayer != null) refs.glowLayer.SetActive(active);
        if (refs.glowlight != null) refs.glowlight.SetActive(active);
    }

    public void PlayJuicyFeedback(Color flashColor, float lightMultiplier, bool hideAfterFade)
    {
        if (activeFeedbackVisualRoutine != null) StopCoroutine(activeFeedbackVisualRoutine);
        activeFeedbackVisualRoutine = StartCoroutine(FeedbackLerpRoutine(flashColor, lightMultiplier, hideAfterFade));
    }

    private IEnumerator FeedbackLerpRoutine(Color flashColor, float lightMultiplier, bool hideAfterFade)
    {
        float elapsed = 0f;
        float duration = 0.25f;

        if (refs.mainRenderer != null)
        {
            refs.mainRenderer.enabled = true;
            refs.mainRenderer.color = flashColor;
        }

        Light2D light2D = refs.glowlight != null ? refs.glowlight.GetComponent<Light2D>() : null;
        float peakIntensity = baseLightIntensity * lightMultiplier;

        if (light2D != null)
        {
            light2D.intensity = peakIntensity;
            if (refs.glowLayer != null) refs.glowLayer.SetActive(true);
            refs.glowlight.SetActive(true);
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            if (hideAfterFade)
            {
                if (refs.mainRenderer != null)
                    refs.mainRenderer.color = Color.Lerp(flashColor, new Color(flashColor.r, flashColor.g, flashColor.b, 0f), t);
                if (light2D != null)
                    light2D.intensity = Mathf.Lerp(peakIntensity, 0f, t);
            }
            else
            {
                if (refs.mainRenderer != null)
                    refs.mainRenderer.color = Color.Lerp(flashColor, baseSpriteColor, t);
                if (light2D != null)
                    light2D.intensity = Mathf.Lerp(peakIntensity, baseLightIntensity, t);
            }
            yield return null;
        }

        if (hideAfterFade)
        {
            if (refs.mainRenderer != null) refs.mainRenderer.enabled = false;
            SetVisualsActive(false);
        }
        else
        {
            ResetVisualsToDefault();
        }
        activeFeedbackVisualRoutine = null;
    }

    public void ResetVisualsToDefault()
    {
        if (activeFeedbackVisualRoutine != null) StopCoroutine(activeFeedbackVisualRoutine);

        if (refs.mainRenderer != null)
        {
            refs.mainRenderer.color = baseSpriteColor;
            refs.mainRenderer.enabled = true;
        }
        if (refs.glowlight != null && refs.glowlight.TryGetComponent<Light2D>(out var light2D))
        {
            light2D.intensity = baseLightIntensity; // 绝对归位原点数据
        }
        SetVisualsActive(true);
        activeFeedbackVisualRoutine = null;
    }

    public void ShowClock()
    {
        if (refs.cooldownClockImage != null)
        {
            refs.cooldownClockImage.gameObject.SetActive(true);
            refs.cooldownClockImage.fillAmount = 0f;
        }
        if (refs.cooldownClockRestImage != null) refs.cooldownClockRestImage.gameObject.SetActive(true);
    }

    public void UpdateClockFill(float progress)
    {
        if (refs.cooldownClockImage != null) refs.cooldownClockImage.fillAmount = progress;
    }

    public void HideClock()
    {
        if (refs.cooldownClockImage != null) refs.cooldownClockImage.gameObject.SetActive(false);
        if (refs.cooldownClockRestImage != null) refs.cooldownClockRestImage.gameObject.SetActive(false);
    }
}