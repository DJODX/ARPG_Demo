using UnityEngine;

/// <summary>装备位：创建后不可修改，由具体子类通过 equipmentType 只读属性固定</summary>
public enum EquipmentType
{
    Weapon,
    Armor,
    Ring,
}

/// <summary>
/// 装备基类：物品大类固定为 Equipment
/// 抽象类，不能直接创建；具体类型见 WeaponData / ArmorData / RingData
/// </summary>
public abstract class EquipmentData : ItemData
{
    public override ItemType itemType => ItemType.Equipment;

    /// <summary>装备位（只读，创建后不可修改）</summary>
    public abstract EquipmentType equipmentType { get; }
}

/// <summary>武器装备：独有攻击力加成</summary>
[CreateAssetMenu(menuName = "ARPG/Items/Weapon", fileName = "Weapon")]
public class WeaponData : EquipmentData
{
    public override EquipmentType equipmentType => EquipmentType.Weapon;

    [Tooltip("攻击力加成")]
    public int itemDamage;
}

/// <summary>防具装备：独有防御力加成</summary>
[CreateAssetMenu(menuName = "ARPG/Items/Armor", fileName = "Armor")]
public class ArmorData : EquipmentData
{
    public override EquipmentType equipmentType => EquipmentType.Armor;

    [Tooltip("防御力加成")]
    public int itemDefense;
}

/// <summary>戒指装备：独有法力上限与暴击率加成</summary>
[CreateAssetMenu(menuName = "ARPG/Items/Ring", fileName = "Ring")]
public class RingData : EquipmentData
{
    public override EquipmentType equipmentType => EquipmentType.Ring;

    [Tooltip("法力上限加成")]
    public int mpBonus;

    [Tooltip("暴击率加成")]
    public float critBonus;
}
