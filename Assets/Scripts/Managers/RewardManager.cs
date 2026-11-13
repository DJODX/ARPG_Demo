using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 奖励管理器（单例）
/// 职责：注册各类奖励策略、提供统一发放入口、广播发放结果
/// 策略模式：内部维护 RewardType → IReward 注册表，发放时按类型分发
/// 观察者：发放成功后触发 OnRewardGranted，供 UI 飘字等外部系统订阅
/// </summary>
public class RewardManager : MonoSingleton<RewardManager>
{
    /// <summary>类型 → 发放策略，运行时可通过 Register 覆盖（例如活动期间替换成双倍金币策略）</summary>
    private readonly Dictionary<RewardType, IReward> _registry = new Dictionary<RewardType, IReward>();

    /// <summary>奖励发放成功事件（参数：奖励类型、数量）</summary>
    public event Action<RewardType, int> OnRewardGranted;

    /// <summary>
    /// 敌人被玩家击杀事件（参数：玩家、被击杀的敌人）
    /// 由 RewardSource 确认击杀归属后主动上报，与是否配置了奖励无关，
    /// 供任务计数等外部系统订阅（不参与奖励发放）
    /// </summary>
    public event Action<PlayerProgression, GameObject> OnEnemyKilled;

    protected override void OnSingletonAwake()
    {
        // 内置策略：新增奖励类型时在此补充一行
        Register(new GoldReward());
        Register(new ExpReward());
        Register(new ItemReward());
    }

    /// <summary>注册（或替换）某个奖励类型的发放策略</summary>
    public void Register(IReward reward)
    {
        if (reward == null) return;
        _registry[reward.Type] = reward;
    }

    /// <summary>注销某个奖励类型的发放策略</summary>
    public void Unregister(RewardType type)
    {
        _registry.Remove(type);
    }

    /// <summary>查询某类型是否已注册策略</summary>
    public bool TryGet(RewardType type, out IReward reward)
    {
        return _registry.TryGetValue(type, out reward);
    }

    /// <summary>发放单个奖励</summary>
    /// <param name="entry">奖励配置</param>
    /// <param name="receiver">奖励接收者</param>
    /// <param name="victim">奖励来源（被击杀的敌人）</param>
    public bool Grant(RewardEntry entry, PlayerProgression receiver, GameObject victim)
    {
        if (!_registry.TryGetValue(entry.type, out IReward reward))
        {
            Debug.LogWarning($"[Reward] 未注册的奖励类型：{entry.type}");
            return false;
        }

        var context = new RewardContext
        {
            receiver = receiver,
            victim = victim,
            entry = entry
        };

        if (!reward.Grant(context)) return false;

        OnRewardGranted?.Invoke(entry.type, entry.amount);
        return true;
    }

    /// <summary>批量发放（击杀掉落），返回成功发放的条数</summary>
    public int GrantAll(IReadOnlyList<RewardEntry> entries, PlayerProgression receiver, GameObject victim)
    {
        if (entries == null || receiver == null) return 0;

        int granted = 0;
        for (int i = 0; i < entries.Count; i++)
        {
            if (Grant(entries[i], receiver, victim)) granted++;
        }
        return granted;
    }

    /// <summary>
    /// 上报一次"玩家击杀敌人"（供任务系统等订阅）
    /// 单独成方法而不放在 GrantAll 里：任务完成奖励同样走 GrantAll，
    /// 若在那里广播会被误认为一次击杀
    /// </summary>
    /// <param name="receiver">击杀者（玩家）</param>
    /// <param name="victim">被击杀的敌人</param>
    public void NotifyEnemyKilled(PlayerProgression receiver, GameObject victim)
    {
        OnEnemyKilled?.Invoke(receiver, victim);
    }
}