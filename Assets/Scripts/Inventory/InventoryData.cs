using System;
using System.Collections.Generic;

/// <summary>
/// 背包格子数据（仅存 itemId + 数量，不持有 ItemData 引用）
/// 普通可序列化类，配合 List 使用，JsonUtility 可直接序列化
/// </summary>
[Serializable]
public class ItemStack
{
    public int itemId;
    public int count;
}

/// <summary>
/// 背包数据容器（普通 C# 类，非 MonoBehaviour，便于单元测试）
/// 仅存 itemId + 数量，读档时由外部 ItemDatabase 重新解析成 ItemData
/// </summary>
[Serializable]
public class InventoryData
{
    public const int DefaultCapacity = 24;   // 背包格子数
    public const int DefaultMaxStack = 99;   // 堆叠上限兜底

    /// <summary>格子数据：列表长度 = 已占用格子数（不预分配空位）</summary>
    public List<ItemStack> slots = new List<ItemStack>();

    /// <summary>堆叠上限解析器（由外部注入，如 ItemDatabase；null 时用 DefaultMaxStack）</summary>
    [NonSerialized] public Func<int, int> maxStackResolver;

    public int Capacity => DefaultCapacity;
    public int ItemCount => slots.Count;

    private int GetMaxStack(int itemId)
    {
        if (maxStackResolver != null)
        {
            int max = maxStackResolver(itemId);
            if (max > 0) return max;
        }
        return DefaultMaxStack;
    }

    /// <summary>查询某物品总数量</summary>
    public int CountItem(int itemId)
    {
        int total = 0;
        foreach (var s in slots)
        {
            if (s.itemId == itemId) total += s.count;
        }
        return total;
    }

    public bool HasItem(int itemId) => CountItem(itemId) > 0;

    /// <summary>
    /// 添加物品：优先堆叠到已有未满格，剩余放入空格
    /// 空格不足时返回 false（已加入的部分不回滚）
    /// </summary>
    public bool AddItem(int itemId, int count)
    {
        if (count <= 0) return true;
        int max = GetMaxStack(itemId);
        int remaining = count;

        // 1) 优先堆叠到已有未满的同类格
        for (int i = 0; i < slots.Count && remaining > 0; i++)
        {
            var s = slots[i];
            if (s.itemId != itemId || s.count >= max) continue;
            int space = max - s.count;
            int put = Math.Min(space, remaining);
            s.count += put;
            remaining -= put;
        }

        // 2) 剩余放入空格（受背包容量限制）
        while (remaining > 0 && slots.Count < DefaultCapacity)
        {
            int put = Math.Min(max, remaining);
            slots.Add(new ItemStack { itemId = itemId, count = put });
            remaining -= put;
        }
        return remaining == 0;
    }

    /// <summary>
    /// 移除物品：从后往前扣减，归零的格子直接移除
    /// 数量不足时返回 false（不会扣成负数）
    /// </summary>
    public bool RemoveItem(int itemId, int count)
    {
        if (count <= 0) return true;
        int remaining = count;
        for (int i = slots.Count - 1; i >= 0 && remaining > 0; i--)
        {
            var s = slots[i];
            if (s.itemId != itemId) continue;
            int take = Math.Min(s.count, remaining);
            s.count -= take;
            remaining -= take;
            if (s.count <= 0) slots.RemoveAt(i);
        }
        return remaining == 0;
    }

    public void Clear() => slots.Clear();
}
