using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables; // 🌟 确保 Timeline 播放器组件正常运作

public class BossFightDirector : MonoBehaviour
{
    public static BossFightDirector Instance;

    [Header("🎬 核心演员 (解耦升级版)")]
    public EyeBossController boss;
    public ButterflyAltar altar;
    public BreathingGlow breathingGlow;
    public Transform playerSavePoint; // 玩家处决后复活的存档点

    [Header("🎥 摄像机机位")]
    public CinemachineCamera bossCam;
    public float introCameraSize = 12f; // 开场看Boss时的视野大小

    [Header("🏆 胜利演出参数 (Boss死亡)")]
    public float bossShowTime = 2.5f;
    public GameObject energyParticlePrefab; // 飞向祭坛的能量粒子
    public float particleFlyTime = 2.0f;
    public GameObject trappedButterfly;     // 祭坛里被困的蝴蝶
    public GameObject shuttleOrb;           // 奖励：穿梭球
    [ColorUsage(true, true)]
    public Color victoryColor = Color.cyan; // 祭坛胜利后的发光色

    [Header("💀 惩罚演出 (玩家死亡/祭坛被毁)")]
    [Tooltip("挂在 Boss 身上的那根蓝色藤蔓陷阱")]
    public BlueVineTrap_Ultimate_MultiCollision bossVine;
    public CanvasGroup jumpscareUI;   // 突脸图片UI
    public float pullPlayerTime = 0.8f;

    [Header("🎬 Timeline 剧本分发器")]
    public PlayableDirector introTimeline;
    public PlayableDirector playerDeathTimeline;
    public PlayableDirector altarDeathTimeline;
    public PlayableDirector victoryTimeline;

    [Header("环境重置 (Boss房地板)")]
    [Tooltip("只要把装有所有地板的【父物体】拖进来就行！")]
    public Transform bossFloorsParent;

    private bool introPlayed = false;
    private bool isCutscenePlaying = false;
    private bool isBossFightActive = false;
    private AttributeFloor[] allBossFloors;

    // =========================================================================
    // 🌟 终极纯净中介者持有人：删掉了 loose 变量区，一揽子抓牢玩家生命线
    // =========================================================================
    private PlayerHub playerHub;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (BossFightUIManager.Instance != null)
            BossFightUIManager.Instance.gameObject.SetActive(false);

        // 1. 🌟 自动化即插即用：利用 C++ 底层扫描快速锁死当场景中的玩家完全体枢纽名片
        playerHub = FindAnyObjectByType<PlayerHub>();

        if (playerHub != null)
        {
            // 2. 完美监听玩家死亡事件：直接通过 Hub 属性安全桥接
            if (playerHub.Health != null) playerHub.Health.OnPlayerDeath += OnPlayerDied;
        }
        else
        {
            Debug.LogError($"<color=red>[大导演]</color> 警告：未在场景中扫描到玩家完全体枢纽 PlayerHub！Timeline剧情演出瘫痪！");
        }

        // 3. 让大导演监听 Boss 的死亡广播！
        if (boss != null) boss.OnBossDeath += HandleBossDeath;

        // 4. 让大导演监听 蝴蝶祭坛 的毁灭广播！
        if (altar != null) altar.OnAltarDestroyed += HandleAltarDestroyed;

