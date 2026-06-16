using UnityEngine;
using UnityEngine.Pool;

public class PooledEffect : MonoBehaviour
{
    private ParticleSystem targetParticleSystem;
    private IObjectPool<GameObject> myPool;
    private float effectDuration;

    private void Awake()
    {
        targetParticleSystem = GetComponent<ParticleSystem>();

        // 计算粒子的最大生命周期 = 基础时长 + 粒子随机存活的最大时间
        if (targetParticleSystem != null)
        {
            var mainModule = targetParticleSystem.main;
            effectDuration = mainModule.duration + mainModule.startLifetime.constantMax;
        }
        else
        {
            effectDuration = 1.5f; // 保底时间
        }
    }

    // 初始化注入所属的对象池引用
    public void InitPoolReference(IObjectPool<GameObject> pool)
    {
        myPool = pool;
    }

    private void OnEnable()
    {
        // 每次从池子里捞出来时，重新计时准备回收
        CancelInvoke(nameof(ReturnToPool));
        Invoke(nameof(ReturnToPool), effectDuration);
    }

    private void ReturnToPool()
    {
        if (myPool != null && gameObject.activeSelf)
        {
            myPool.Release(gameObject); // 安全回池
        }
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(ReturnToPool));
    }
}