using System;
using UnityEngine;

/// <summary>
/// 装备管理器（全局单例）
/// 持有 EquipmentSlots（纯数据层），负责装备/卸下的类型校验与替换逻辑；
/// 装备变化通过 OnEquipmentChanged 通知 UI——UI 只订阅事件刷新，不直接操作数据层
/// </summary>
public class EquipmentManager : MonoSingleton<EquipmentManager>
{
    private EquipmentSlots _data;

    /// <summary>装备数据（只读访问）</summary>
    public EquipmentSlots Data => _data;

    /// <summary>装备变化事件（UI 订阅刷新）</summary>
    public event Action OnEquipmentChanged;

    protected override void OnSingletonAwake()
    {
        _data = new EquipmentSlots();
    }

    /// <summary>取某槽位已装备的物品配置（空槽或物品库未注册时返回 null）</summary>
    public ItemData GetEquipped(EquipmentType type)
    {
        int itemId = _data.Get(type);
        return itemId == 0 ? null : InventoryManager.Instance.GetItem(itemId);
    }

    /// <summary>
    /// 把背包中指定序号的装备放入目标槽位。
    /// 校验：格子有物品 → 是装备 → 装备类型与槽位匹配；
    /// 替换：槽位已有装备时放回背包，放不下则回滚（不会丢失物品）
    /// </summary>
    /// <param name="backpackIndex">背包格子序号</param>
    /// <param name="slotType">目标装备槽位类型</param>
    /// <returns>是否装备成功</returns>
    public bool EquipFromBackpack(int backpackIndex, EquipmentType slotType)
    {
        InventoryManager inventory = InventoryManager.Instance;
        ItemStack stack = inventory.GetStack(backpackIndex);
        if (stack == null) return false;

        // 校验一：必须是装备类物品
        EquipmentData equipment = inventory.GetItem(stack.itemId) as EquipmentData;
        if (equipment == null)
        {
            Debug.LogWarning($"[Equipment] 物品 {stack.itemId} 不是装备，无法放入装备栏");
            return false;
        }

        // 校验二：装备类型必须与槽位匹配（武器不能放进戒指位）
        if (equipment.equipmentType != slotType)
        {
            Debug.LogWarning($"[Equipment] 类型不匹配：{equipment.equipmentType} 无法放入 {slotType} 槽位");
            return false;
        }

        int oldItemId = _data.Get(slotType);

        // 1) 先从背包移除新装备（数量为 1 时会腾出一个格子）
        if (!inventory.RemoveAt(backpackIndex, 1))
        {
            Debug.LogWarning("[Equipment] 从背包移除装备失败");
            return false;
        }

        // 2) 替换：槽位已有旧装备则放回背包，放不下则整体回滚
        if (oldItemId != 0 && !inventory.AddItem(oldItemId, 1))
        {
            inventory.AddItem(stack.itemId, 1);   // 回滚：把新装备放回背包
            Debug.LogWarning("[Equipment] 背包已满，无法替换装备");
            return false;
        }

        // 3) 写入槽位
        _data.Set(slotType, equipment.itemID);
        OnEquipmentChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// 卸下某槽位装备并放回背包（背包满时失败，装备保留在槽位）
    /// </summary>
    public bool Unequip(EquipmentType slotType)
    {
        int itemId = _data.Get(slotType);
        if (itemId == 0) return false;

        // 先尝试放回背包，成功后再清空槽位，避免物品丢失
        if (!InventoryManager.Instance.AddItem(itemId, 1))
        {
            Debug.LogWarning("[Equipment] 背包已满，无法卸下装备");
            return false;
        }

        _data.Set(slotType, 0);
        OnEquipmentChanged?.Invoke();
        return true;
    }
}