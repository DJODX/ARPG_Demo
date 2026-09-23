/// <summary>金币奖励：增加玩家金币</summary>
public class GoldReward : RewardBase
{
    public override RewardType Type => RewardType.Gold;

    public override bool Grant(RewardContext context)
    {
        if (!CanGrant(context)) return false;

        context.receiver.AddGold(context.entry.amount);
        return true;
    }
}

/// <summary>经验奖励：增加玩家经验</summary>
public class ExpReward : RewardBase
{
    public override RewardType Type => RewardType.Exp;

    public override bool Grant(RewardContext context)
    {
        if (!CanGrant(context)) return false;

        context.receiver.AddExp(context.entry.amount);
        return true;
    }
}

/// <summary>
/// 道具奖励：发放任意 ItemData（材料 / 装备 / 消耗品）
/// 装备是 ItemData 的子类，因此不需要单独的装备奖励类
/// </summary>
public class ItemReward : RewardBase
{
    public override RewardType Type => RewardType.Item;

    public override bool Grant(RewardContext context)
    {
        if (!CanGrant(context) || context.entry.item == null) return false;

        return InventoryManager.Instance.AddItem(context.entry.item.itemID, context.entry.amount);
    }
}