using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 背包管理器（全局单例）
/// 持有 InventoryData（纯数据层），对外提供增删/查询接口；
/// 数据变化通过 OnInventoryChanged 事件通知 UI——UI 只订阅事件刷新，不直接操作数据层
/// </summary>
public class InventoryManager : MonoSingleton<InventoryManager>
{
    [Header("物品数据库")]
    [Tooltip("全局物品配置（建议放到场景中的管理器物体上；运行时按 itemID 查找物品与堆叠上限）")]
    public List<ItemData> itemDatabase = new List<ItemData>();

    private InventoryData _data;
    private readonly Dictionary<int, ItemData> _lookup = new Dictionary<int, ItemData>();

    /// <summary>背包数据（只读访问）</summary>
    public InventoryData Data => _data;

    /// <summary>背包数据变化事件（UI 订阅刷新）</summary>
    public event Action OnInventoryChanged;

    protected override void OnSingletonAwake()
    {
        _data = new InventoryData();
        // 注入堆叠上限解析器：按 itemID 从物品库查询，查不到用默认值
        _data.maxStackResolver = itemId =>
        {
            ItemData item = GetItem(itemId);
            return item != null ? item.itemMaxStack : 0;
        };
        BuildLookup();
    }

    /// <summary>按 ID 查找物品配置（查不到返回 null）</summary>
    public ItemData GetItem(int itemId)
    {
        _lookup.TryGetValue(itemId, out ItemData item);
        return item;
    }

    /// <summary>添加物品（按 ItemData 引用），成功/失败均会触发事件</summary>
    public bool AddItem(ItemData item, int count)
    {
        if (item == null || count <= 0) return false;
        bool ok = _data.AddItem(item.itemID, count);
        OnInventoryChanged?.Invoke();
        return ok;
    }

    /// <summary>添加物品（按 ID），ID 未在物品库中注册时返回 false</summary>
    public bool AddItem(int itemId, int count)
    {
        if (GetItem(itemId) == null || count <= 0) return false;
        bool ok = _data.AddItem(itemId, count);
        OnInventoryChanged?.Invoke();
        return ok;
    }

    /// <summary>移除物品，数量不足时返回 false（不触发回滚）</summary>
    public bool RemoveItem(int itemId, int count)
    {
        bool ok = _data.RemoveItem(itemId, count);
        OnInventoryChanged?.Invoke();
        return ok;
    }

    /// <summary>查询某物品总数量</summary>
    public int CountItem(int itemId) => _data.CountItem(itemId);

    public bool HasItem(int itemId) => _data.HasItem(itemId);

    private void BuildLookup()
    {
        _lookup.Clear();
        foreach (ItemData item in itemDatabase)
        {
            if (item == null) continue;
            _lookup[item.itemID] = item;
        }
    }

#if UNITY_EDITOR
    // 编辑器下修改物品库列表后自动重建索引，避免运行时报错
    private void OnValidate()
    {
        BuildLookup();
    }
#endif
}
