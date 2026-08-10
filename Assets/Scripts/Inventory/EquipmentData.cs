using UnityEngine;

/// <summary>装备位（所有装备的公共维度，与具体类型无关）</summary>
public enum EquipmentType
{

    body,
    ring,
    Weapon,
}

/// <summary>
/// 装备基类：公共字段（装备位等）
/// 抽象类，不能直接创建；具体类型见 WeaponData / ArmorData / RingData。
/// 用继承代替类型枚举：每种装备类型一个类，Inspector 只显示该类独有的字段
/// </summary>
public abstract class EquipmentData : ItemData
{
    public EquipmentType equipmentType;
}

/// <summary>武器装备：武器独有字段</summary>
[CreateAssetMenu(menuName = "ARPG/Items/Weapon", fileName = "Weapon")]
public class WeaponData : EquipmentData
{
    public int itemDamage;
    private void OnEnable()
    {
        equipmentType = EquipmentType.Weapon;
    }
}

/// <summary>盔甲装备：盔甲独有字段</summary>
[CreateAssetMenu(menuName = "ARPG/Items/Armor", fileName = "Armor")]
public class ArmorData : EquipmentData
{
    public int itemDefense;
    private void OnEnable()
    {
        equipmentType = EquipmentType.body;
    }
}

/// <summary>饰品装备：饰品独有字段</summary>
[CreateAssetMenu(menuName = "ARPG/Items/Ring", fileName = "Ring")]
public class RingData : EquipmentData
{
    public int mpBonus;      // 法力上限加成
    public float critBonus;  // 暴击率加成
    private void OnEnable()
    {
        equipmentType = EquipmentType.ring;
    }
}
