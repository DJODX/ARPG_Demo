using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 背包格子：负责单个格子的图标/数量显示
/// </summary>
public class InventorySlot : MonoBehaviour
{
    [SerializeField, Tooltip("物品图标（默认隐藏）")]
    private Image _iconImage;

    [SerializeField, Tooltip("堆叠数量文本（默认隐藏）")]
    private Text _countText;

    /// <summary>刷新格子内容；item 为 null 时空显示</summary>
    public void Refresh(ItemData item, int count)
    {
        if (item == null || count <= 0)
        {
            _iconImage.gameObject.SetActive(false);
            _countText.text = string.Empty;
            return;
        }

        _iconImage.sprite = item.itemIcon;
        _iconImage.gameObject.SetActive(true);
        _countText.text = count > 1 ? count.ToString() : string.Empty;
    }
}