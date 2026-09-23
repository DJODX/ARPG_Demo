using UnityEngine;

/// <summary>奖励类型：新增类型时在此扩展，并在 RewardManager 中注册对应策略</summary>
public enum RewardType
{
    Gold,
    Exp,
    Item,
}

/// <summary>
/// 奖励配置行：描述"发什么、发多少"
/// 只承载数据，具体如何发放由对应类型的策略决定
/// </summary>
[System.Serializable]
public struct RewardEntry
{
    [Tooltip("奖励类型")]
    public RewardType type;

    [Tooltip("数量（金币/经验为数值，道具为个数）")]
    [Min(1)] public int amount;

    [Tooltip("仅道具/装备奖励需要指定，其他类型留空")]
    public ItemData item;
}

/// <summary>
/// 奖励上下文：一次发放所需的运行时数据
/// 作为 Grant 的统一参数，使奖励实现不依赖具体调用方
/// </summary>
public struct RewardContext
{
    /// <summary>奖励接收者（玩家）</summary>
    public PlayerProgression receiver;

    /// <summary>奖励来源（被击杀的敌人），供后续掉落表现与统计使用</summary>
    public GameObject victim;

    /// <summary>本次要发放的配置行</summary>
    public RewardEntry entry;
}

/// <summary>
/// 奖励接口：所有奖励类型的统一抽象
/// 新增奖励类型只需继承 RewardBase 并在 RewardManager 注册，无需改动发放入口
/// </summary>
public interface IReward
{
    /// <summary>奖励类型（注册表中的键）</summary>
    RewardType Type { get; }

    /// <summary>发放奖励（标准方法）</summary>
    /// <param name="context">奖励上下文</param>
    /// <returns>是否发放成功</returns>
    bool Grant(RewardContext context);
}