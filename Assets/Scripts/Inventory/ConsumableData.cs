using UnityEngine;

/// <summary>
/// 消耗品基类：公共字段（使用冷却等）
/// 抽象类，不能直接创建；具体类型见 HealConsumableData / BuffConsumableData。
/// 用继承代替类型枚举：每种消耗品一个类，Inspector 只显示该类独有的字段
/// </summary>
public abstract class ConsumableData : ItemData
{
    [Tooltip("使用冷却（秒）")]
    public float cooldown;
}

/// <summary>恢复类消耗品：回复生命/法力</summary>
[CreateAssetMenu(menuName = "ARPG/Items/HealConsumable", fileName = "HealConsumable")]
public class HealConsumableData : ConsumableData
{
    [Tooltip("生命回复量")]
    public int itemHeal;

    [Tooltip("法力回复量")]
    public int itemMana;
}

/// <summary>增益类消耗品：临时状态加成</summary>
[CreateAssetMenu(menuName = "ARPG/Items/BuffConsumable", fileName = "BuffConsumable")]
public class BuffConsumableData : ConsumableData
{
    [Tooltip("移动速度加成")]
    public int itemSpeed;

    [Tooltip("持续时长（秒）")]
    public float duration;
}
