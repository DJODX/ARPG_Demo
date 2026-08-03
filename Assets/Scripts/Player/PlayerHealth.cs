using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using StarterAssets;

/// <summary>
/// 玩家健康组件：实现 IDamageable，统一处理玩家受伤/死亡逻辑
/// 依赖 AttributeComponent 管理血量（数据与事件），本组件负责行为与表现：
/// 受伤：短暂无敌 + 播放受伤动画 + 击退 + 音效，硬直期间禁止移动/攻击/跳跃
/// 死亡：禁用输入与移动、播放死亡动画
/// 挂载要求：CharacterController、Animator、AttributeComponent（均为可选，缺失时自动跳过对应逻辑）
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AttributeComponent))]
public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("受伤配置")]
    [Tooltip("受伤硬直/无敌时间（秒）")]
    public float hurtDuration = 0.5f;

    [Tooltip("受伤击退强度（0 表示不击退）")]
    public float knockbackForce = 5f;

    private AttributeComponent _attribute;
    private Animator _animator;
    private CharacterController _controller;
    private StarterAssetsInputs _input;

    private readonly HashSet<string> _animParams = new HashSet<string>();
    private bool _invincible;
    private Vector3 _knockbackVelocity;

    /// <summary>是否已死亡（委托给 AttributeComponent）</summary>
    public bool IsDead => _attribute != null && _attribute.IsDead;

    /// <summary>是否处于受伤硬直中（供 ThirdPersonController 判断禁用移动/攻击/跳跃）</summary>
    public bool IsHurt { get; private set; }

    private void Awake()
    {
        _attribute = GetComponent<AttributeComponent>();
        _animator = GetComponent<Animator>();
        _controller = GetComponent<CharacterController>();
        _input = GetComponent<StarterAssetsInputs>();

        CacheAnimParams();
        if (_attribute != null) _attribute.OnDeath += HandleDeath;
    }

    private void Update()
    {
        // 受击击退：沿击退方向推动角色并随时间衰减（硬直期间 Move() 已禁用，两者互不冲突）
        if (_knockbackVelocity.sqrMagnitude > 0.01f)
        {
            if (_controller != null && _controller.enabled)
            {
                _controller.Move(_knockbackVelocity * Time.deltaTime);
            }
            _knockbackVelocity = Vector3.Lerp(_knockbackVelocity, Vector3.zero, 8f * Time.deltaTime);
        }
    }

    /// <summary>
    /// 实现 IDamageable：受伤入口（由攻击方 HitboxDetector 调用）
    /// </summary>
    public void TakeDamage(DamageInfo info)
    {
        if (_attribute == null || _attribute.IsDead || _invincible) return;

        _attribute.TakeDamage(info);
        Debug.Log($"TakeDamage: {info.amount} from {_attribute.CurrentHp}"); 
        // 未死亡 → 进入受伤硬直；死亡由 AttributeComponent.OnDeath → HandleDeath 处理
        if (!_attribute.IsDead)
        {
            StartCoroutine(HurtSequence(info));
        }
    }

    /// <summary>受伤流程：硬直 + 无敌 → 播放动画/击退/音效 → 结束后恢复</summary>
    private IEnumerator HurtSequence(DamageInfo info)
    {
        IsHurt = true;
        _invincible = true;

        // 受伤动画
        SetTriggerSafe("Hurt");

        // 击退（沿击退方向水平推动）
        if (knockbackForce > 0f && info.hitDirection.sqrMagnitude > 0.01f)
        {
            Vector3 dir = info.hitDirection;
            dir.y = 0f;
            _knockbackVelocity = dir.normalized * knockbackForce;
        }

        // 受伤音效
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFXAtPoint(AudioManager.Instance.hitReceivedClip, transform.position);
        }

        yield return new WaitForSeconds(hurtDuration);

        IsHurt = false;
        _invincible = false;
    }

    /// <summary>死亡处理：禁用输入与移动、播放死亡动画（由 AttributeComponent.OnDeath 触发）</summary>
    private void HandleDeath()
    {
        IsHurt = false;
        _invincible = true;
        _knockbackVelocity = Vector3.zero;

        // 禁用输入，防止死亡后仍可移动/攻击
        if (_input != null)
        {
            _input.move = Vector2.zero;
            _input.look = Vector2.zero;
            _input.jump = false;
            _input.sprint = false;
            _input.attack = false;
            _input.block = false;
        }

        // 清除正在播放的脚步声等世界空间音效
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopSpatialSFX();
        }

        // 播放死亡动画
        SetTriggerSafe("Die");

        // 禁用角色控制器，停止移动与碰撞响应
        if (_controller != null) _controller.enabled = false;
    }

    // ================= 工具 =================

    private void CacheAnimParams()
    {
        if (_animator == null) return;
        foreach (var p in _animator.parameters)
        {
            _animParams.Add(p.name);
        }
    }

    /// <summary>安全触发 Trigger（参数未配置时不报错）</summary>
    private void SetTriggerSafe(string name)
    {
        if (_animator != null && _animParams.Contains(name))
        {
            _animator.SetTrigger(name);
        }
    }
}
