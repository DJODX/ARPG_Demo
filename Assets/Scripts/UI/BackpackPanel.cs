using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 背包面板：订阅 InventoryManager 数据变化事件，刷新格子显示
/// 预制体需配置：Content(GridLayoutGroup) + SlotPrefab
/// </summary>
public class BackpackPanel : PanelBase
{
    [SerializeField, Tooltip("格子容器（挂 GridLayoutGroup）")]
    private Transform _gridParent;

    [SerializeField, Tooltip("格子预制体（挂 InventorySlot）")]
    private GameObject _slotPrefab;

    private readonly List<InventorySlot> _slots = new List<InventorySlot>();
    private InventoryManager _inventory;

    private void Awake()
    {
        // 缓存单例引用：避免在 OnDestroy 中访问 Instance（此时单例可能已销毁，getter 会重新 new 一个）
        _inventory = InventoryManager.Instance;
    }

    protected override void Init()
    {
        // 按背包容量一次性预创建全部格子，之后只刷新内容，避免频繁创建销毁
        for (int i = 0; i < InventoryData.DefaultCapacity; i++)
        {
            GameObject go = Instantiate(_slotPrefab, _gridParent);
            _slots.Add(go.GetComponent<InventorySlot>());
        }

        _inventory.OnInventoryChanged += Refresh;
        Refresh();
    }

    private void Refresh()
    {
        InventoryData data = InventoryManager.Instance.Data;
        for (int i = 0; i < _slots.Count; i++)
        {
            bool has = i < data.slots.Count;
            ItemStack stack = has ? data.slots[i] : null;
            ItemData item = has ? InventoryManager.Instance.GetItem(stack.itemId) : null;
            _slots[i].Refresh(item, has ? stack.count : 0);
        }
    }

    private void OnDestroy()
    {
        if (_inventory != null)
            _inventory.OnInventoryChanged -= Refresh;
    }
}
