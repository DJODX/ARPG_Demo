using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 武器命中检测器
/// 挂在武器物体上（武器自身无 Animator，无法直接接收动画事件）。
/// hitboxCollider 为武器自身或其子物体上的触发碰撞体，
/// 命中判定窗口由持有 Animator 的角色控制器转发动画事件来开关，
/// 在 OnTriggerEnter 中向 IDamageable 施加伤害
/// </summary>
[RequireComponent(typeof(Collider))]
public class HitboxDetector : MonoBehaviour
{
    [Tooltip("命中判定碰撞体（武器子对象上的 Collider，需勾选 IsTrigger）")]
    public Collider hitboxCollider;

    /// <summary>攻击者自身的碰撞体（避免打到自己）</summary>
    private Collider _ownerCollider;

    /// <summary>攻击者的属性组件（读取攻击力与暴击率）</summary>
    private AttributeComponent _ownerAttribute;

    /// <summary>本次攻击已命中的目标，防止同一目标被重复判定</summary>
    private readonly HashSet<IDamageable> _alreadyHit = new HashSet<IDamageable>();

    [Tooltip("兜底伤害值：仅当攻击者没有 AttributeComponent 时使用")]
    public float fallbackDamage = 10f;

    private void Awake()
    {
        // 未手动指定时取自身 Collider
        if (hitboxCollider == null)
        {
            hitboxCollider = GetComponent<Collider>();
        }

        // 获取攻击者自身的碰撞体与属性组件（挂在实体或其父级）
        _ownerAttribute = GetComponentInParent<AttributeComponent>();

        // 默认禁用，等待动画事件开启
        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = false;
        }
    }

    /// <summary>
    /// 由 AnimationEvent 调用：开启命中判定（攻击动画的关键判定帧）
    /// </summary>
    public void EnableHitbox()
    {
        if (hitboxCollider == null) return;
        hitboxCollider.enabled = true;
        _alreadyHit.Clear(); // 每次攻击重新清空，允许下一次攻击重新命中
    }

    /// <summary>
    /// 由 AnimationEvent 调用：关闭命中判定（判定窗口结束）
    /// </summary>
    public void DisableHitbox()
    {
        if (hitboxCollider == null) return;
        hitboxCollider.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hitboxCollider == null) return;

        // 忽略攻击者自身
        if (_ownerCollider != null && other == _ownerCollider) return;

        // 常开模式下武器可能与自身模型重叠：忽略同一实体（祖先/后代）内的碰撞体，防止自伤
        if (other.transform.IsChildOf(transform) || transform.IsChildOf(other.transform)) return;

        // 目标必须实现 IDamageable
        var damageable = other.GetComponentInParent<IDamageable>();
        if (damageable == null) return;

        // 防止同一目标在一次攻击中被多次命中
        if (!_alreadyHit.Add(damageable)) return;

        // 伤害来源：优先使用攻击者的攻击力与暴击率，缺失时用兜底值
        float rawDamage = _ownerAttribute != null ? _ownerAttribute.atk : fallbackDamage;
        bool isCrit = _ownerAttribute != null && Random.value < _ownerAttribute.critRate;

        damageable.TakeDamage(new DamageInfo
        {
            amount = rawDamage,
            source = gameObject,
            hitDirection = (other.transform.position - transform.position).normalized,
            isCrit = isCrit
        });
    }

    private void OnTriggerExit(Collider other)
    {
        // 目标离开判定范围后清除已命中记录，允许常开武器再次接触时重复造成伤害
        var damageable = other.GetComponentInParent<IDamageable>();
        if (damageable != null) _alreadyHit.Remove(damageable);
    }
    
}
