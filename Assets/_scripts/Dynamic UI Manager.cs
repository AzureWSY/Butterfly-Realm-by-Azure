using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DynamicUIManager : MonoBehaviour
{
    [Header("血量系统引用")]
    public PlayerHealth playerHealth;
    public Image[] hpFills;

    [Header("能量系统引用")]
    public PlayerEnergySystem Energy;
    public Image[] energyBars;

    [Header("表现设置")]
    [Tooltip("UI平滑填充的速度（数值越小，过渡越慢）")]
    public float lerpSpeed = 5f;

    // --- 能量平滑控制变量 ---
    private Coroutine energyLerpCoroutine;
    private float[] currentEnergyFills;

    // --- ?? 血量平滑控制变量 ---
    private Coroutine hpLerpCoroutine;
    private float[] currentHPFills;

    private void Awake()
    {
        // 1. 初始化能量数组 (备好案板)
        if (energyBars.Length > 0)
        {
            currentEnergyFills = new float[energyBars.Length];
            for (int i = 0; i < energyBars.Length; i++)
            {
                if (energyBars[i] != null) currentEnergyFills[i] = energyBars[i].fillAmount;
            }
        }

        // 2. ?? 初始化血量数组 (备好第二块案板)
        if (hpFills.Length > 0)
        {
            currentHPFills = new float[hpFills.Length];
            for (int i = 0; i < hpFills.Length; i++)
            {
                if (hpFills[i] != null) currentHPFills[i] = hpFills[i].fillAmount;
            }
        }
    }

    private void OnEnable()
    {
        // 血量依然用最硬核的代码订阅
        if (playerHealth != null) playerHealth.OnHealthChanged += UpdateHealthBar;

        // （能量的订阅咱们留在了 Unity 面板的 UnityEvent 里，所以这里不用写）
    }

    private void OnDisable()
    {
        if (playerHealth != null) playerHealth.OnHealthChanged -= UpdateHealthBar;
    }

    // ==========================================
    // ?? 血量表现系统 (升级为平滑倒水算法！)
    // ==========================================
    private void UpdateHealthBar(int currentHP, int maxHP)
    {
        if (hpFills.Length == 0) return;

        // 1. 算比例
        float healthPercentage = (float)currentHP / maxHP;

        // 2. 算目标
        float[] targets = new float[hpFills.Length];
        for (int i = 0; i < targets.Length; i++)
        {
            targets[i] = Mathf.Clamp01((healthPercentage * hpFills.Length) - i);
        }

        // 3. 牵狗绳，放平滑导弹！
        if (hpLerpCoroutine != null) StopCoroutine(hpLerpCoroutine);
        hpLerpCoroutine = StartCoroutine(LerpHealthSegments(targets));
    }

    // ?? 血量专用的平滑协程
    IEnumerator LerpHealthSegments(float[] targetFills)
    {
        bool stillLerping = true;
        while (stillLerping)
        {
            stillLerping = false;
            for (int i = 0; i < hpFills.Length; i++)
            {
                if (hpFills[i] == null) continue;

                currentHPFills[i] = Mathf.Lerp(currentHPFills[i], targetFills[i], Time.deltaTime * lerpSpeed);

                if (Mathf.Abs(currentHPFills[i] - targetFills[i]) > 0.001f)
                {
                    stillLerping = true;
                }
                else
                {
                    currentHPFills[i] = targetFills[i];
                }
                hpFills[i].fillAmount = currentHPFills[i];
            }
            yield return null;
        }
    }

    // ==========================================
    // ?? 能量表现系统 (保持不变)
    // ==========================================
    public void OnEnergyChanged(int current, int max)
    {
        if (energyBars.Length == 0) return;

        float totalFillPercentage = (float)current / max;
        float[] targets = new float[energyBars.Length];

        for (int i = 0; i < targets.Length; i++)
        {
            targets[i] = Mathf.Clamp01((totalFillPercentage * energyBars.Length) - i);
        }

        if (energyLerpCoroutine != null) StopCoroutine(energyLerpCoroutine);
        energyLerpCoroutine = StartCoroutine(LerpEnergySegments(targets));
    }

    // ?? 能量专用的平滑协程
    IEnumerator LerpEnergySegments(float[] targetFills)
    {
        bool stillLerping = true;
        while (stillLerping)
        {
            stillLerping = false;
            for (int i = 0; i < energyBars.Length; i++)
            {
                if (energyBars[i] == null) continue;

                currentEnergyFills[i] = Mathf.Lerp(currentEnergyFills[i], targetFills[i], Time.deltaTime * lerpSpeed);

                if (Mathf.Abs(currentEnergyFills[i] - targetFills[i]) > 0.001f)
                {
                    stillLerping = true;
                }
                else
                {
                    currentEnergyFills[i] = targetFills[i];
                }
                energyBars[i].fillAmount = currentEnergyFills[i];
            }
            yield return null;
        }
    }
}