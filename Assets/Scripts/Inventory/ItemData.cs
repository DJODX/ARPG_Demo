using UnityEngine;

/// <summary>物品大类：创建后不可修改，由具体子类通过 itemType 只读属性固定</summary>
public enum ItemType
{
    Material,
    Equipment,
    Consumable,
}

/// <summary>
/// 物品数据基类：所有物品配置的公共字段
/// 抽象类，不能直接创建；物品大类由子类固定，Inspector 中不显示、不可改
/// </summary>
public abstract class ItemData : ScriptableObject
{
    [Header("基础信息")]
    [Tooltip("物品唯一 ID")]
    public int itemID;

    [Tooltip("物品名称")]
    public string itemName;

    [Tooltip("物品图标")]
    public Sprite itemIcon;

    [Tooltip("堆叠上限（1 表示不可堆叠）")]
    public int itemMaxStack = 1;

    [Tooltip("物品售价")]
    public int itemPrice;

    /// <summary>物品大类（只读，创建后不可修改）</summary>
    public abstract ItemType itemType { get; }
}
