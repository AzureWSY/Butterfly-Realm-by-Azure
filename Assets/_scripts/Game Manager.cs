using UnityEngine;

public class GameManager : MonoBehaviour
{
    // 单例：让全游戏任何脚本都能瞬间找到它
    // 工业级标准：全局唯一常驻单例
    public static GameManager Instance { get; private set; }
    private Vector3 currentRespawnPos; // 存在班长脑子里的复活坐标
    private PlayerHub playerHub;
    // 真正的、不可磨灭的全局档案
    public int currentCloneID = 28;
    // 🌟 1. 在你的 GameManager.cs 顶部或成员变量区声明一个当前抓取器句柄槽位
    private BlueVineTrap_Ultimate_MultiCollision currentActiveGrabber = null;
    public BlueVineTrap_Ultimate_MultiCollision CurrentActiveGrabber => currentActiveGrabber;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 跨场景不死，记忆永存
        }
        else
        {
            Destroy(gameObject);
        }
    }


    private void Update()
    {
        if (LaserTrapManager.Instance == null)
            return;

        if (DynamicFloorManager.Instance == null)
            return;

        if (playerHub == null)
            return;

        if (!playerHub.CanBeTracked)
            return;

        if (LaserTrapManager.Instance != null && playerHub.Collider != null)
        {
            // 算好中心，主动推下去，让下层完全变成被动听令的工具
            Vector3 currentCenter = playerHub.Collider.bounds.center;
            LaserTrapManager.Instance.UpdateTrackTarget(currentCenter);
        }
    }


    // 开放给蝴蝶存档点调用的【指令】接收口
    public void SetNewRespawnPoint(Vector3 newPos) => currentRespawnPos = newPos;

    // 真正的复活逻辑
    private void RespawnPlayer()
    {
        Debug.Log("<color=red>GameManager 收到死讯，矩阵正在熔断重组...</color>");

        // 🚨【最高优先级：中央集权强控熔断闸】🚨
        // 在搬运肉体和重组克隆体的前一微秒，必须雷打不动地执行定点轰杀，强行斩断一切 Transform 坐标强刷践踏！
        if (currentActiveGrabber != null)
        {
            currentActiveGrabber.ForceReleaseAndAbort(); // 轰碎触手内的一切 while 强刷循环
            currentActiveGrabber = null; // 强行清空槽位，不留任何后患
        }

        ExecuteCurrentClone();

        


        if (playerHub != null) playerHub.Respawn(currentRespawnPos);
       

        // 2. 🔥 【终极快车道】：由于换成了 List，我们可以用完全规避任何接口调用的经典 for 循环！
        // 数组大小在内存中是连续的，CPU 缓存命中率（Cache Locality）直接拉满！
        int LaserTrapTriggercount = LaserChaseTrigger.ActiveTriggersList.Count;
        for (int i = 0; i < LaserTrapTriggercount; i++)
        {
            LaserChaseTrigger trigger = LaserChaseTrigger.ActiveTriggersList[i];

            // 强网防护：即使有极端罕见的场景切换意外丢失，null 校验也能在连续内存中一闪而过
            if (trigger != null)
            {
                trigger.ResetTriggerState();
            }
        }

        Debug.Log($"<color=#00FFCC>[GameManager]</color> 连续内存名册重置完毕！完美批量重装 <color=yellow>{LaserTrapTriggercount}</color> 个地砖触发器。");
        
        LaserTrapManager.Instance?.ResetAllDrivers(); 

        DynamicFloorManager.Instance?.ResetAllFloors();
        Debug.Log("动态平台重置完毕");

        Debug.Log(
        $"Respawn Pos = {playerHub.transform.position}"
        );

    }
    // 处决后调用的方法
    public void ExecuteCurrentClone()
    {
        currentCloneID++;
        // 这里甚至可以顺便触发全局存档逻辑：SaveSystem.SaveGame();
    }

    //Part 1 登记玩家
    public void RegisterPlayerHub(PlayerHub hub)
    {
        UnSubscribePlayerDeath();

        playerHub = hub;

        SubscribePlayerDeath();

        if (playerHub != null)
        {
            currentRespawnPos = playerHub.GetPosition();
        }

        Debug.Log(
            $"<color=green>【GameManager】No.{currentCloneID} 代克隆体枢纽已成功激活并接入中央网络！</color>"
        );
    }

    private void SubscribePlayerDeath()
    {
        if (playerHub != null && playerHub.Health != null) playerHub.Health.OnPlayerDeath += RespawnPlayer;
    }
    private void UnSubscribePlayerDeath()
    {
        if (playerHub != null && playerHub.Health != null) playerHub.Health.OnPlayerDeath -= RespawnPlayer;
    }

    // Part 2. 暴露给藤蔓调用的登记与注销接口
    public void RegisterCurrentGrabber(BlueVineTrap_Ultimate_MultiCollision grabber)
    {
        currentActiveGrabber = grabber;
        Debug.Log($"<color=orange>[指挥部记] 玩家已被藤蔓陷阱 {grabber.name} 捕获，句柄已被中央锁定。</color>");
    }

    public void ClearCurrentGrabber(BlueVineTrap_Ultimate_MultiCollision grabber)
    {
        if (currentActiveGrabber == grabber)
        {
            currentActiveGrabber = null;
            Debug.Log("<color=green>[指挥部登记清空] 玩家已被正常抛出或挣脱，句柄解控。</color>");
        }
    }

}