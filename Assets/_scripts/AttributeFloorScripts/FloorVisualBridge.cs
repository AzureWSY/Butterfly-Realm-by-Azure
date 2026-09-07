using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class FloorVisualBridge : MonoBehaviour
{
    [System.Serializable]
    public struct VisualReferences
    {
        [Header("核心视觉组件 (留空将自动抓取)")]
        public SpriteRenderer mainRenderer;

        [Header("Shader/光照发光节点")]
        public GameObject glowLayer;
        public GameObject glowlight;

        [Header("UI 冷却时钟图片")]
        public Image cooldownClockImage;
        public Image cooldownClockRestImage;

        [Header("✨ 粒子特效组件 (无需手动拖拽，代码自动获取子物体)")]
        public ParticleSystem floorParticleSystem;

        [Tooltip("踩踏交互时瞬间喷发的粒子数量")]
        public int burstParticleCount;
    }

    [Header("🧩 视觉与UI资产绑定")]
    [SerializeField] private VisualReferences refs = new VisualReferences { burstParticleCount = 25 };

    public SpriteRenderer MainRenderer => refs.mainRenderer;

    // 材质属性块沙盒
    private MaterialPropertyBlock propBlock;
    private int activeColorPropID;

    // Shader 属性名预缓存（性能零分配）
    private static readonly int GlowColorID = Shader.PropertyToID("_GlowColor");
    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorID = Shader.PropertyToID("_Color");

    private float baseLightIntensity = 1.0f;
    private Color baseSpriteColor = Color.white;
    private Color baseShaderColor = Color.white; // 提取出来的 HDR 颜色
    private Coroutine activeFeedbackVisualRoutine;

    private void Awake()
    {
        propBlock = new MaterialPropertyBlock();

        // 🌟【核心突破】：自动化组件装配与防呆双保险
        AutoBindComponents();

        // 初始化光照与渲染器
        if (refs.glowlight != null && refs.glowlight.TryGetComponent<Light2D>(out var light2D))
        {
            baseLightIntensity = light2D.intensity;
        }

        if (refs.mainRenderer != null)
        {
            baseSpriteColor = refs.mainRenderer.color;
            DetectShaderPropertyID();
            ExtractColorFromMaterial();
        }

        
    }

    /// <summary>
    /// 🤖 自动装配逻辑：彻底摆脱手动拖线
    /// </summary>
    private void AutoBindComponents()
    {
        // 1. 自动绑定主 SpriteRenderer
        if (refs.mainRenderer == null)
        {
            refs.mainRenderer = GetComponent<SpriteRenderer>();
        }

        // 2. 自动抓取子物体上的 ParticleSystem
        bool needReGrabParticle = refs.floorParticleSystem == null;

        // 🛡️ 防呆锁：如果槽位里误拖入了 Project 文件夹里的资源文件，强制清空并重新从子物体抓取实例！
        if (!needReGrabParticle && !refs.floorParticleSystem.gameObject.scene.IsValid())
        {
            Debug.LogWarning($"[FloorVisualBridge] {gameObject.name} 的粒子槽位指向了 Project 资产，已自动纠正为从自身子物体获取！", this);
            needReGrabParticle = true;
        }

        // 自动化从子物体中深度搜索粒子系统 (true 代表就算子物体处于未激活状态也能精准抓取)
        if (needReGrabParticle)
        {
            refs.floorParticleSystem = GetComponentInChildren<ParticleSystem>(true);
        }
    }

    private void DetectShaderPropertyID()
    {
        if (refs.mainRenderer == null || refs.mainRenderer.sharedMaterial == null)
        {
            activeColorPropID = ColorID;
            return;
        }

        Material mat = refs.mainRenderer.sharedMaterial;
        if (mat.HasProperty(GlowColorID)) activeColorPropID = GlowColorID;
        else if (mat.HasProperty(BaseColorID)) activeColorPropID = BaseColorID;
        else activeColorPropID = ColorID;
    }

    private void ExtractColorFromMaterial()
    {
        if (refs.mainRenderer == null) return;

        Material mat = refs.mainRenderer.sharedMaterial;
        if (mat != null && mat.HasProperty(activeColorPropID))
        {
            baseShaderColor = mat.GetColor(activeColorPropID);
        }
        else
        {
            baseShaderColor = refs.mainRenderer.color;
        }
    }

    /// <summary>
    /// MaterialPropertyBlock 安全插值变色
    /// </summary>
    private void ApplyPropertyBlockColor(Color targetColor)
    {
        if (refs.mainRenderer == null) return;

        refs.mainRenderer.GetPropertyBlock(propBlock);
        propBlock.SetColor(activeColorPropID, targetColor);
        refs.mainRenderer.SetPropertyBlock(propBlock);

        refs.mainRenderer.color = targetColor;

        // 可选扩展：使子物体的 SpriteRenderer / 额外发光层也能同步吃这个颜色（如果有需要）
    }

    public void InitParticleColor(Color floorHdrColor)
    {
        baseShaderColor = floorHdrColor;
        if (refs.floorParticleSystem != null)
        {
            var main = refs.floorParticleSystem.main;
            main.startColor = baseShaderColor;
            
        }
    }

    public void SetVisualsActive(bool active)
    {
        if (refs.glowLayer != null) refs.glowLayer.SetActive(active);
        if (refs.glowlight != null) refs.glowlight.SetActive(active);

        if (refs.floorParticleSystem != null)
        {
            var emission = refs.floorParticleSystem.emission;
            emission.enabled = active;
        }
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
        Color particleColor = flashColor * lightMultiplier / 4;

        if (refs.mainRenderer != null)
        {
            refs.mainRenderer.enabled = true;
            ApplyPropertyBlockColor(particleColor);
        }

        Light2D light2D = refs.glowlight != null ? refs.glowlight.GetComponent<Light2D>() : null;
        float peakIntensity = baseLightIntensity * lightMultiplier;

        if (light2D != null)
        {
            light2D.intensity = peakIntensity;
            if (refs.glowLayer != null) refs.glowLayer.SetActive(true);
            refs.glowlight.SetActive(true);
        }

        // 💥【粒子喷发与闪烁变色】
        if (refs.floorParticleSystem != null)
        {
            var particleMain = refs.floorParticleSystem.main;
            particleMain.startColor = particleColor;
            refs.floorParticleSystem.Emit(refs.burstParticleCount);
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            if (hideAfterFade)
            {
                if (refs.mainRenderer != null)
                {
                    Color lerpColor = Color.Lerp(particleColor, new Color(particleColor.r, particleColor.g, particleColor.b, 0f), t);
                    ApplyPropertyBlockColor(lerpColor);
                }
                if (light2D != null)
                    light2D.intensity = Mathf.Lerp(peakIntensity, 0f, t);
            }
            else
            {
                if (refs.mainRenderer != null)
                {
                    Color lerpColor = Color.Lerp(particleColor, baseShaderColor, t);
                    ApplyPropertyBlockColor(lerpColor);
                }
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
            ApplyPropertyBlockColor(baseSpriteColor);
            refs.mainRenderer.enabled = true;
        }

        if (refs.glowlight != null && refs.glowlight.TryGetComponent<Light2D>(out var light2D))
        {
            light2D.intensity = baseLightIntensity;
        }

        InitParticleColor(baseShaderColor);
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