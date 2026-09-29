using UnityEngine;

/// <summary>
/// 敌人标识：挂在敌人身上，为击杀类任务提供可配置的敌人 ID
/// 与物品的 itemID 同理，使用字符串便于在配置面板中直观填写（如 orc、goblin）
/// </summary>
public class EnemyIdentity : MonoBehaviour
{
    [Tooltip("敌人标识，需与击杀类任务目标中填写的 ID 完全一致（如 orc）")]
    public string enemyId = "orc";
}