using UnityEngine;

/// <summary>
/// 测试用敌人
/// 实现 IDamageable，配合 AttributeComponent 验证战斗闭环：
/// 攻击 → 掉血 → 血条变化 → 死亡 → 销毁
/// </summary>
[RequireComponent(typeof(AttributeComponent))]
public class SimpleEnemy : MonoBehaviour, IDamageable
{
    [Header("死亡后延迟销毁（秒）")]
    [Tooltip("给死亡动画/掉落特效留出播放时间")]
    public float destroyDelay = 2f;

    private AttributeComponent _attribute;
    private Collider _collider;

    /// <summary>是否已死亡</summary>
    public bool IsDead => _attribute != null && _attribute.IsDead;

    private void Awake()
    {
        _attribute = GetComponent<AttributeComponent>();
        _collider = GetComponent<Collider>();
    }

    private void OnEnable()
    {
        // 订阅死亡事件（每次启用时订阅，OnDisable 时取消，防止重复）
        if (_attribute != null)
        {
            _attribute.OnDeath += Die;
        }
    }

    private void OnDisable()
    {
        if (_attribute != null)
        {
            _attribute.OnDeath -= Die;
        }
    }

    /// <summary>
    /// 承受伤害：转发给属性组件，触发掉血与事件
    /// </summary>
    public void TakeDamage(DamageInfo info)
    {
        if (IsDead) return;

        _attribute.TakeDamage(info);

        // 测试辅助：Console 打印伤害，方便确认战斗闭环
        Debug.Log($"[SimpleEnemy] 受到伤害 {info.amount}，剩余血量 {_attribute.CurrentHp}/{_attribute.maxHp}");
    }

    /// <summary>
    /// 死亡处理：禁用碰撞防止尸体挡路，延迟销毁
    /// </summary>
    private void Die()
    {
        if (_collider != null)
        {
            _collider.enabled = false;
        }

        Debug.Log($"[SimpleEnemy] {name} 死亡");

        Destroy(gameObject, destroyDelay);
    }
}
