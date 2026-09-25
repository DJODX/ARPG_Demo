using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 背包格子：负责单个格子的图标/数量显示，并支持点击选中
/// 选中高亮直接复用根节点的背景 Image 变色，无需在 Inspector 额外配置
/// </summary>
public class InventorySlot : MonoBehaviour, IPointerClickHandler
{
    [SerializeField, Tooltip("物品图标（默认隐藏）")]
    private Image _iconImage;

    [SerializeField, Tooltip("堆叠数量文本（默认隐藏）")]
    private Text _countText;

    [SerializeField, Tooltip("选中时的背景色")]
    private Color _selectedColor = new Color(1f, 0.85f, 0.3f, 1f);

    /// <summary>根节点背景 Image，用于选中高亮</summary>
    private Image _background;
    private Color _normalColor;

    /// <summary>格子序号（由面板创建时写入，与 InventoryData 下标对应）</summary>
    public int SlotIndex { get; private set; } = -1;

    /// <summary>是否处于选中状态</summary>
    public bool IsSelected { get; private set; }

    /// <summary>点击事件（参数为格子序号），由面板订阅</summary>
    public event Action<int> OnClicked;

    private void Awake()
    {
        _background = GetComponent<Image>();
        if (_background != null) _normalColor = _background.color;
    }

    /// <summary>由面板在创建格子时设置序号</summary>
    public void SetIndex(int index) => SlotIndex = index;

    /// <summary>设置选中状态（仅切换背景色）</summary>
    public void SetSelected(bool selected)
    {
        IsSelected = selected;
        if (_background != null)
            _background.color = selected ? _selectedColor : _normalColor;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnClicked?.Invoke(SlotIndex);
    }

    /// <summary>刷新格子内容；item 为 null 时空显示</summary>
    public void Refresh(ItemData item, int count)
    {
        bool show = item != null && count > 0;
        _iconImage.gameObject.SetActive(show);

        if (!show)
        {
            _countText.text = string.Empty;
            _countText.gameObject.SetActive(false);
            return;
        }

        _iconImage.sprite = item.itemIcon;

        // 数量为 1 时不显示数字，且显隐由代码显式控制，不依赖预制体初始状态
        bool showCount = count > 1;
        _countText.text = showCount ? count.ToString() : string.Empty;
        _countText.gameObject.SetActive(showCount);
    }
}