        // 5. 自动化抓取地板
        if (bossFloorsParent != null)
        {
            allBossFloors = bossFloorsParent.GetComponentsInChildren<AttributeFloor>(true);
            Debug.Log($"<color=cyan>[大导演]</color> 自动抓取到了 {allBossFloors.Length} 块 Boss 房地板！");
        }
    }

    private void OnDestroy()
    {
        // 🌟 严谨清理事件解绑，防止内存残留及切关卡时引发薛定谔的伪空指针诈尸
        if (playerHub != null && playerHub.Health != null)
        {
            playerHub.Health.OnPlayerDeath -= OnPlayerDied;
        }

        if (boss != null) boss.OnBossDeath -= HandleBossDeath;
        if (altar != null) altar.OnAltarDestroyed -= HandleAltarDestroyed;
    }

    // =========================================================================
    // 🟩 第一幕：开场动画 (由原本的硬编码协程，平稳过渡到 Timeline 信号)
    // =========================================================================
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!introPlayed && collision.CompareTag("Player"))
        {
            introPlayed = true;
            isBossFightActive = true;
            LockPlayer(true);

            if (BossFightUIManager.Instance != null)
                BossFightUIManager.Instance.gameObject.SetActive(true);

            if (introTimeline != null) introTimeline.Play();
        }
    }

    public void Signal_OnIntroComplete()
    {
        LockPlayer(false);
        if (boss != null) boss.StartBossFight();
    }

    // =========================================================================
    // 🟥 第二幕：失败惩罚 (藤蔓抓取 -> 突脸 -> 回档重置)
    // =========================================================================
    private void HandleAltarDestroyed()
    {
        if (!isBossFightActive || isCutscenePlaying) return;
        isCutscenePlaying = true;
        LockPlayer(true);
        if (altarDeathTimeline != null) altarDeathTimeline.Play();
    }

    private void OnPlayerDied()
    {
        if (!isBossFightActive || isCutscenePlaying) return;
        isCutscenePlaying = true;
        LockPlayer(true);
        if (playerDeathTimeline != null) playerDeathTimeline.Play();
    }

    public void Signal_TriggerPlayerDeathVine()
    {
        if (playerDeathTimeline != null)
        {
            playerDeathTimeline.Pause();
            StartCoroutine(ProceduralVineGrabRoutine(playerDeathTimeline));
        }
    }

    public void Signal_TriggerAltarDeathVine()
    {
        if (altarDeathTimeline != null)
        {
            altarDeathTimeline.Pause();
            StartCoroutine(ProceduralVineGrabRoutine(altarDeathTimeline));
        }
    }

    private IEnumerator ProceduralVineGrabRoutine(PlayableDirector activeDirector)
    {
        // 🌟 完美修复：直接把完全体 playerHub 指针作为硬数据注入给藤蔓！
        // 编译报错原地灰飞烟灭，藤蔓能够合规接管玩家刚体执行完美拉扯
        if (bossVine != null && playerHub != null)
        {
            yield return StartCoroutine(bossVine.ExecuteDirectorGrab(playerHub, pullPlayerTime));
        }
        else
        {
            yield return new WaitForSeconds(pullPlayerTime);
        }
        if (activeDirector != null) activeDirector.Resume();
    }

    public void Signal_OnDefeatDataReset()
    {
        if (boss != null) boss.ResetBoss();
        if (altar != null) altar.ResetAltar();

        if (BossFightUIManager.Instance != null)
            BossFightUIManager.Instance.gameObject.SetActive(false);

        // 地板环境回档
        if (allBossFloors != null)
        {
            int floorCount = allBossFloors.Length;
            for (int i = 0; i < floorCount; i++) // 零GC高性能索引遍历优化
            {
                if (allBossFloors[i] != null) allBossFloors[i].ResetFloorForBossFight();
            }
        }

        // =========================================================================
        // 🌟 完美的架构收拢：不再伸手去乱改玩家组件。
        // 下达最高死讯复位命令：“Hub，带克隆体闪现到保存点，并自己洗干净内部速度！”
        // =========================================================================
        if (playerHub != null)
        {
            Vector3 respawnPos = playerSavePoint != null ? playerSavePoint.position : playerHub.transform.position;
            playerHub.Respawn(respawnPos);
        }

        // 🌟 双重熔断保险：呼叫激光大管家，一键强制清洗重置场上所有在册运行的追击/时钟激光
        if (LaserTrapManager.Instance != null)
        {
            LaserTrapManager.Instance.ResetAllDrivers();
        }
    }

    public void Signal_OnDefeatSequenceComplete()
    {
        LockPlayer(false);
        isCutscenePlaying = false;
        introPlayed = false;
        isBossFightActive = false;
    }

    // =========================================================================
    // 🟦 第三幕：胜利结算 (Boss 死亡 -> 触发胜利时间轴)
    // =========================================================================
    private void HandleBossDeath()
    {
        if (!isBossFightActive || isCutscenePlaying) return;
        isCutscenePlaying = true;
        LockPlayer(true);

        if (BossFightUIManager.Instance != null)
            BossFightUIManager.Instance.gameObject.SetActive(false);

        if (victoryTimeline != null) victoryTimeline.Play();
    }

    public void Signal_OnVictoryApplyRewardsAndKillBoss()
    {
        if (breathingGlow != null) breathingGlow.ChangeColor(victoryColor);
        if (trappedButterfly != null) trappedButterfly.SetActive(false);
        if (shuttleOrb != null) shuttleOrb.SetActive(true);

        if (boss != null) Destroy(boss.gameObject);
    }

    public void Signal_OnVictorySequenceComplete()
    {
        LockPlayer(false);
        isCutscenePlaying = false;
        isBossFightActive = false;
    }

    // =========================================================================
    // 🛠️ 基础控制零件 (同步升级为中介者数据流通道)
    // =========================================================================
    private void LockPlayer(bool shouldLock)
    {
        if (playerHub == null) return;

        // 通过枢纽下达控制指令
        if (playerHub.Control != null)
        {
            playerHub.Control.SetMovementPermission(!shouldLock);
        }

        // 清零死前多余的惯性位移
        if (shouldLock && playerHub.Rb != null)
        {
            playerHub.Rb.linearVelocity = Vector2.zero;
        }

        Debug.Log($"<color=orange>【大导演】</color> 剧情权限流转 -> 锁定状态变更为: {shouldLock}");
    }
}