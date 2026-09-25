using UnityEngine;

/// <summary>
/// 玩家装备属性桥接：订阅装备变化事件，汇总所有已装备物品的加成写入 AttributeComponent
/// 采用"整体重算 + 覆盖写入"，不依赖上一次的数值，天然避免装备反复穿脱导致的数值漂移
/// 挂在 Player 身上（与 AttributeComponent 同一物体，或在其子物体上）
/// </summary>
[RequireComponent(typeof(AttributeComponent))]
public class PlayerEquipmentStats : MonoBehaviour
{
    /// <summary>装备槽位类型全集（缓存避免每次重算都分配数组）</summary>
    private static readonly EquipmentType[] AllSlotTypes =
        (EquipmentType[])System.Enum.GetValues(typeof(EquipmentType));

    private AttributeComponent _attribute;
    private EquipmentManager _equipment;

    private void Awake()
    {
        _attribute = GetComponent<AttributeComponent>();
        _equipment = EquipmentManager.Instance;

        // 挂错物体时立刻报错，否则装备加成会静默失效、难以排查
        if (_attribute == null)
            Debug.LogError($"[Equipment] {name} 上未找到 AttributeComponent，装备加成不会生效");
    }

    private void OnEnable()
    {
        if (_equipment != null) _equipment.OnEquipmentChanged += Refresh;
    }

    private void OnDisable()
    {
        if (_equipment != null) _equipment.OnEquipmentChanged -= Refresh;
    }

    private void Start()
    {
        // 首次进入游戏或读档后应用一次，保证初始装备的属性生效
        Refresh();
    }

    /// <summary>遍历全部装备槽位，汇总加成后一次性写入属性组件</summary>
    private void Refresh()
    {
        if (_attribute == null) return;

        float atkBonus = 0f;
        float defBonus = 0f;
        float mpBonus = 0f;
        float critBonus = 0f;

        for (int i = 0; i < AllSlotTypes.Length; i++)
        {
            EquipmentData item = _equipment.GetEquipped(AllSlotTypes[i]) as EquipmentData;
            if (item == null) continue;

            // 按具体装备类型取各自的加成字段
            switch (item)
            {
                case WeaponData weapon:
                    atkBonus += weapon.itemDamage;
                    break;
                case ArmorData armor:
                    defBonus += armor.itemDefense;
                    break;
                case RingData ring:
                    mpBonus += ring.mpBonus;
                    critBonus += ring.critBonus;
                    break;
            }
        }

        _attribute.SetEquipmentBonus(atkBonus, defBonus, mpBonus, critBonus);
    }
}