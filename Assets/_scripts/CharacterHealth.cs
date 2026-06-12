using UnityEngine;
using System;

public class CharacterHealth : MonoBehaviour
{
    [Header("🌟 数据驱动配置")]
    [SerializeField] protected BaseStatsData statsAsset; // 拖入对应的 SO 文件

    // 核心广播：UI 或其他系统订阅
    public event Action<int, int> OnHealthChanged;

    // 内部使用的当前血量（如果是小怪，就用这个；如果是玩家，可以重写指向 SO）
    protected int _currentHealth;

    // 属性封装，方便外部只读，且内部修改时自动触发广播
    public virtual int CurrentHealth
    {
        get => _currentHealth;
        set
        {
            _currentHealth = Mathf.Clamp(value, 0, MaxHealth);
            OnHealthChanged?.Invoke(_currentHealth, MaxHealth);
        }
    }

    public virtual int MaxHealth => statsAsset != null ? statsAsset.maxHealth : 100;

    protected virtual void Awake()
    {
        // 健壮性检查
        if (statsAsset == null)
        {
            Debug.LogWarning($"{gameObject.name} 缺失 StatsAsset 数据文件！使用默认值。");
        }
    }

    protected virtual void Start()
    {
        // 初始化血量：从数据模板读取
        if (statsAsset != null)
        {
            _currentHealth = statsAsset.maxHealth;
        }
        else
        {
            _currentHealth = 100;
        }

        // 初始广播
        OnHealthChanged?.Invoke(_currentHealth, MaxHealth);
    }

    public virtual void TakeDamage(int damage)
    {
        if (damage <= 0) return;

        CurrentHealth -= damage; // 使用属性赋值，自动触发广播

        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    public virtual void Heal(int amount)
    {
        if (amount <= 0) return;
        CurrentHealth += amount;
    }

    protected virtual void Die()
    {
        Debug.Log($"[死亡广播]: {gameObject.name} 已阵亡。");
    }
}