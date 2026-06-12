using UnityEngine;
using System.Collections;
using UnityEngine.Rendering.Universal;

public class PlayerGlowController : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Material glowMaterial;

    [Header("光效绑定 (主副双灯流)")]
    [Tooltip("照亮周围环境的灯 (剔除Player层)")]
    public Light2D environmentLight;
    [Tooltip("专门照亮玩家自己的灯 (只勾选Player层)")]
    public Light2D selfLight;
    public float baseLightIntensity = 0f;

    [Header("1. 蝴蝶光 (手动常驻) 配置")]
    public float butterflyShaderIntensity = 0.4f;
    public float butterflyEnvLight = 0.8f;
    public float butterflySelfLight = 0.5f;
    public float butterflyFadeSpeed = 0.8f;
    private bool isButterflyLightOn = false;

    [Header("2. 充能爆发 (最高优先级) 配置")]
    public string multiplierParam = "_CurrentGlowIntensityMultiplier";
    public float burstShaderIntensity = 1.0f;
    public float burstEnvLight = 1.8f;
    public float burstSelfLight = 0.8f;
    public float fadeInDuration = 1.0f;
    public float stayDuration = 2.0f;
    public float fadeOutDuration = 1.5f;
    private bool isBursting = false;

    [Header("3. 受击故障 (独立系统) 配置")]
    public string glitchMultiplierParam = "_DamageGlitchMultiplier";
    public float damageFlashDuration = 0.15f;
    public float damageEnvLight = 2.5f;
    public float damageSelfLight = 1.2f;

    [Header("4. 电量系统预留")]
    public float batteryDrainPerSecond = 2.0f;
    public float currentBattery = 100f;

    // 🌟【优化核心】：预留运行时哈希化之后的整数 ID 变量
    private int multiplierID;
    private int glitchMultiplierID;

    // ======== 核心状态隔离变量 ========
    private float currentBaseEnvLight = 0f;
    private float currentDamageEnvLight = 0f;
    private float currentBaseSelfLight = 0f;
    private float currentDamageSelfLight = 0f;

    private Coroutine baseGlowCoroutine;
    private Coroutine damageFlashCoroutine;

    void Awake()
    {
        // 🌟【优化点 1】：在游戏初始化的第一帧，瞬间将配置的字符串转换为唯一的 int ID
        multiplierID = Shader.PropertyToID(multiplierParam);
        glitchMultiplierID = Shader.PropertyToID(glitchMultiplierParam);

        // 🌟【最核心的改造】：从 GetComponent 改为 GetComponentInChildren！
        // 脚本挂在父物体上当总指挥，它会跨越层级，自动钻进子物体 Visual 里面把衣服（材质球）完好无损地抓出来！
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            glowMaterial = spriteRenderer.material;
            // 🌟【优化点 2】：HasProperty 和 SetFloat 全部无缝切换为性能极高的 ID 版本
            if (glowMaterial.HasProperty(multiplierID)) glowMaterial.SetFloat(multiplierID, 0f);
            if (glowMaterial.HasProperty(glitchMultiplierID)) glowMaterial.SetFloat(glitchMultiplierID, 0f);
        }

        currentBaseEnvLight = baseLightIntensity;
        currentBaseSelfLight = baseLightIntensity;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.L)) ToggleButterflyLight();
        if (isButterflyLightOn) ConsumeBattery();

        if (environmentLight != null)
            environmentLight.intensity = Mathf.Max(currentBaseEnvLight, currentDamageEnvLight);

        if (selfLight != null)
            selfLight.intensity = Mathf.Max(currentBaseSelfLight, currentDamageSelfLight);
    }

    private void ToggleButterflyLight()
    {
        isButterflyLightOn = !isButterflyLightOn;

        if (!isBursting)
        {
            if (baseGlowCoroutine != null) StopCoroutine(baseGlowCoroutine);

            float targetShader = isButterflyLightOn ? butterflyShaderIntensity : 0f;
            float targetEnv = isButterflyLightOn ? butterflyEnvLight : baseLightIntensity;
            float targetSelf = isButterflyLightOn ? butterflySelfLight : baseLightIntensity;

            baseGlowCoroutine = StartCoroutine(AnimateBaseGlow(targetShader, targetEnv, targetSelf, butterflyFadeSpeed));
        }
    }

    private void ConsumeBattery()
    {
        if (currentBattery > 0)
        {
            currentBattery -= batteryDrainPerSecond * Time.deltaTime;
            if (currentBattery <= 0)
            {
                currentBattery = 0;
                if (isButterflyLightOn) ToggleButterflyLight();
                Debug.Log("电池耗尽！蝴蝶光已强制关闭。");
            }
        }
    }

    public void StartGlowPulse()
    {
        if (baseGlowCoroutine != null) StopCoroutine(baseGlowCoroutine);
        baseGlowCoroutine = StartCoroutine(GlowPulseSequence());
    }

    private IEnumerator GlowPulseSequence()
    {
        isBursting = true;

        yield return StartCoroutine(AnimateBaseGlow(burstShaderIntensity, burstEnvLight, burstSelfLight, fadeInDuration));
        yield return new WaitForSeconds(stayDuration);

        float endShader = isButterflyLightOn ? butterflyShaderIntensity : 0f;
        float endEnv = isButterflyLightOn ? butterflyEnvLight : baseLightIntensity;
        float endSelf = isButterflyLightOn ? butterflySelfLight : baseLightIntensity;

        yield return StartCoroutine(AnimateBaseGlow(endShader, endEnv, endSelf, fadeOutDuration));

        isBursting = false;
    }

    public void PlayDamageFlash()
    {
        if (damageFlashCoroutine != null) StopCoroutine(damageFlashCoroutine);
        damageFlashCoroutine = StartCoroutine(DamageFlashSequence());
    }

    private IEnumerator DamageFlashSequence()
    {
        if (glowMaterial == null) yield break;

        // 🌟【优化点 3】：协程内部及高频循环全部采用 ID 操作
        glowMaterial.SetFloat(glitchMultiplierID, 0.8f);
        currentDamageEnvLight = damageEnvLight;
        currentDamageSelfLight = damageSelfLight;

        float elapsed = 0f;
        while (elapsed < damageFlashDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / damageFlashDuration;

            glowMaterial.SetFloat(glitchMultiplierID, Mathf.Lerp(0.8f, 0f, t));
            currentDamageEnvLight = Mathf.Lerp(damageEnvLight, 0f, t);
            currentDamageSelfLight = Mathf.Lerp(damageSelfLight, 0f, t);
            yield return null;
        }

        glowMaterial.SetFloat(glitchMultiplierID, 0f);
        currentDamageEnvLight = 0f;
        currentDamageSelfLight = 0f;
    }

    private IEnumerator AnimateBaseGlow(float targetShader, float targetEnv, float targetSelf, float duration)
    {
        // 🌟【优化点 4】：高频 GetFloat 换成 ID 抓取，杜绝每帧字符串寻址
        float startShader = glowMaterial.GetFloat(multiplierID);
        float startEnv = currentBaseEnvLight;
        float startSelf = currentBaseSelfLight;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            glowMaterial.SetFloat(multiplierID, Mathf.Lerp(startShader, targetShader, t));
            currentBaseEnvLight = Mathf.Lerp(startEnv, targetEnv, t);
            currentBaseSelfLight = Mathf.Lerp(startSelf, targetSelf, t);

            yield return null;
        }

        glowMaterial.SetFloat(multiplierID, targetShader);
        currentBaseEnvLight = targetEnv;
        currentBaseSelfLight = targetSelf;
    }

    public void TurnOffAllGlows()
    {
        StopAllCoroutines();
        isBursting = false;
        isButterflyLightOn = false;

        if (glowMaterial != null)
        {
            glowMaterial.SetFloat(multiplierID, 0f);
            glowMaterial.SetFloat(glitchMultiplierID, 0f);
        }
        currentBaseEnvLight = baseLightIntensity;
        currentBaseSelfLight = baseLightIntensity;
        currentDamageEnvLight = 0f;
        currentDamageSelfLight = 0f;
    }
}