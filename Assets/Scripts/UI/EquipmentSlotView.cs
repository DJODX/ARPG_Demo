using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 装备栏槽位：显示该槽位已装备的物品，并处理点击（放入选中的装备 / 卸下）
/// 挂在 EquipmentSlot 预制体实例上；Icon 与 Count 子物体未指定时按名字自动查找
/// </summary>
public class EquipmentSlotView : MonoBehaviour, IPointerClickHandler
{
    [SerializeField, Tooltip("槽位类型（决定能放哪种装备）")]
    private EquipmentType _slotType = EquipmentType.Weapon;

    [SerializeField, Tooltip("物品图标（留空则自动查找子物体 Icon）")]
    private Image _iconImage;

    [SerializeField, Tooltip("槽位名称文本（留空则自动查找子物体 Count）")]
    private Text _labelText;

    /// <summary>点击事件（参数为槽位类型），由 BackpackPanel 订阅</summary>
    public event Action<EquipmentType> OnClicked;

    /// <summary>槽位类型</summary>
    public EquipmentType SlotType => _slotType;

    private void Awake()
    {
        // 未手动指定时按预制体的子物体名查找，避免逐个拖引用
        if (_iconImage == null)
        {
            Transform icon = transform.Find("Icon");
            if (icon != null) _iconImage = icon.GetComponent<Image>();
        }

        if (_labelText == null)
        {
            Transform label = transform.Find("Count");
            if (label != null) _labelText = label.GetComponent<Text>();
        }
    }

    /// <summary>刷新显示：装备中显示图标，空槽显示槽位名称</summary>
    public void Refresh(ItemData item)
    {
        bool equipped = item != null;

        if (_iconImage != null)
        {
            _iconImage.gameObject.SetActive(equipped);
            if (equipped) _iconImage.sprite = item.itemIcon;
        }

        if (_labelText != null)
        {
            _labelText.text = equipped ? string.Empty : _slotType.ToString();
            _labelText.gameObject.SetActive(!equipped);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnClicked?.Invoke(_slotType);
    }
}