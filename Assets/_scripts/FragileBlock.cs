using UnityEngine;
using UnityEngine.Pool;

public class FragileBlock : MonoBehaviour
{
    [Header("组件引用")]
    [Tooltip("方块的实体碰撞体（非 Trigger）")]
    public BoxCollider2D solidCollider;

    [Header("特效预制体")]
    public GameObject breakEffectPrefab;

    // ==========================================
    // 👑 【核心重构】：焊上 static 关键字！
    // 让全场景不管是 100 个还是 1000 个方块，在内存里死死公用同一个大池子！
    // ==========================================
    private static IObjectPool<GameObject> sharedEffectPool;
    private static GameObject staticPrefabRef; // 静态工厂需要用到的预制体引脚

    private void Awake()
    {
        // 只要有任意一个方块醒来，就把预制体引脚焊死，防止静态方法找不到资源
        if (breakEffectPrefab != null)
        {
            staticPrefabRef = breakEffectPrefab;
        }

        // 核心关卡锁：不管有多少方块，全局只初始化【唯一一次】池子
        if (sharedEffectPool == null && staticPrefabRef != null)
        {
            sharedEffectPool = new ObjectPool<GameObject>(
                createFunc: CreateEffectInstance,
                actionOnGet: (obj) => obj.SetActive(true),
                actionOnRelease: (obj) => obj.SetActive(false),
                actionOnDestroy: (obj) => Destroy(obj),
                collectionCheck: true,
                defaultCapacity: 10,                   // 初始容量给 10 个
                maxSize: 30                            // 动作爆发期上限 30 个
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

        // 哪怕当前方块下一秒就要 active = false 了，也完全不耽误全局池子的运转！
        if (sharedEffectPool != null)
        {
            GameObject effect = sharedEffectPool.Get();
            effect.transform.position = transform.position;
            effect.transform.rotation = Quaternion.identity;
        }

        gameObject.SetActive(false);
    }

    public void ResetBlock()
    {
        gameObject.SetActive(true);
        if (solidCollider != null)
        {
            solidCollider.enabled = true;
        }
    }

    // 静态生产工厂：必须是 static 方法，才能被静态池子回调
    private static GameObject CreateEffectInstance()
    {
        GameObject effectGo = Instantiate(staticPrefabRef);
        var pooledScript = effectGo.GetComponent<PooledEffect>();
        if (pooledScript == null)
        {
            pooledScript = effectGo.AddComponent<PooledEffect>();
        }

        // 把这个全局唯一的静态池子注入给特效，让它播完后认得回家的路
        pooledScript.InitPoolReference(sharedEffectPool);
        return effectGo;
    }
}