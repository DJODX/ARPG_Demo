using UnityEngine;

/// <summary>材料：物品大类固定为 Material，无额外战斗属性</summary>
[CreateAssetMenu(menuName = "ARPG/Items/Material", fileName = "Material")]
public class MaterialData : ItemData
{
    public override ItemType itemType => ItemType.Material;
}
