using UnityEngine;
using System.Collections;
using System; // 🌟 必须引入 System 才能使用 Action 委托！
using Random = UnityEngine.Random;

public class EyeBossController : CharacterHealth
{
    public enum BossState { Calm, SingleLaser, CircleLaser, MildAnger, Enrage }
    [Header("📊 实时状态监视")]
    public BossState currentState = BossState.Calm;

    // 🌟🌟🌟 核心架构升级：Boss 的死亡广播！
    
    public event Action OnBossDeath; 

    [Header("⏱️ 节奏配置")]
    public float thinkInterval = 0.5f;
    public float calmTimeMin = 1f;
    public float calmTimeMax = 2f;

    [Header("💥 伤害与数值配置")]
    public int mildAngerAltarDamage = 50;
    public int enrageAltarDamage = 150;
    public int mildDrainMin = 10;
    public int mildDrainMax = 40;
    public float laserLengthMin = 10f;
    public float laserLengthMax = 25f;

    [Header("🎲 概率配置")]
    public float mildAngerBaseChance = 20f;
    public float enrageBaseChance = 15f;
    public float mildAngerPityAdd = 15f;
    public float enragePityAdd = 10f;

    [Header("👁️ 视差追踪与目标")]
    public Transform player;
    public ButterflyAltar altar;
    public float moveRadius = 0.5f;

    [Header("⚔️ 攻击表现配置")]
    public GameObject redLaserPrefab;      // 打玩家用的预制体
    public LineRenderer blueDrainLine;     // 吸能量的蓝光
    public LineRenderer redEnvLaserLine;   // 🌟 打祭坛/地板的红光

    [Header("⏱️ 激光动态生长参数 (核心)")]
    public float laserStretchTime = 0.3f;
    public float laserStayTime = 1f;
    public float laserVanishTime = 0.5f;

    private Vector3 initialLocalPos;
    private float mildAngerPity = 0f;
    private float enragePity = 0f;
    private bool isAttacking = false;

    // 📢 状态实时广播 (别忘了贴上我们刚才学过的 using 别名或者UnityEngine指定)
    public event Action<BossState> OnStateChanged;
    protected override void Awake()
    {
        base.Awake();
        initialLocalPos = transform.localPosition;

        if (blueDrainLine != null) blueDrainLine.enabled = false;
        if (redEnvLaserLine != null) redEnvLaserLine.enabled = false;
    }

    protected override void Start()
    {
        base.Start();
    }

    private void Update() { TrackPlayerParallax(); }

    public void StartBossFight()
    {
        StartCoroutine(BossStateMachine());
    }

    protected override void Die()
    {
        base.Die();

        // 1. 掐断 Boss 的所有攻击协程
        StopAllCoroutines();

        // 2. 关闭眼球的视觉（隐藏 Sprite、发光和特效，假装它死了）
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = false;

        // 🌟🌟🌟 3. 架构优化：Boss 不再去找导演，而是全图广播“我死了！”
        Debug.Log("<color=red>[Boss]</color> 眼球被击败，发送死亡广播！");
        OnBossDeath?.Invoke(); 
    }

