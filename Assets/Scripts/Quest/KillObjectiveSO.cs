using UnityEngine;

/// <summary>
/// 击杀类目标：击杀指定标识的敌人 requiredCount 只
/// 依赖敌人身上的 EnemyIdentity 提供标识；
/// 击杀上报由敌人的 RewardSource 在确认击杀归属（玩家击杀）后发出，
/// 因此挂有 RewardSource 的敌人才会被计数
/// </summary>
[CreateAssetMenu(menuName = "ARPG/Quests/Objectives/Kill", fileName = "Obj_Kill")]
public class KillObjectiveSO : QuestObjectiveSO
{
    [Tooltip("目标敌人标识，需与敌人身上 EnemyIdentity.enemyId 完全一致（如 orc）")]
    public string enemyId;

    public override bool OnEnemyKilled(QuestRuntime runtime, int index, string killedEnemyId)
    {
        if (string.IsNullOrEmpty(enemyId) || killedEnemyId != enemyId) return false;
        return runtime.AddProgress(index, 1);
    }
}