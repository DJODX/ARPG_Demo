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