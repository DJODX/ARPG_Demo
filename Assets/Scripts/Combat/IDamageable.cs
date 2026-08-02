using UnityEngine;

/// <summary>
/// 伤害信息结构体
/// 封装一次伤害所需的全部数据，作为 TakeDamage 的统一参数
/// </summary>
public struct DamageInfo
{
    /// <summary>伤害数值（未经过防御减免）</summary>
    public float amount;

    /// <summary>伤害来源（攻击者 GameObject）</summary>
    public GameObject source;

    /// <summary>击退方向（单位向量）</summary>
    public Vector3 hitDirection;

    /// <summary>是否暴击</summary>
    public bool isCrit;
}

/// <summary>
/// 可受伤接口
/// 所有能承受伤害的对象（玩家/敌人/可破坏物）实现此接口，
/// 攻击方只依赖接口，不关心受击方具体类型
/// </summary>
public interface IDamageable
{
    /// <summary>是否已死亡</summary>
    bool IsDead { get; }

    /// <summary>
    /// 承受一次伤害
    /// </summary>
    /// <param name="info">伤害信息</param>
    void TakeDamage(DamageInfo info);
}
