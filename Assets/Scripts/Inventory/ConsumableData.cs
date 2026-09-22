using UnityEngine;

/// <summary>
/// 消耗品基类：物品大类固定为 Consumable，公共字段为使用冷却
/// 抽象类，不能直接创建；具体类型见 HealConsumableData / ManaConsumableData
/// </summary>
public abstract class ConsumableData : ItemData
{
    public override ItemType itemType => ItemType.Consumable;

    [Tooltip("使用冷却（秒）")]
    public float cooldown;
}

/// <summary>回血消耗品：仅恢复生命</summary>
[CreateAssetMenu(menuName = "ARPG/Items/HealthPotion", fileName = "HealthPotion")]
public class HealConsumableData : ConsumableData
{
    [Tooltip("生命回复量")]
    public int healAmount;
}

/// <summary>回蓝消耗品：仅恢复法力</summary>
[CreateAssetMenu(menuName = "ARPG/Items/ManaPotion", fileName = "ManaPotion")]
public class ManaConsumableData : ConsumableData
{
    [Tooltip("法力回复量")]
    public int manaAmount;
}