    private void TrackPlayerParallax()
    {
        if (player == null) return;
        Vector3 dir = (player.position - transform.position).normalized;
        transform.localPosition = initialLocalPos + dir * moveRadius;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle + 90f);
    }

    private IEnumerator BossStateMachine()
    {
        yield return new WaitForSeconds(2f);
        while (CurrentHealth > 0)
        {
            if (!isAttacking) DetermineNextAttack();
            yield return new WaitForSeconds(thinkInterval);
        }
    }

    private void DetermineNextAttack()
    {
        float hpPercent = (float)CurrentHealth / MaxHealth;
        float randomRoll = Random.Range(0f, 100f);

        if (hpPercent <= 0.2f)
        {
            float chance = (0.2f - hpPercent) * 200f + enragePity + enrageBaseChance;
            if (randomRoll <= chance) { StartCoroutine(EnrageRoutine()); enragePity = 0f; return; }
            enragePity += enragePityAdd;
        }

        if (hpPercent <= 0.5f)
        {
            float chance = (0.5f - hpPercent) * 100f + mildAngerPity + mildAngerBaseChance;
            if (randomRoll <= chance) { StartCoroutine(MildAngerRoutine()); mildAngerPity = 0f; return; }
            mildAngerPity += mildAngerPityAdd;
        }

        int roll = Random.Range(0, 3);
        if (roll == 0) StartCoroutine(CalmRoutine());
        else if (roll == 1) StartCoroutine(SingleLaserRoutine());
        else StartCoroutine(CircleLaserRoutine());
    }

    private IEnumerator CalmRoutine()
    {
        isAttacking = true; currentState = BossState.Calm;
        OnStateChanged?.Invoke(currentState); // 🌟 补上这句！朝全场大喊状态变了！
        yield return new WaitForSeconds(Random.Range(calmTimeMin, calmTimeMax));
        isAttacking = false;
    }

    private IEnumerator SingleLaserRoutine()
    {
        isAttacking = true; currentState = BossState.SingleLaser;
        OnStateChanged?.Invoke(currentState); // 🌟 补上这句！朝全场大喊状态变了！
        if (redLaserPrefab != null && player != null)
        {
            Vector3 dir = (player.position - transform.position).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            GameObject laser = Instantiate(redLaserPrefab, transform.position, Quaternion.Euler(0, 0, angle));

            laser.GetComponent<LaserTrap>()?.SetupBossLaser(Random.Range(laserLengthMin, laserLengthMax), laserStretchTime, laserStayTime, laserVanishTime);
        }
        yield return new WaitForSeconds(3f);
        isAttacking = false;
    }

    private IEnumerator CircleLaserRoutine()
    {
        isAttacking = true; currentState = BossState.CircleLaser;
        OnStateChanged?.Invoke(currentState); // 🌟 补上这句！朝全场大喊状态变了！
        int count = 12; float len = Random.Range(laserLengthMin * 0.8f, laserLengthMax * 0.8f);
        for (int i = 0; i < count; i++)
        {
            GameObject laser = Instantiate(redLaserPrefab, transform.position, Quaternion.Euler(0, 0, i * (360f / count)));
            laser.GetComponent<LaserTrap>()?.SetupBossLaser(len, laserStretchTime, laserStayTime, laserVanishTime);
        }
        yield return new WaitForSeconds(4f);
        isAttacking = false;
    }

    private IEnumerator MildAngerRoutine()
    {
        isAttacking = true; currentState = BossState.MildAnger;
        OnStateChanged?.Invoke(currentState); // 🌟 补上这句！朝全场大喊状态变了！
        if (altar != null)
        {
            if (Random.value > 0.5f)
            {
                if (blueDrainLine != null)
                {
                    blueDrainLine.enabled = true;
                    blueDrainLine.SetPosition(0, transform.position);
                    blueDrainLine.SetPosition(1, altar.transform.position);
                }
                
                // 🌟🌟🌟 核心修复：绝对禁止越权修改！调用祭坛自己的 AddEnergy 方法！
                // 这样才会触发祭坛内部的 UI 广播！
                int drainAmount = Random.Range(mildDrainMin, mildDrainMax);
                altar.AddEnergy(-drainAmount); 
                
                yield return new WaitForSeconds(1.5f);
                if (blueDrainLine != null) blueDrainLine.enabled = false;
            }
            else
            {
                yield return StartCoroutine(DrawEnvLaserBeam(altar.transform.position));
                altar.TakeDamage(mildAngerAltarDamage);
            }
        }
        yield return new WaitForSeconds(3f);
        isAttacking = false;
    }

    private IEnumerator EnrageRoutine()
    {
        isAttacking = true; currentState = BossState.Enrage;
        OnStateChanged?.Invoke(currentState); // 🌟 补上这句！朝全场大喊状态变了！
                                              // 决定这次随机炸几块（1块或2块）
        int currentFloorsCount = AttributeFloor.ActiveEnergyFloors.Count;

        if (currentFloorsCount > 0)
        {
            int destroyCount = Mathf.Min(Random.Range(1, 3), currentFloorsCount);

            for (int i = 0; i < destroyCount; i++)
            {
                // 🚨 工业级防呆核心：因为每一次循环末尾都会调用 PermanentDestroy() 划掉一块地板，
                // 哈希表内的数量在动态减少！所以每一次循环开始，必须重新读取最新的“剩余数量”！
                int remainingCount = AttributeFloor.ActiveEnergyFloors.Count;
                if (remainingCount == 0) break; // 如果地板被炸光了，立刻安全跳出循环

                // 1. 摇号：在当前剩下的柜子数量里，摇出一个幸运序号（比如从0到remainingCount-1）
                int randomIndex = Random.Range(0, remainingCount);
                AttributeFloor target = null;

                // 2. 🌟 零分配（Zero Alloc）数数法：提着刷子顺着储物柜数数
                int counter = 0;
                foreach (var floor in AttributeFloor.ActiveEnergyFloors)
                {
                    if (counter == randomIndex)
                    {
                        target = floor; // 数到了！成功抓出这块地板
                        break;
                    }
                    counter++;
                }

                // 3. 安全把关，确保抓到的人不是空的
                if (target != null)
                {
                    // 轰击激光反馈（等待你的激光动画播完）
                    yield return StartCoroutine(DrawEnvLaserBeam(target.transform.position));

                    // 实施永久毁灭（这会触发地板内部的 ActiveEnergyFloors.Remove(this)，将自己从储物柜擦除）
                    target.PermanentDestroy();
                }
            }
        }
        if (altar != null) altar.TakeDamage(enrageAltarDamage);

        if (redLaserPrefab != null && player != null)
        {
            // 1. 秽土转生 4 根死线激光
            GameObject top = Instantiate(redLaserPrefab);
            GameObject bottom = Instantiate(redLaserPrefab);
            GameObject left = Instantiate(redLaserPrefab);
            GameObject right = Instantiate(redLaserPrefab);

            float trackTime = laserStretchTime + laserStayTime;
            float boxSize = 2.5f;

            // 🌟🌟🌟 核心高级重构：动态计算封闭所需的激光长度
            // 围栏半宽是 boxSize，总宽就是 boxSize * 2。我们再加上 1.0f，
            // 让四个角延伸超出一点点，在视觉上能形成一个极具工业设计感的“死线封闭牢笼”！
            float adaptiveLength = (boxSize * 2f) + 1.0f;

            // 统一赋予动态生长的数据（再也不用写死 9f 了，长宽随你面板里的 boxSize 自动缩放！）
            top.GetComponent<LaserTrap>()?.SetupBossLaser(adaptiveLength, laserStretchTime, laserStayTime, laserVanishTime);
            bottom.GetComponent<LaserTrap>()?.SetupBossLaser(adaptiveLength, laserStretchTime, laserStayTime, laserVanishTime);
            left.GetComponent<LaserTrap>()?.SetupBossLaser(adaptiveLength, laserStretchTime, laserStayTime, laserVanishTime);
            right.GetComponent<LaserTrap>()?.SetupBossLaser(adaptiveLength, laserStretchTime, laserStayTime, laserVanishTime);

            float elapsed = 0f;
            while (elapsed < trackTime && player != null)
            {
                Vector3 pPos = player.position;

                // ==============================================================
                // 🌟🌟🌟 纯正几何学微操：利用左端点进行强力合围 🌟🌟🌟
                // ==============================================================

                // 🟩 1. 天花板激光（顶边）：
                // 出生在【左上角】，朝右（Rotation.identity）横向平射
                top.transform.SetPositionAndRotation(
                    pPos + new Vector3(-boxSize, boxSize, 0),
                    Quaternion.identity
                );

                // 🟩 2. 地板激光（底边）：
                // 出生在【左下角】，朝右（Rotation.identity）横向平射
                bottom.transform.SetPositionAndRotation(
                    pPos + new Vector3(-boxSize, -boxSize, 0),
                    Quaternion.identity
                );

                // 🟩 3. 左侧激光墙（左边）：
                // 出生在【左下角】，原地往上转 90 度，笔直朝天发射，封死左路
                left.transform.SetPositionAndRotation(
                    pPos + new Vector3(-boxSize, -boxSize, 0),
                    Quaternion.Euler(0, 0, 90)
                );

                // 🟩 4. 右侧激光墙（右边）：
                // 出生在【右下角】，原地往上转 90 度，笔直朝天发射，封死右路
                right.transform.SetPositionAndRotation(
                    pPos + new Vector3(boxSize, -boxSize, 0),
                    Quaternion.Euler(0, 0, 90)
                );

                elapsed += Time.deltaTime;
                yield return null;
            }
        }
        yield return new WaitForSeconds(5f);
        isAttacking = false;
    }

    private IEnumerator DrawEnvLaserBeam(Vector3 targetPos)
    {
        if (redEnvLaserLine == null) yield break;

        redEnvLaserLine.enabled = true;
        redEnvLaserLine.positionCount = 2;
        redEnvLaserLine.SetPosition(0, transform.position);
        redEnvLaserLine.SetPosition(1, transform.position);

        float elapsed = 0f;
        float originalWidth = redEnvLaserLine.widthMultiplier;

        while (elapsed < laserStretchTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / laserStretchTime;
            float easeOutT = 1f - Mathf.Pow(1f - t, 4f);

            redEnvLaserLine.SetPosition(0, transform.position); 
            redEnvLaserLine.SetPosition(1, Vector3.Lerp(transform.position, targetPos, easeOutT));
            yield return null;
        }
        redEnvLaserLine.SetPosition(1, targetPos);

        float stayElapsed = 0f;
        while (stayElapsed < laserStayTime)
        {
            stayElapsed += Time.deltaTime;
            redEnvLaserLine.SetPosition(0, transform.position);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < laserVanishTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / laserVanishTime;
            redEnvLaserLine.SetPosition(0, Vector3.Lerp(transform.position, targetPos, t));
            redEnvLaserLine.widthMultiplier = Mathf.Lerp(originalWidth, 0f, t);
            yield return null;
        }

        redEnvLaserLine.enabled = false;
        redEnvLaserLine.widthMultiplier = originalWidth; 
    }

    public void ResetBoss()
    {
        StopAllCoroutines(); 
        isAttacking = false;
        CurrentHealth = MaxHealth; 
        currentState = BossState.Calm;

        if (blueDrainLine != null) blueDrainLine.enabled = false;
        if (redEnvLaserLine != null) redEnvLaserLine.enabled = false;
        
        // 恢复模型显示
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = true;
    }

    [ContextMenu("【调试】强制发射红光打祭坛")]
    public void TestFireRedLaser()
    {
        if (altar != null) StartCoroutine(DrawEnvLaserBeam(altar.transform.position));
    }
}