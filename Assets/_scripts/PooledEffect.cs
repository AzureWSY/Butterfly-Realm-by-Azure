using UnityEngine;
using UnityEngine.Pool;

[RequireComponent(typeof(ParticleSystem))]
public class PooledEffect : MonoBehaviour
{
    private IObjectPool<GameObject> myPool;
    private ParticleSystem ps;

    private void Awake()
    {
        ps = GetComponent<ParticleSystem>();
    }

    public void InitPoolReference(IObjectPool<GameObject> pool)
    {
        myPool = pool;
    }

    // ========================================================
    // 👑 【官方钦定绝杀】：Unity 底层 C++ 直接驱动的原生停止回调
    // 零垃圾产生，百分之百精准（哪怕粒子被风吹长了时间也能完美适应）
    // ========================================================
    private void OnParticleSystemStopped()
    {
        if (myPool != null && gameObject.activeSelf)
        {
            myPool.Release(gameObject); // 精准回池
        }
    }
}