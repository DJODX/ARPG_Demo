using UnityEngine;

/// <summary>
/// 收集类目标：累计获得指定物品 requiredCount 个
/// 通过背包的"入包"事件推进，只统计新增获得的数量；
/// 丢弃/装备等离开背包的操作不会让进度回退（进度只增不减）
/// </summary>
[CreateAssetMenu(menuName = "ARPG/Quests/Objectives/Collect", fileName = "Obj_Collect")]
public class CollectObjectiveSO : QuestObjectiveSO
{
    [Tooltip("需要收集的物品")]
    public ItemData targetItem;

    public override bool OnItemAdded(QuestRuntime runtime, int index, int itemId, int count)
    {
        if (targetItem == null || itemId != targetItem.itemID) return false;
        return runtime.AddProgress(index, count);
    }
}