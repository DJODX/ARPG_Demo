using System;
using UnityEngine;

/// <summary>
/// 属性组件
/// 管理血量、攻击、防御等基础属性，提供受伤/回复/死亡的事件通知
/// 通过组合挂载到玩家、敌人等实体上（组合优于继承）
/// </summary>
public class AttributeComponent : MonoBehaviour
{
    [Header("基础属性")]
    [Tooltip("最大生命值")]
    public float maxHp = 100f;

    [Tooltip("攻击力")]
    public float atk = 10f;

    [Tooltip("防御力")]
    public float def = 5f;

    [Tooltip("暴击率 0~1")]
    [Range(0f, 1f)] public float critRate = 0.05f;

    [Tooltip("暴击倍率")]
    [Range(1f, 5f)] public float critMult = 1.5f;

    /// <summary>当前生命值（私有，只能通过 TakeDamage/Heal 修改）</summary>
    private float _currentHp;

    /// <summary>当前生命值</summary>
    public float CurrentHp => _currentHp;

    /// <summary>生命值百分比 0~1，供 UI 血条使用</summary>
    public float HpPercent => maxHp <= 0f ? 0f : _currentHp / maxHp;

    /// <summary>是否已死亡</summary>
    public bool IsDead => _currentHp <= 0f;

    /// <summary>生命值变化事件（参数为当前生命值），UI 监听刷新血条</summary>
    public event Action<float> OnHpChanged;

    /// <summary>死亡事件，供受击方自身（动画、掉落、禁用输入）监听</summary>
    public event Action OnDeath;

    private void Awake()
    {
        _currentHp = maxHp;
    }

    /// <summary>
    /// 承受伤害：应用伤害公式 → 扣血 → 触发事件 → 死亡检查
    /// </summary>
    /// <param name="info">伤害信息</param>
    public void TakeDamage(DamageInfo info)
    {
        if (IsDead) return; // 已死亡不再承受伤害

        // 伤害公式，含暴击判定（此处直接使用传入的 isCrit，由攻击方或外部判定）
        float finalDamage = DamageCalculator.Calculate(info.amount, def, info.isCrit, critMult);

        _currentHp = Mathf.Max(0f, _currentHp - finalDamage);
        OnHpChanged?.Invoke(_currentHp);

        if (_currentHp <= 0f)
        {
            OnDeath?.Invoke();
        }
    }

    /// <summary>
    /// 回复生命值，不会超过最大生命值
    /// </summary>
    /// <param name="amount">回复量</param>
    public void Heal(float amount)
    {
        if (IsDead) return;

        _currentHp = Mathf.Min(maxHp, _currentHp + amount);
        OnHpChanged?.Invoke(_currentHp);
    }

    /// <summary>
    /// 重新初始化生命值（复活或读档时调用）
    /// </summary>
    public void ResetHp()
    {
        _currentHp = maxHp;
        OnHpChanged?.Invoke(_currentHp);
    }
}
