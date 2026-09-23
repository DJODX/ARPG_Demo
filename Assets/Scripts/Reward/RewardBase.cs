/// <summary>
/// 奖励基类：定义发放的标准方法，子类只关心"如何把这类奖励给到玩家"
/// 抽象类，不能直接使用；具体类型见 GoldReward / ExpReward / ItemReward
/// </summary>
public abstract class RewardBase : IReward
{
    /// <summary>奖励类型（只读，由子类固定）</summary>
    public abstract RewardType Type { get; }

    /// <summary>发放奖励（统一入口，由 RewardManager 调用）</summary>
    /// <param name="context">奖励上下文</param>
    /// <returns>是否发放成功</returns>
    public abstract bool Grant(RewardContext context);

    /// <summary>公共校验：接收者有效且数量为正</summary>
    protected static bool CanGrant(RewardContext context)
    {
        return context.receiver != null && context.entry.amount > 0;
    }
}