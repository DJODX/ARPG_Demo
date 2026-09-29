using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 任务配置（ScriptableObject）
/// 只描述"任务是什么"：ID、名称、目标列表、奖励列表；
/// 进度等运行时数据由 QuestRuntime 承载——配置与进度分离，存档只需存进度
///
/// 奖励直接复用奖励系统的 RewardEntry 配置，发奖也走 RewardManager，
/// 因此新增任务不需要写任何发放逻辑
/// </summary>
[CreateAssetMenu(menuName = "ARPG/Quests/Quest", fileName = "Quest_")]
public class QuestDefinition : ScriptableObject
{
    [Header("基础信息")]
    [Tooltip("任务唯一 ID（存档依据，不可为空、不可重复）")]
    public string questId;

    [Tooltip("任务名称")]
    public string title;

    [Tooltip("任务描述")]
    [TextArea] public string description;

    [Header("任务目标（全部达成即任务完成）")]
    [Tooltip("目标列表，支持不同类型混排；每个目标是独立的 SO 资产，可被多个任务复用")]
    public List<QuestObjectiveSO> objectives = new List<QuestObjectiveSO>();

    [Header("完成奖励")]
    [Tooltip("复用奖励系统的配置结构，支持金币/经验/道具多条")]
    public List<RewardEntry> rewards = new List<RewardEntry>();
}