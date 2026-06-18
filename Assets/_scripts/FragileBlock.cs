using UnityEngine;
using UnityEngine.Pool;

public class FragileBlock : MonoBehaviour
{
    [Header("组件引用")]
    public BoxCollider2D solidCollider;

    [Header("特效预制体")]
    public GameObject breakEffectPrefab;

    private static IObjectPool<GameObject> sharedEffectPool;
    private static GameObject staticPrefabRef;

    private void Awake()
    {
        if (breakEffectPrefab != null)
        {
            staticPrefabRef = breakEffectPrefab;
        }

        if (sharedEffectPool == null && staticPrefabRef != null)
        {
            sharedEffectPool = new ObjectPool<GameObject>(
                createFunc: CreateEffectInstance,
                actionOnGet: (obj) => obj.SetActive(true),
                actionOnRelease: (obj) => obj.SetActive(false),
                actionOnDestroy: (obj) => Destroy(obj),
                collectionCheck: true,
                defaultCapacity: 10,
                maxSize: 30
            );
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<Playercontrol>(out Playercontrol pc))
        {
            if (pc.isSuperJumping)
            {
                DoBreak();
            }
        }
    }

    private void DoBreak()
    {
        if (solidCollider != null)
        {
            solidCollider.enabled = false;
        }

        if (sharedEffectPool != null)
        {
            // 1. 先捞出物体
            GameObject effect = sharedEffectPool.Get();

            // 2. 【避坑关键】：先强制把骨架挪到正确的位置上！
            effect.transform.position = transform.position;
            effect.transform.rotation = Quaternion.identity;

            // 3. 挪完位置后，再通知粒子系统：“现在可以开火了！”。彻底消灭闪烁
            if (effect.TryGetComponent<ParticleSystem>(out var ps))
            {
                ps.Clear(true);
                ps.Play(true);
            }
        }

        gameObject.SetActive(false);
    }

    private static GameObject CreateEffectInstance()
    {
        GameObject effectGo = Instantiate(staticPrefabRef);
        var pooledScript = effectGo.GetComponent<PooledEffect>();
        if (pooledScript == null)
        {
            pooledScript = effectGo.AddComponent<PooledEffect>();
        }
        pooledScript.InitPoolReference(sharedEffectPool);
        return effectGo;
    }
}