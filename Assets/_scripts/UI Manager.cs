using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class UIManager : MonoBehaviour
{
    [Header("基础系统 UI")]
    public TMP_Text statusText;       // 你的 "SYS_ID: TESK 028"

    [Header("阶段一 & 二：警告弹窗系统")]
    public GameObject warningPanel;   // 小警告弹窗底板
    public Image warningIcon;         // 小弹窗里的相框
    public Sprite yellowIcon;         // 画作A：黄色初级警告
    public Sprite redIcon;            // 画作B：红色终极警告
    public TMP_Text warningText;      // 小弹窗里的文字

    [Header("阶段三：终极处决系统")]
    public GameObject glitchTerminationVolume; // 全屏处决容器 (包含你画的那个蓝色/粉色标志)
    public GameObject terminationIcon; // 蓝色异类三角形图标

    [Header("跨部门通讯引用")]
    public Playercontrol player;
    public Volume globalVolume;

    [Header("处决演出设置")]
    [SerializeField] private float pulseFrequency = 10f; // TERMINATION 文字和蓝色三角形闪烁频率
    // 内部数据
    private int clickCount = 0;
    private ChromaticAberration chromaticAberration;
    private FilmGrain filmGrain;
    private LensDistortion lensDistortion;
    private Vignette vignette;
    private ColorAdjustments colorAdjustments;

    private void Start()
    {
        UpdateScreenID();

        // 开局时，把小警告框和全屏处决框都隐藏起来
        if (warningPanel != null) warningPanel.SetActive(false);
        if (glitchTerminationVolume != null) glitchTerminationVolume.SetActive(false);

        // 尝试获取后处理滤镜遥控器
        if (globalVolume != null)
        {
            globalVolume.profile.TryGet(out chromaticAberration);
            globalVolume.profile.TryGet(out filmGrain);
            globalVolume.profile.TryGet(out lensDistortion);
            globalVolume.profile.TryGet(out vignette);
            globalVolume.profile.TryGet(out colorAdjustments);
        }
        
    }

    // 绑在 EXIT 按钮上的事件
    public void OnExitButtonClicked()
    {
        clickCount++;
        
        if (clickCount == 1)
        {
            // --- 第一次点击：弹出黄色初级警告 ---
            if (player != null) player.SetMovementPermission(false);

            if (warningPanel != null) warningPanel.SetActive(true);
            if (warningIcon != null) warningIcon.sprite = yellowIcon;
            if (warningText != null) warningText.text = "WARNING:UNAUTHORIZED ACTION";
        }
        else if (clickCount == 2)
        {
            // --- 第二次点击：就地升级为红色终极警告 ---
            if (player != null) player.SetMovementPermission(false);
            if (warningPanel != null) warningPanel.SetActive(true);
            if (warningIcon != null) warningIcon.sprite = redIcon;
            if (warningText != null) warningText.text = "FINAL WARNING:SYSTEM OVERRIDE IMMINENT";
        }
        else if (clickCount == 3)
        {
            // --- 第三次点击：彻底激怒系统，准备处决！ ---

            // 1. 把刚才那个小警告框关掉（给大场面腾出空间）
            if (warningPanel != null) warningPanel.SetActive(false);

            // 2. 空降全屏蓝色处决标志！
            if (glitchTerminationVolume != null) glitchTerminationVolume.SetActive(true);

            // 3. 启动花屏处决演出！
            StartCoroutine(ExecutionSequence());
        }
    }
    public void OnCancelButtonClicked()
    {
        // 玩家认怂了，把小警告弹窗关掉
        if (warningPanel != null)
        {
            warningPanel.SetActive(false);
        }
        if (player != null) player.SetMovementPermission(true);
        // 注意：这里没有 clickCount = 0; 
        // 系统暗中记仇！下次再点 Exit，直接吃红牌！
    }
    private IEnumerator ExecutionSequence()
    {
        // 锁死主角手柄！
        if (player != null) player.SetMovementPermission(false);

        // 爆发出核弹级 URP 视觉花屏演出！
        float elapsed = 0f;
        float duration = 1.8f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            if (globalVolume != null)
            {
                // 疯狂随机拧动滤镜数值
                if (chromaticAberration != null) chromaticAberration.intensity.value = Random.Range(0.8f, 1.0f);
                if (filmGrain != null) filmGrain.intensity.value = Random.Range(0.9f, 1.0f);
                if (lensDistortion != null) lensDistortion.intensity.value = Random.Range(-50f, 50f);
                if (colorAdjustments != null)
                {
                    // 随机红色暗角和对比度
                    if (vignette != null) vignette.intensity.value = 0.5f + Random.Range(0.0f, 0.3f); // 鲜红色暗角随机跳动
                    colorAdjustments.contrast.value = 50f + Random.Range(0.0f, 20f);
                    colorAdjustments.saturation.value = 100f + Random.Range(0.0f, 20f);

                    // 2. 【重头戏：随机 Color Filter，造成色彩癫痫】
                    // 核心是渗人的鲜红 (1, 0, 0)，但我们给蓝绿色加一点随机性，让它产生不稳定的频闪。
                    colorAdjustments.colorFilter.value = new Color(1.0f, Random.Range(0.0f, 0.2f), Random.Range(0.0f, 0.2f), 1.0f);
                    // // 红色通道：必须是满的 (1.0),// 绿色通道：极低随机,// 蓝色通道：极低随机,透明度
                    
                }
            }
            // 让 UI 闪烁
            // 1. 让 TERMINATION 文字和蓝色三角形图标以 pulseFrequency 频率疯狂闪烁
            float pulseTime = Mathf.Repeat(elapsed * pulseFrequency, 1.0f);
            if (warningText != null) warningText.enabled = (pulseTime < 0.5f);
            if (terminationIcon != null) terminationIcon.SetActive(pulseTime < 0.5f);
            if (glitchTerminationVolume != null)
            {
                glitchTerminationVolume.SetActive(Random.value > 0.25f);
            }

            yield return null;
        }

        // 演出结束，恢复宁静
        ResetPostProcessing();

        // 核心数据变更：死了一个，编号 +1
        if (GameManager.Instance != null) GameManager.Instance.ExecuteCurrentClone();
        UpdateScreenID();

        // UI 状态重置
        if (glitchTerminationVolume != null) glitchTerminationVolume.SetActive(false); // 撤销全屏处决标志
        clickCount = 0; // 重置点击次数，029 上线重新计次

        // 解锁新克隆体
        if (player != null) player.SetMovementPermission(true);
    }

    private void ResetPostProcessing()
    {
        // 安全归零
        if (chromaticAberration != null) chromaticAberration.intensity.value = 0f;
        if (filmGrain != null) filmGrain.intensity.value = 0f;
        if (lensDistortion != null) lensDistortion.intensity.value = 0f;
        if (vignette != null) vignette.intensity.value = 0f;
        if (colorAdjustments != null)
        {
            colorAdjustments.contrast.value = 0f;
            colorAdjustments.saturation.value = 0f;
            colorAdjustments.colorFilter.value = Color.white;
        }
        
    }

    private void UpdateScreenID()
    {
        if (GameManager.Instance != null && statusText != null)
        {
            statusText.text = "SYS_ID: TESK 0" + GameManager.Instance.currentCloneID;
        }
    }
}