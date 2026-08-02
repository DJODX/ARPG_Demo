using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 兽人控制：FSM 初始化与注册、玩家检测、NavMesh 移动、动画控制、伤害接口
/// 需挂载：NavMeshAgent、Animator、AttributeComponent（血量/受伤/死亡），
/// 并为玩家 GameObject 设置 "Player" 标签（或在 Inspector 手动指定 player）
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class OrcishControl : MonoBehaviour, IDamageable
{
    [Header("战斗配置")]
    [Tooltip("玩家检测范围（米）")]
    public float detectRange = 7f;
    [Tooltip("攻击范围（米）")]
    public float attackRange = 2f;
    [Tooltip("待机时定期检测玩家的间隔（秒）")]
    public float detectInterval = 0.5f;
    [Tooltip("受伤硬直/无敌时间（秒）")]
    public float hurtDuration = 0.5f;
    [Tooltip("攻击动画兜底超时（秒），动画事件缺失时防止卡死，0=关闭")]
    public float attackTimeout = 3f;
    [Tooltip("攻击时移动速度倍率（0.5 = 减速到一半）")]
    public float attackMoveSpeedFactor = 0.5f;
    [Tooltip("死亡后销毁延迟（秒）")]
    public float destroyDelay = 5f;

    [Header("组件引用")]
    [Tooltip("玩家 Transform（不指定则按 Player 标签查找）")]
    public Transform player;

    private NavMeshAgent _agent;
    private Animator _animator;
    private AttributeComponent _attribute;
    private Collider[] _bodyColliders;
    private readonly HashSet<string> _animParams = new HashSet<string>();
    private float _baseSpeed;
    private bool _invincible;

    /// <summary>状态机（在 Start 中初始化并注册全部状态）</summary>
    public EnemyFSM Fsm { get; private set; }

    public bool HasPlayer => player != null;
    public bool IsDead => _attribute != null && _attribute.IsDead;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _animator = GetComponent<Animator>();
        _attribute = GetComponent<AttributeComponent>();
        _bodyColliders = GetComponentsInChildren<Collider>();
        CacheAnimParams();
        _baseSpeed = _agent != null ? _agent.speed : 0f;

        if (player == null) FindPlayer();
        if (_attribute != null) _attribute.OnDeath += HandleDeath;
    }

    private void Start()
    {
        try
        {
            Fsm = new EnemyFSM();
            Fsm.AddState(EnemyState.Idle, new IdleState(this));
            Fsm.AddState(EnemyState.Chase, new ChaseState(this));
            Fsm.AddState(EnemyState.Attack, new AttackState(this));
            Fsm.AddState(EnemyState.Hurt, new HurtState(this));
            Fsm.AddState(EnemyState.Death, new DeathState(this));
            Fsm.SwitchState(EnemyState.Idle);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Orc:{name}] Start 异常: {e}");
        }
    }

    private void Update()
    {
        Fsm?.UpdateState();
        UpdateAnimSpeed();
    }

    // ================= 玩家检测 =================

    private void FindPlayer()
    {
        var go = GameObject.FindGameObjectWithTag("Player");
        if (go != null) player = go.transform;
    }

    /// <summary>玩家是否在指定范围内（忽略高度差，等价于球形范围检测）</summary>
    private bool IsPlayerInRange(float range)
    {
        if (!HasPlayer) return false;
        var diff = player.position - transform.position;
        diff.y = 0f;
        return diff.sqrMagnitude <= range * range;
    }

    public bool IsPlayerInDetectRange() => IsPlayerInRange(detectRange);
    public bool IsPlayerInAttackRange() => IsPlayerInRange(attackRange);

    /// <summary>面向玩家（追击与攻击时保持朝向）</summary>
    public void FacePlayer()
    {
        if (!HasPlayer) return;
        var diff = player.position - transform.position;
        diff.y = 0f;
        if (diff.sqrMagnitude < 0.001f) return;
        transform.rotation = Quaternion.LookRotation(diff);
    }

    // ================= 移动（NavMesh） =================

    /// <summary>寻路追踪玩家</summary>
    public void MoveToPlayer()
    {
        if (_agent == null || !HasPlayer) return;
        _agent.isStopped = false;
        _agent.SetDestination(player.position);
    }

    public void ResumeMoving() { if (_agent != null) _agent.isStopped = false; }
    public void StopMoving() { if (_agent != null) _agent.isStopped = true; }

    /// <summary>进入攻击移动：恢复寻路并将移动速度降为 attackMoveSpeedFactor 倍</summary>
    public void EnterAttackMove()
    {
        if (_agent == null) return;
        _agent.isStopped = false;
        _agent.speed = _baseSpeed * attackMoveSpeedFactor;
    }

    /// <summary>退出攻击移动：恢复基础移动速度</summary>
    public void ExitAttackMove()
    {
        if (_agent == null) return;
        _agent.speed = _baseSpeed;
    }

    /// <summary>用移动速度驱动 Animator 的 Speed 参数（Idle/移动动画混合）</summary>
    private void UpdateAnimSpeed()
    {
        if (_animator == null || _agent == null || !_animParams.Contains("Speed")) return;
        _animator.SetFloat("Speed", _agent.velocity.magnitude, 0.1f, Time.deltaTime);
    }

    // ================= 动画 =================

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

    /// <summary>播放指定 Trigger 动画（用于受伤）</summary>
    public void PlayAnim(string triggerName) => SetTriggerSafe(triggerName);

    /// <summary>发起攻击（播放攻击动画）</summary>
    public void Attack() => SetTriggerSafe("Attack");

    /// <summary>由攻击动画最后一帧的 AnimationEvent 调用：标记攻击完成</summary>
    public void OnAttackAnimEnd()
    {
        (Fsm.CurrentState as AttackState)?.OnAttackFinished();
    }

    // ================= 受伤 / 死亡 =================

    public void SetInvincible(bool value) => _invincible = value;

    /// <summary>实现 IDamageable：受伤立即强制进入受伤状态（优先级最高）</summary>
    public void TakeDamage(DamageInfo info)
    {
        if (_attribute == null || _attribute.IsDead || _invincible) return;

        _attribute.TakeDamage(info);

        // 未死亡 → 无论当前处于什么状态，立即强制进入受伤
        if (!_attribute.IsDead)
        {
            Fsm.ForceSwitchState(EnemyState.Hurt);
        }
        // 死亡由 AttributeComponent.OnDeath → HandleDeath 处理
    }

    private void HandleDeath()
    {
        Fsm.ForceSwitchState(EnemyState.Death);
    }

    /// <summary>死亡行为：禁用移动与碰撞、播放死亡动画、延迟销毁</summary>
    public void Die()
    {
        if (_agent != null)
        {
            _agent.isStopped = true;
            _agent.enabled = false;
        }
        foreach (var col in _bodyColliders)
        {
            col.enabled = false;
        }
        SetTriggerSafe("Die");
        Destroy(gameObject, destroyDelay);
    }
}
