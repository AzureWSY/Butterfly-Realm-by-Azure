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

        ExecuteCurrentClone();

        if (playerHub != null) playerHub.Respawn(currentRespawnPos);

        LaserTrapManager.Instance?.ResetAllDrivers();
    }
    // 处决后调用的方法
    public void ExecuteCurrentClone()
    {
        currentCloneID++;
        // 这里甚至可以顺便触发全局存档逻辑：SaveSystem.SaveGame();
    }
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
}