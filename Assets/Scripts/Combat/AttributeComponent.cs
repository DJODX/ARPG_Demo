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
    [Tooltip("基础最大生命值（装备不影响）")]
    public float baseMaxHp = 100f;

    [Tooltip("基础攻击力")]
    public float baseAtk = 10f;

    [Tooltip("基础法力上限")]
    public float baseMaxMp = 10f;

    [Tooltip("基础防御力")]
    public float baseDef = 5f;

    [Tooltip("基础暴击率 0~1")]
    [Range(0f, 1f)] public float baseCritRate = 0.05f;

    [Tooltip("暴击倍率")]
    [Range(1f, 5f)] public float critMult = 1.5f;

    /// <summary>装备提供的加成（由 PlayerEquipmentStats 整体重算后覆盖写入，此处不做累加）</summary>
    private float _bonusAtk;
    private float _bonusDef;
    private float _bonusMaxMp;
    private float _bonusCritRate;

    /// <summary>最大生命值（基础值，装备不影响）</summary>
    public float maxHp => baseMaxHp;

    /// <summary>攻击力 = 基础值 + 装备加成</summary>
    public float atk => baseAtk + _bonusAtk;

    /// <summary>防御力 = 基础值 + 装备加成</summary>
    public float def => baseDef + _bonusDef;

    /// <summary>法力上限 = 基础值 + 装备加成</summary>
    public float maxMp => baseMaxMp + _bonusMaxMp;

    /// <summary>暴击率 = 基础值 + 装备加成（钳制在 0~1，防止超过 100%）</summary>
    public float critRate => Mathf.Clamp01(baseCritRate + _bonusCritRate);

    /// <summary>当前生命值（私有，只能通过 TakeDamage/Heal 修改）</summary>
    private float _currentHp;
    /// <summary>当前法力值（私有，只能通过 UseMp/RegenerateMp 修改）</summary>
    private float _currentMp;

    /// <summary>当前生命值</summary>
    public float CurrentHp => _currentHp;
    /// <summary>当前法力值</summary>
    public float CurrentMp => _currentMp;

    /// <summary>法力值百分比 0~1，供 UI 法力条使用</summary>
    public float MpPercent => maxMp <= 0f ? 0f : _currentMp / maxMp;

    /// <summary>生命值百分比 0~1，供 UI 血条使用</summary>
    public float HpPercent => maxHp <= 0f ? 0f : _currentHp / maxHp;

    /// <summary>是否已死亡</summary>
    public bool IsDead => _currentHp <= 0f;

    /// <summary>最后一次造成伤害的攻击者（用于击杀归属判定，如掉落奖励）</summary>
    public GameObject LastAttacker { get; private set; }

    /// <summary>生命值变化事件（参数为当前生命值），UI 监听刷新血条</summary>
    public event Action<float> OnHpChanged;
    /// <summary>法力值变化事件（参数为当前法力值），UI 监听刷新法力条</summary>
    public event Action<float> OnMpChanged;

    /// <summary>死亡事件，供受击方自身（动画、掉落、禁用输入）监听</summary>
    public event Action OnDeath;

    private void Awake()
    {
        _currentHp = maxHp;
        _currentMp = maxMp;
    }

    /// <summary>
    /// 承受伤害：应用伤害公式 → 扣血 → 触发事件 → 死亡检查
    /// </summary>
    /// <param name="info">伤害信息</param>
    public void TakeDamage(DamageInfo info)
    {
        if (IsDead) return; // 已死亡不再承受伤害

        // 记录攻击者，供击杀归属判定（掉落奖励等）使用
        LastAttacker = info.source;

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

    /// <summary>
    /// 设置装备加成（装备/卸下时由 PlayerEquipmentStats 汇总后调用）
    /// 采用覆盖写入而非累加，保证反复装备、读档重算都不会产生数值漂移
    /// </summary>
    /// <param name="atkBonus">攻击力加成</param>
    /// <param name="defBonus">防御力加成</param>
    /// <param name="maxMpBonus">法力上限加成</param>
    /// <param name="critRateBonus">暴击率加成</param>
    public void SetEquipmentBonus(float atkBonus, float defBonus, float maxMpBonus, float critRateBonus)
    {
        _bonusAtk = atkBonus;
        _bonusDef = defBonus;
        _bonusMaxMp = maxMpBonus;
        _bonusCritRate = critRateBonus;

        // 卸下加法力上限的装备后上限会变小，当前法力需钳制，否则 MpPercent 会超过 1
        if (_currentMp > maxMp)
        {
            _currentMp = maxMp;
            OnMpChanged?.Invoke(_currentMp);
        }
    }
}
