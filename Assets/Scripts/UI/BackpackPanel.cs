using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 背包面板：订阅背包/装备数据变化事件刷新显示，
/// 并编排"点击背包格选中 → 点击装备栏放置"的交互
/// 预制体需配置：Content(GridLayoutGroup) + SlotPrefab；
/// 装备栏为面板下的 EquipmentSlotView 子物体，运行时自动收集
/// </summary>
public class BackpackPanel : PanelBase
{
    [SerializeField, Tooltip("格子容器（挂 GridLayoutGroup）")]
    private Transform _gridParent;

    [SerializeField, Tooltip("格子预制体（挂 InventorySlot）")]
    private GameObject _slotPrefab;

    private readonly List<InventorySlot> _slots = new List<InventorySlot>();
    private readonly List<EquipmentSlotView> _equipmentSlots = new List<EquipmentSlotView>();

    private InventoryManager _inventory;
    private EquipmentManager _equipment;

    /// <summary>当前选中的背包格子序号，-1 表示未选中</summary>
    private int _selectedIndex = -1;

    private void Awake()
    {
        // 缓存单例引用：避免在 OnDestroy 中访问 Instance（此时单例可能已销毁，getter 会重新 new 一个）
        _inventory = InventoryManager.Instance;
        _equipment = EquipmentManager.Instance;
    }

    protected override void Init()
    {
        // 按背包容量一次性预创建全部格子，之后只刷新内容，避免频繁创建销毁
        for (int i = 0; i < InventoryData.DefaultCapacity; i++)
        {
            GameObject go = Instantiate(_slotPrefab, _gridParent);
            InventorySlot slot = go.GetComponent<InventorySlot>();
            slot.SetIndex(i);                 // 记录序号，供选中/装备读写对应数据
            slot.OnClicked += HandleSlotClicked;
            _slots.Add(slot);
        }

        // 装备栏是面板的子物体，自动收集，无需在 Inspector 里拖引用
        GetComponentsInChildren(true, _equipmentSlots);
        foreach (EquipmentSlotView view in _equipmentSlots)
        {
            view.OnClicked += HandleEquipmentSlotClicked;
        }

        _inventory.OnInventoryChanged += Refresh;
        _equipment.OnEquipmentChanged += Refresh;
        Refresh();
    }

    private void Refresh()
    {
        InventoryData data = _inventory.Data;

        // 背包格子：序号直接对应数据下标，空位显示为空
        for (int i = 0; i < _slots.Count; i++)
        {
            ItemStack stack = data.GetStack(i);
            ItemData item = stack != null ? _inventory.GetItem(stack.itemId) : null;
            _slots[i].Refresh(item, stack != null ? stack.count : 0);
        }

        // 装备栏：按槽位类型取已装备物品
        foreach (EquipmentSlotView view in _equipmentSlots)
        {
            view.Refresh(_equipment.GetEquipped(view.SlotType));
        }

        // 数据变化后选中的格子可能已空（如装备后腾出该格），清掉失效的选中状态
        if (_selectedIndex >= 0 && data.GetStack(_selectedIndex) == null)
        {
            SetSelectedIndex(-1);
        }
    }

    /// <summary>点击背包格：有物品则选中，点同一格或空格则取消选中</summary>
    private void HandleSlotClicked(int index)
    {
        if (_selectedIndex == index || _inventory.GetStack(index) == null)
        {
            SetSelectedIndex(-1);
            return;
        }

        SetSelectedIndex(index);
    }

    /// <summary>点击装备栏：有选中的背包物品则放入，否则卸下该槽位装备</summary>
    private void HandleEquipmentSlotClicked(EquipmentType slotType)
    {
        if (_selectedIndex < 0)
        {
            _equipment.Unequip(slotType);
            return;
        }

        // 装备失败（类型不匹配/背包满）时保留选中状态，便于玩家换一个槽位重试
        if (_equipment.EquipFromBackpack(_selectedIndex, slotType))
        {
            SetSelectedIndex(-1);
        }
    }

    private void SetSelectedIndex(int index)
    {
        if (_selectedIndex >= 0 && _selectedIndex < _slots.Count)
            _slots[_selectedIndex].SetSelected(false);

        _selectedIndex = index;

        if (_selectedIndex >= 0 && _selectedIndex < _slots.Count)
            _slots[_selectedIndex].SetSelected(true);
    }

    private void OnDestroy()
    {
        if (_inventory != null)
            _inventory.OnInventoryChanged -= Refresh;

        if (_equipment != null)
            _equipment.OnEquipmentChanged -= Refresh;

        foreach (InventorySlot slot in _slots) slot.OnClicked -= HandleSlotClicked;
        foreach (EquipmentSlotView view in _equipmentSlots) view.OnClicked -= HandleEquipmentSlotClicked;
    }
}