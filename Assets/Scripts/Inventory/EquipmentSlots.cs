using System;

/// <summary>
/// 装备槽数据容器（普通 C# 类，非 MonoBehaviour，便于单元测试）
/// 固定 3 个槽位（武器/防具/戒指），下标与 EquipmentType 枚举值一一对应；
/// 与 InventoryData 保持一致：只存 itemId（0 表示空槽），不持有 ItemData 引用，
/// 读档时再由物品库重新解析
/// </summary>
[Serializable]
public class EquipmentSlots
{
    /// <summary>槽位数量（武器/防具/戒指）</summary>
    public const int SlotCount = 3;

    /// <summary>槽位数据：下标 = (int)EquipmentType，0 表示空槽</summary>
    public int[] slots = new int[SlotCount];

    /// <summary>取槽位上的物品 ID（空槽返回 0）</summary>
    public int Get(EquipmentType type) => slots[(int)type];

    /// <summary>写入槽位（传 0 表示清空）</summary>
    public void Set(EquipmentType type, int itemId) => slots[(int)type] = itemId;

    /// <summary>是否为空槽</summary>
    public bool IsEmpty(EquipmentType type) => slots[(int)type] == 0;

    /// <summary>清空全部槽位</summary>
    public void Clear()
    {
        for (int i = 0; i < slots.Length; i++) slots[i] = 0;
    }
}