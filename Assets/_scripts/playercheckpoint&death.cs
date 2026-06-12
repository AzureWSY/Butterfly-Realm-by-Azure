using UnityEngine;

public class Checkpoint : MonoBehaviour
{

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // 下达【指令】：班长，记住我现在的坐标！
            GameManager.Instance.SetNewRespawnPoint(transform.position);
            Debug.Log("叮！蝴蝶共鸣，复活点已更新！");
        }
    }
}