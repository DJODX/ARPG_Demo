using System.Collections;
using UnityEngine;

/// <summary>
/// 状态基类：持有兽人引用，提供默认实现，只重写需要的方法，避免样板代码
/// </summary>
public abstract class EnemyStateBase : IEnemyState
{
    protected readonly OrcishControl orc;
    protected EnemyFSM fsm => orc.Fsm;

    protected EnemyStateBase(OrcishControl orc)
    {
        this.orc = orc;
    }

    public abstract void Enter();
    public virtual void Update() { }
    public virtual void Exit() { }

    /// <summary>默认允许切换；需要限制的状态（攻击/死亡）自行重写</summary>
    public virtual bool CanExit(EnemyState toState) => true;
}

/// <summary>
/// 待机：静止，定期检测玩家；玩家进入检测范围（默认7米）后切换追击
/// </summary>
public class IdleState : EnemyStateBase
{
    private float _checkTimer;

    public IdleState(OrcishControl orc) : base(orc) { }

    public override void Enter()
    {
        orc.StopMoving();
        _checkTimer = 0f;
    }

    public override void Update()
    {
        // 定期检测，而非每帧检测
        _checkTimer += Time.deltaTime;
        if (_checkTimer < orc.detectInterval) return;
        _checkTimer = 0f;

        if (orc.HasPlayer && orc.IsPlayerInDetectRange())
        {
            fsm.SwitchState(EnemyState.Chase);
        }
    }
}

/// <summary>
/// 追击：NavMesh 追踪玩家；进入攻击范围→攻击；玩家超出检测范围→回待机
/// </summary>
public class ChaseState : EnemyStateBase
{
    public ChaseState(OrcishControl orc) : base(orc) { }

    public override void Enter()
    {
        orc.ResumeMoving();
    }

    public override void Update()
    {
        // 玩家丢失或超出7米检测范围 → 回待机
        if (!orc.HasPlayer || !orc.IsPlayerInDetectRange())
        {
            fsm.SwitchState(EnemyState.Idle);
            return;
        }

        orc.FacePlayer();
        orc.MoveToPlayer();

        // 进入攻击范围 → 攻击
        if (orc.IsPlayerInAttackRange())
        {
            fsm.SwitchState(EnemyState.Attack);
        }
    }
}

/// <summary>
/// 攻击：播放攻击动画并禁止移动；只有攻击动画播放完成（动画事件）才能切换
/// </summary>
public class AttackState : EnemyStateBase
{
    private bool _attackFinished;
    private bool _switching;
    private float _elapsed;
    private bool _hitboxOpened;
    private float _hitboxTimer;

    public AttackState(OrcishControl orc) : base(orc) { }

    public override void Enter()
    {
        orc.EnterAttackMove(); // 攻击中可移动，速度降为一半
        _attackFinished = false;
        _switching = false;
        _elapsed = 0f;
        _hitboxOpened = false;
        _hitboxTimer = 0f;
        orc.Attack();
    }

    public override void Update()
    {
        _elapsed += Time.deltaTime;
        orc.FacePlayer();
        orc.MoveToPlayer(); // 攻击中保持减速追击

        // 进入攻击状态后延迟 weaponHitboxDelay 秒再开启武器判定（避免起手帧就命中）
        if (!_hitboxOpened)
        {
            _hitboxTimer += Time.deltaTime;
            if (_hitboxTimer >= orc.weaponHitboxDelay)
            {
                orc.EnableWeaponHitbox();
                _hitboxOpened = true;
            }
        }

        // 安全兜底：动画事件未配置时防止卡死在攻击状态（避免逻辑死锁）
        if (!_attackFinished && orc.attackTimeout > 0f && _elapsed >= orc.attackTimeout)
        {
            _attackFinished = true;
        }
        if (!_attackFinished || _switching) return;

        // 攻击完成：根据玩家位置决定回追击或待机
        _switching = true;
        if (orc.HasPlayer && orc.IsPlayerInDetectRange())
        {
            fsm.SwitchState(EnemyState.Chase);
        }
        else
        {
            fsm.SwitchState(EnemyState.Idle);
        }
    }

    /// <summary>退出攻击：关闭武器判定并恢复基础移动速度</summary>
    public override void Exit()
    {
        orc.DisableWeaponHitbox(); // 离开攻击状态：关闭武器命中判定
        orc.ExitAttackMove();
    }

    /// <summary>条件判断：只有攻击动画播放完成才允许离开攻击状态</summary>
    public override bool CanExit(EnemyState toState) => _attackFinished;

    /// <summary>由攻击动画最后一帧的 AnimationEvent（OnAttackAnimEnd）调用</summary>
    public void OnAttackFinished() => _attackFinished = true;
}

/// <summary>
/// 受伤：通过 ForceSwitchState 强制进入（无论当前状态）；播放受伤动画并短暂无敌；
/// 动画结束后恢复之前状态（死亡除外）
/// </summary>
public class HurtState : EnemyStateBase
{
    public HurtState(OrcishControl orc) : base(orc) { }

    public override void Enter()
    {
        orc.StopMoving();
        orc.PlayAnim("Hurt");
        orc.SetInvincible(true);
        orc.StartCoroutine(EndHurt(orc.hurtDuration));
    }

    public override void Exit()
    {
        orc.SetInvincible(false);
    }

    private IEnumerator EndHurt(float duration)
    {
        yield return new WaitForSeconds(duration);

        // 恢复之前状态；若期间已死亡，SwitchState 因 IsDead 自动拒绝
        fsm.RevertToPreviousState();
    }
}

/// <summary>
/// 死亡：最高优先级，任何状态下可强制触发；禁用移动与碰撞、播放死亡动画；
/// 终结状态，不可再切换
/// </summary>
public class DeathState : EnemyStateBase
{
    public DeathState(OrcishControl orc) : base(orc) { }

    public override void Enter()
    {
        orc.Die();
    }

    public override bool CanExit(EnemyState toState) => false;
}
