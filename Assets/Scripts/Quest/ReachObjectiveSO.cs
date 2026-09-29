using UnityEngine;

/// <summary>
/// 到达类目标：玩家进入指定标识的触发区域
/// 区域内需挂 ReachPoint 组件并填写相同的 pointId；多数情况 requiredCount 保持 1
/// </summary>
[CreateAssetMenu(menuName = "ARPG/Quests/Objectives/Reach", fileName = "Obj_Reach")]
public class ReachObjectiveSO : QuestObjectiveSO
{
    [Tooltip("目标区域标识，需与 ReachPoint.pointId 完全一致")]
    public string pointId;

    public override bool OnReachPoint(QuestRuntime runtime, int index, string reachedPointId)
    {
        if (string.IsNullOrEmpty(pointId) || reachedPointId != pointId) return false;
        return runtime.AddProgress(index, 1);
    }
}