using System.Collections.Generic;

/// <summary>敌人状态枚举</summary>
public enum EnemyState
{
    Idle,   // 待机
    Chase,  // 追击
    Hurt,   // 受伤
    Death,  // 死亡
    Attack, // 攻击
}

/// <summary>
/// 状态接口：所有状态必须实现生命周期回调与切换条件判断
/// </summary>
public interface IEnemyState
{
    /// <summary>进入状态（播放动画、初始化变量）</summary>
    void Enter();

    /// <summary>每帧更新（状态行为逻辑、切换判断）</summary>
    void Update();

    /// <summary>退出状态（清理状态数据）</summary>
    void Exit();

    /// <summary>条件判断：当前状态是否允许切换到目标状态</summary>
    bool CanExit(EnemyState toState);
}

/// <summary>
/// 敌人状态机
/// 支持状态注册/移除/切换、Enter/Update/Exit 生命周期回调、
/// 切换条件判断，以及受伤/死亡等紧急状态的强制切换
/// </summary>
public class EnemyFSM
{
    private readonly Dictionary<EnemyState, IEnemyState> _states = new Dictionary<EnemyState, IEnemyState>();

    /// <summary>当前状态实例</summary>
    public IEnemyState CurrentState { get; private set; }

    /// <summary>当前状态（枚举）</summary>
    public EnemyState CurrentStateKey { get; private set; }

    /// <summary>上一状态（受伤结束后用于恢复，死亡除外）</summary>
    public EnemyState PreviousStateKey { get; private set; }

    /// <summary>是否已死亡（死亡为终结状态，不可再切换）</summary>
    public bool IsDead { get; private set; }

    /// <summary>注册状态（重复注册忽略）</summary>
    public void AddState(EnemyState state, IEnemyState enemyState)
    {
        if (_states.ContainsKey(state)) return;
        _states.Add(state, enemyState);
    }

    /// <summary>移除状态（不能移除当前正在执行的状态）</summary>
    public void RemoveState(EnemyState state)
    {
        if (!_states.ContainsKey(state) || state == CurrentStateKey) return;
        _states.Remove(state);
    }

    /// <summary>
    /// 普通切换：经过合法性检查与条件判断（CanExit），失败返回 false
    /// </summary>
    public bool SwitchState(EnemyState toState)
    {
        if (IsDead) return false;                            // 死亡为终结状态，禁止再切换
        if (CurrentState == null)                            // 尚未初始化任何状态 → 直接进入
        {
            PerformSwitch(toState);
            return true;
        }
        if (toState == CurrentStateKey) return true;         // 已在目标状态
        if (!_states.ContainsKey(toState)) return false;     // 目标状态未注册
        if (!CurrentState.CanExit(toState)) return false;    // 条件判断

        PerformSwitch(toState);
        return true;
    }

    /// <summary>
    /// 紧急强制切换：跳过条件判断，用于受伤/死亡等必须立即触发的场景
    /// </summary>
    public void ForceSwitchState(EnemyState toState)
    {
        if (!_states.ContainsKey(toState)) return;
        if (IsDead && toState != EnemyState.Death) return;   // 死亡后只能保持死亡
        if (toState == EnemyState.Death) IsDead = true;
        if (toState == CurrentStateKey && CurrentState != null) return; // 已在目标状态（受伤中再次受伤不重复进入）

        // 仅在真正发生切换时记录上一状态
        PreviousStateKey = CurrentStateKey;
        PerformSwitch(toState);
    }

    /// <summary>恢复受伤前状态（受伤动画结束后调用）</summary>
    public bool RevertToPreviousState()
    {
        return SwitchState(PreviousStateKey);
    }

    /// <summary>每帧驱动当前状态</summary>
    public void UpdateState()
    {
        CurrentState?.Update();
    }

    private void PerformSwitch(EnemyState toState)
    {
        CurrentState?.Exit();
        CurrentState = _states[toState];
        CurrentStateKey = toState;
        CurrentState.Enter();
    }
}
